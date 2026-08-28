using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using CraftSharp.Helpers;

namespace CraftSharp.Windows.Wallpaper;

public class DynamicWallpaperWindow : IDisposable
{
    private IntPtr _workerw;
    private IntPtr _hwnd;
    private Process? _mpvProcess;
    private TaskCompletionSource<bool>? _renderReadyTcs;
    private NamedPipeClientStream? _ipcPipe;
    private string? _ipcPipeName;
    private bool _disposed;
    private System.Windows.Threading.Dispatcher? _ownerDispatcher;

    public IntPtr Hwnd => _hwnd;

    /// <summary>
    /// 在桌面 WorkerW 下创建覆盖指定物理矩形的子窗口。
    /// physicalBounds 为虚拟桌面坐标系（可为负坐标），跨屏拼接时传虚拟屏总矩形。
    /// </summary>
    public void CreateAndShow(Win32Helper.RECT physicalBounds)
    {
        // nudge=false：多窗口场景下重复广播 0x052C 会互相销毁 WorkerW，
        // WorkerW 的建立/重建统一由编排层负责
        _workerw = FindDesktopWorkerW(nudge: false);

        // 子窗口坐标相对 WorkerW 客户区原点，实测偏移而非假设其等于虚拟屏原点
        Win32Helper.GetWindowRect(_workerw, out var workerRect);
        int x = physicalBounds.Left - workerRect.Left;
        int y = physicalBounds.Top - workerRect.Top;
        int w = physicalBounds.Right - physicalBounds.Left;
        int h = physicalBounds.Bottom - physicalBounds.Top;

        var dpiScope = DpiScope.EnterPerMonitorV2();
        try
        {
            _ownerDispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;
            IntPtr hInstance = System.Runtime.InteropServices.Marshal.GetHINSTANCE(
                typeof(DynamicWallpaperWindow).Assembly.Modules.First());
            _hwnd = Win32Helper.CreateWindowEx(
                0x00000000, "Static", $"CraftSharpWallpaper_{Guid.NewGuid():N}",
                0x10000000 | 0x40000000, // WS_VISIBLE | WS_CHILD
                x, y, w, h,
                _workerw, IntPtr.Zero, hInstance, IntPtr.Zero);

            Win32Helper.SetWindowPos(_hwnd, Win32Helper.HWND_BOTTOM,
                x, y, w, h,
                Win32Helper.SWP_FRAMECHANGED);
        }
        finally
        {
            dpiScope?.Dispose();
        }

        Debug.WriteLine($"[Wallpaper] Host window {_hwnd} in WorkerW {_workerw}, bounds=({physicalBounds.Left},{physicalBounds.Top}) {w}x{h}");
    }

    public void LoadAndPlay(string videoPath)
    {
        Stop();
        _renderReadyTcs = new TaskCompletionSource<bool>();

        string mpvPath = GetMpvPath();
        if (!File.Exists(mpvPath))
        {
            Debug.WriteLine($"[Wallpaper] mpv.exe not found: {mpvPath}");
            _renderReadyTcs.TrySetResult(false);
            return;
        }

        // 每个 MPV 实例使用唯一的管道名
        _ipcPipeName = $"craftsharp-mpv-{Guid.NewGuid():N}";

        var args = $"--wid={_hwnd} --no-audio --loop --no-input-default-bindings " +
                   $"--no-input-terminal --no-terminal --hwdec=auto " +
                   $"--vo=gpu --keep-open --panscan=1.0 " +
                   $"--idle=yes --force-window=no " +
                   $"--input-ipc-server=\\\\.\\pipe\\{_ipcPipeName} " +
                   $"\"{videoPath}\"";

        var psi = new ProcessStartInfo
        {
            FileName = mpvPath,
            Arguments = args,
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardError = true
        };

        _mpvProcess = new Process
        {
            StartInfo = psi,
            EnableRaisingEvents = true
        };
        _mpvProcess.ErrorDataReceived += OnMpvErrorData;
        _mpvProcess.Exited += OnMpvExited;

        _mpvProcess.Start();
        _mpvProcess.BeginErrorReadLine();

        Debug.WriteLine($"[Wallpaper] mpv started, pid={_mpvProcess?.Id}, wid={_hwnd}");

        // 超时兜底：2秒后无论如何标记完成
        Task.Delay(2000).ContinueWith(_ => _renderReadyTcs.TrySetResult(true));
    }

    /// <summary>
    /// 通过 IPC 向当前 MPV 进程发送 loadfile 命令切换视频，复用已有进程。
    /// 返回 true 表示成功，false 表示 IPC 不可用需要回退。
    /// </summary>
    public async Task<bool> SwitchVideoAsync(string videoPath)
    {
        // 等待 IPC 管道可用
        var pipe = await ConnectIpcAsync();
        if (pipe == null)
        {
            Debug.WriteLine("[Wallpaper] IPC pipe not available, falling back to restart");
            return false;
        }

        // 发送 loadfile 命令
        var command = $"{{\"command\":[\"loadfile\",\"{videoPath.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"]}}\n";
        var bytes = Encoding.UTF8.GetBytes(command);
        await pipe.WriteAsync(bytes, 0, bytes.Length);
        await pipe.FlushAsync();

        Debug.WriteLine($"[Wallpaper] IPC loadfile sent: {videoPath}");

        // loadfile 不会触发新的 VO: 日志，短延迟让 MPV 开始解码即可
        await Task.Delay(200);
        return true;
    }

    /// <summary>
    /// 指示 IPC 连接是否就绪（MPV 进程存活且管道可连接）。
    /// </summary>
    public bool IsIpcReady => _mpvProcess != null && !_mpvProcess.HasExited && _ipcPipeName != null;

    private async Task<NamedPipeClientStream?> ConnectIpcAsync()
    {
        if (_ipcPipeName == null || _mpvProcess == null || _mpvProcess.HasExited)
            return null;

        try
        {
            var pipe = new NamedPipeClientStream(".", _ipcPipeName, PipeDirection.Out);
            // 最多等 1 秒连接管道
            await pipe.ConnectAsync(1000);
            _ipcPipe = pipe;
            return pipe;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Wallpaper] IPC connect failed: {ex.Message}");
            return null;
        }
    }

    private void OnMpvErrorData(object sender, DataReceivedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.Data)) return;
        Debug.WriteLine($"[Wallpaper] mpv: {e.Data}");
        // VO: 行表示视频输出已初始化，首帧即将渲染
        if (e.Data.Contains("VO:"))
            _renderReadyTcs?.TrySetResult(true);
    }

    private void OnMpvExited(object? sender, EventArgs e)
    {
        _renderReadyTcs?.TrySetResult(false);
    }

    public Task<bool> WaitForRenderReadyAsync() =>
        _renderReadyTcs?.Task ?? Task.FromResult(true);

    public void Stop()
    {
        _ipcPipe?.Dispose();
        _ipcPipe = null;

        if (_mpvProcess == null) return;

        _mpvProcess.ErrorDataReceived -= OnMpvErrorData;
        _mpvProcess.Exited -= OnMpvExited;
        if (!_mpvProcess.HasExited)
        {
            try { _mpvProcess.Kill(true); } catch { }
        }
        _mpvProcess.Dispose();
        _mpvProcess = null;
    }

    public void SetVolume(double volume, bool muted)
    {
        // mpv 启动时已 --no-audio，如需后续控制可用 IPC
    }

    public void Close()
    {
        // DestroyWindow 只能在创建窗口的线程上执行
        if (_ownerDispatcher != null && !_ownerDispatcher.CheckAccess())
        {
            _ownerDispatcher.BeginInvoke(Close);
            return;
        }
        Stop();
        if (_hwnd != IntPtr.Zero)
        {
            Win32Helper.DestroyWindow(_hwnd);
            _hwnd = IntPtr.Zero;
        }
    }

    private static string GetMpvPath()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string path = Path.Combine(baseDir, "tools", "mpv.exe");
        if (File.Exists(path)) return path;

        // 开发环境：从项目根目录查找
        path = Path.Combine(baseDir, "..", "..", "..", "tools", "mpv.exe");
        return Path.GetFullPath(path);
    }

    /// <summary>
    /// 查找桌面 WorkerW。nudge=true 时先广播 0x052C 确保其存在（仅编排层在重排时使用一次）。
    /// </summary>
    public static IntPtr FindDesktopWorkerW(bool nudge)
    {
        IntPtr progman = Win32Helper.FindWindow("Progman", null);
        if (nudge)
        {
            Win32Helper.SendMessageTimeout(progman, 0x052C, IntPtr.Zero, IntPtr.Zero,
                Win32Helper.SMTO_NORMAL, 3000, out _);
        }

        IntPtr workerw = Win32Helper.FindWindowEx(progman, IntPtr.Zero, "WorkerW", null);
        if (workerw != IntPtr.Zero) return workerw;

        IntPtr result = IntPtr.Zero;
        Win32Helper.EnumWindows((hWnd, _) =>
        {
            var className = new System.Text.StringBuilder(256);
            Win32Helper.GetClassName(hWnd, className, 256);
            if (className.ToString() == "WorkerW")
            {
                if (Win32Helper.GetParent(hWnd) == progman)
                {
                    result = hWnd;
                    return false;
                }
            }
            return true;
        }, IntPtr.Zero);

        return result != IntPtr.Zero ? result : progman;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Close();
    }
}
