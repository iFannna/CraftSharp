using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
using CraftSharp.Helpers;

namespace CraftSharp.Windows.Wallpaper;

public class DynamicWallpaperWindow : IDisposable
{
    private IntPtr _workerw;
    private IntPtr _hwnd;
    private Process? _mpvProcess;

    public void CreateAndShow(bool primary = true)
    {
        // 不在这里创建窗口，mpv 会自己创建
    }

    public void LoadAndPlay(string videoPath)
    {
        Stop();

        string mpvPath = GetMpvPath();
        if (!File.Exists(mpvPath))
        {
            Debug.WriteLine($"[Wallpaper] mpv.exe not found: {mpvPath}");
            return;
        }

        // 先找 WorkerW
        _workerw = FindDesktopWorkerW();

        // 启动 mpv，让它自己创建窗口，然后我们再 SetParent
        var args = $"--no-audio --loop --no-input-default-bindings " +
                   $"--no-input-terminal --no-terminal --panscan=1.0 --hwdec=auto " +
                   $"--vo=gpu --keep-open --gpu-api=d3d11 --force-window " +
                   $"--geometry=1920x1080+0+0 \"{videoPath}\"";

        var psi = new ProcessStartInfo
        {
            FileName = mpvPath,
            Arguments = args,
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardError = true
        };

        _mpvProcess = Process.Start(psi);
        if (_mpvProcess != null)
        {
            _mpvProcess.BeginErrorReadLine();
            _mpvProcess.ErrorDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    Debug.WriteLine($"[Wallpaper] mpv: {e.Data}");
            };
        }

        Debug.WriteLine($"[Wallpaper] mpv started, pid={_mpvProcess?.Id}, video={videoPath}");

        // 等一下让 mpv 创建窗口，然后把它嵌入 WorkerW
        System.Threading.Thread.Sleep(500);
        EmbedMpvWindow();
    }

    private void EmbedMpvWindow()
    {
        if (_mpvProcess == null || _mpvProcess.HasExited) return;

        // 找 mpv 的窗口句柄
        IntPtr mpvHwnd = FindProcessWindow(_mpvProcess.Id);
        if (mpvHwnd == IntPtr.Zero)
        {
            Debug.WriteLine("[Wallpaper] Could not find mpv window");
            return;
        }

        Debug.WriteLine($"[Wallpaper] Found mpv window: {mpvHwnd}, embedding into WorkerW: {_workerw}");

        // 设为 WorkerW 的子窗口
        Win32Helper.SetParent(mpvHwnd, _workerw);

        // 获取屏幕尺寸
        var monitor = Win32Helper.GetPrimaryMonitorInfo();
        int w = monitor.rcMonitor.Right - monitor.rcMonitor.Left;
        int h = monitor.rcMonitor.Bottom - monitor.rcMonitor.Top;

        // 调整大小并放到最底层
        Win32Helper.SetWindowPos(mpvHwnd, Win32Helper.HWND_BOTTOM,
            0, 0, w, h, Win32Helper.SWP_FRAMECHANGED);

        // 隐藏 Alt+Tab
        Win32Helper.ApplyToolWindowStyle(mpvHwnd);

        _hwnd = mpvHwnd;
        Debug.WriteLine($"[Wallpaper] mpv window {mpvHwnd} embedded successfully");
    }

    private static IntPtr FindProcessWindow(int pid)
    {
        IntPtr result = IntPtr.Zero;
        Win32Helper.EnumWindows((hWnd, _) =>
        {
            Win32Helper.GetWindowThreadProcessId(hWnd, out int windowPid);
            if (windowPid == pid)
            {
                result = hWnd;
                return false;
            }
            return true;
        }, IntPtr.Zero);
        return result;
    }

    public void Stop()
    {
        if (_mpvProcess != null && !_mpvProcess.HasExited)
        {
            try { _mpvProcess.Kill(true); } catch { }
            _mpvProcess.Dispose();
            _mpvProcess = null;
        }
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
        Close();
    }
}
