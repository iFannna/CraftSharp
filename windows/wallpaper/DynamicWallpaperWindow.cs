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
        _workerw = FindDesktopWorkerW();

        var monitor = Win32Helper.GetPrimaryMonitorInfo();
        int screenW = monitor.rcMonitor.Right - monitor.rcMonitor.Left;
        int screenH = monitor.rcMonitor.Bottom - monitor.rcMonitor.Top;

        // 在 WorkerW 内创建隐藏子窗口，mpv 将直接渲染到这里
        IntPtr hInstance = System.Runtime.InteropServices.Marshal.GetHINSTANCE(
            typeof(DynamicWallpaperWindow).Assembly.Modules.First());
        _hwnd = Win32Helper.CreateWindowEx(
            0x00000000, "Static", "CraftSharpWallpaper",
            0x10000000 | 0x40000000, // WS_VISIBLE | WS_CHILD
            0, 0, screenW, screenH,
            _workerw, IntPtr.Zero, hInstance, IntPtr.Zero);

        Win32Helper.SetWindowPos(_hwnd, Win32Helper.HWND_BOTTOM,
            0, 0, screenW, screenH, 0x0020); // SWP_FRAMECHANGED

        Debug.WriteLine($"[Wallpaper] Host window {_hwnd} in WorkerW {_workerw}, {screenW}x{screenH}");
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

        var monitor = Win32Helper.GetPrimaryMonitorInfo();
        int screenW = monitor.rcMonitor.Right - monitor.rcMonitor.Left;
        int screenH = monitor.rcMonitor.Bottom - monitor.rcMonitor.Top;

        // --wid 直接渲染到宿主窗口，零闪烁
        var args = $"--wid={_hwnd} --no-audio --loop --no-input-default-bindings " +
                   $"--no-input-terminal --no-terminal --hwdec=auto " +
                   $"--vo=gpu --keep-open --panscan=1.0 \"{videoPath}\"";

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

        Debug.WriteLine($"[Wallpaper] mpv started, pid={_mpvProcess?.Id}, wid={_hwnd}");
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
