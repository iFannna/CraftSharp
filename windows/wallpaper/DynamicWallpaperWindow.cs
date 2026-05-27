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

    public IntPtr Hwnd => _hwnd;

    public void CreateAndShow(bool primary = true, IntPtr behindWindow = default)
    {
        _workerw = FindDesktopWorkerW();

        var monitor = Win32Helper.GetPrimaryMonitorInfo();
        int screenW = monitor.rcMonitor.Right - monitor.rcMonitor.Left;
        int screenH = monitor.rcMonitor.Bottom - monitor.rcMonitor.Top;

        IntPtr hInstance = System.Runtime.InteropServices.Marshal.GetHINSTANCE(
            typeof(DynamicWallpaperWindow).Assembly.Modules.First());
        _hwnd = Win32Helper.CreateWindowEx(
            0x00000000, "Static", "CraftSharpWallpaper",
            0x10000000 | 0x40000000, // WS_VISIBLE | WS_CHILD
            0, 0, screenW, screenH,
            _workerw, IntPtr.Zero, hInstance, IntPtr.Zero);

        if (behindWindow != IntPtr.Zero)
        {
            Win32Helper.SetWindowPos(_hwnd, behindWindow,
                0, 0, screenW, screenH,
                Win32Helper.SWP_FRAMECHANGED | 0x0010); // SWP_NOACTIVATE
        }
        else
        {
            Win32Helper.SetWindowPos(_hwnd, Win32Helper.HWND_BOTTOM,
                0, 0, screenW, screenH,
                Win32Helper.SWP_FRAMECHANGED);
        }

        Debug.WriteLine($"[Wallpaper] Host window {_hwnd} in WorkerW {_workerw}, {screenW}x{screenH}");
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

        var monitor = Win32Helper.GetPrimaryMonitorInfo();
        int screenW = monitor.rcMonitor.Right - monitor.rcMonitor.Left;
        int screenH = monitor.rcMonitor.Bottom - monitor.rcMonitor.Top;

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
    /// </summary>
    public async Task SwitchVideoAsync(string videoPath)
    {
        _renderReadyTcs = new TaskCompletionSource<bool>();

        // 等待 IPC 管道可用
        var pipe = await ConnectIpcAsync();
        if (pipe == null)
        {
            Debug.WriteLine("[Wallpaper] IPC pipe not available, falling back to restart");
            return;
        }

        // 发送 loadfile 命令
        var command = $"{{\"command\":[\"loadfile\",\"{videoPath.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"]}}\n";
        var bytes = Encoding.UTF8.GetBytes(command);
        await pipe.WriteAsync(bytes, 0, bytes.Length);
        await pipe.FlushAsync();

        Debug.WriteLine($"[Wallpaper] IPC loadfile sent: {videoPath}");

        // 等待首帧渲染（VO 日志会触发）
        await _renderReadyTcs.Task;

        // 超时兜底
        Task.Delay(2000).ContinueWith(_ => _renderReadyTcs?.TrySetResult(true));
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

    public Task WaitForRenderReadyAsync() =>
        _renderReadyTcs?.Task ?? Task.CompletedTask;

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

    private static IntPtr FindDesktopWorkerW()
    {
        IntPtr progman = Win32Helper.FindWindow("Progman", null);
        Win32Helper.SendMessageTimeout(progman, 0x052C, IntPtr.Zero, IntPtr.Zero,
            Win32Helper.SMTO_NORMAL, 3000, out _);

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
