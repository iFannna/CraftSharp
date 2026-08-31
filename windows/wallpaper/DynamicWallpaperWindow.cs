using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using CraftSharp.Helpers;

namespace CraftSharp.Windows.Wallpaper;

public class DynamicWallpaperWindow : IDisposable
{
    private IntPtr _workerw;
    private IntPtr _hwnd;
    private IntPtr _mpv;
    private Thread? _eventThread;
    private volatile bool _eventThreadStop;
    private TaskCompletionSource<bool>? _renderReadyTcs;
    private bool _disposed;
    private System.Windows.Threading.Dispatcher? _ownerDispatcher;
    private Win32Helper.RECT _bounds;

    public IntPtr Hwnd => _hwnd;

    /// <summary>窗口覆盖的虚拟桌面矩形，用于遮挡判断</summary>
    public Win32Helper.RECT Bounds => _bounds;

    /// <summary>
    /// 宿主窗口与 libmpv 实例是否都还活着。WorkerW 被系统重建时会连带销毁子窗口，
    /// 这里用于看门狗检测。
    /// </summary>
    public bool IsAlive => _hwnd != IntPtr.Zero
        && Win32Helper.IsWindow(_hwnd)
        && _mpv != IntPtr.Zero;

    /// <summary>
    /// 在桌面 WorkerW 下创建覆盖指定物理矩形的子窗口。
    /// physicalBounds 为虚拟桌面坐标系（可为负坐标），跨屏拼接时传虚拟屏总矩形。
    /// </summary>
    public void CreateAndShow(Win32Helper.RECT physicalBounds)
    {
        // nudge=false：多窗口场景下重复广播 0x052C 会互相销毁 WorkerW，
        // WorkerW 的建立/重建统一由编排层负责
        _workerw = FindDesktopWorkerW(nudge: false);
        _bounds = physicalBounds;

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

            if (_hwnd == IntPtr.Zero)
                Debug.WriteLine($"[Wallpaper] Host window creation failed, err={Marshal.GetLastWin32Error()}, workerw={_workerw}");

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

        if (!MpvNative.EnsureLoaded())
        {
            MpvDiag("libmpv-2.dll not found in tools/");
            _renderReadyTcs.TrySetResult(false);
            return;
        }

        var ctx = MpvNative.mpv_create();
        if (ctx == IntPtr.Zero)
        {
            MpvDiag("mpv_create failed");
            _renderReadyTcs.TrySetResult(false);
            return;
        }

        MpvNative.mpv_request_log_messages(ctx, "warn");

        void Opt(string name, string value)
        {
            var rc = MpvNative.mpv_set_option_string(ctx, name, value);
            if (rc < 0)
                MpvDiag($"option {name}={value} rejected, rc={rc}");
        }

        MpvDiag($"=== LoadAndPlay wid=0x{_hwnd:X} file={videoPath}");

        Opt("wid", _hwnd.ToInt64().ToString());
        Opt("vo", "gpu");
        Opt("hwdec", "auto");
        Opt("audio", "no");
        Opt("loop-file", "inf");
        Opt("panscan", "1.0");
        Opt("keep-open", "yes");
        // mpv 默认 demuxer 缓存上限 150+50MiB，循环播放挂机后会涨满，收紧
        Opt("demuxer-max-bytes", "8388608");
        Opt("demuxer-max-back-bytes", "16777216");

        var initRc = MpvNative.mpv_initialize(ctx);
        if (initRc < 0)
        {
            MpvDiag($"mpv_initialize failed rc={initRc}");
            MpvNative.mpv_terminate_destroy(ctx);
            _renderReadyTcs.TrySetResult(false);
            return;
        }

        _mpv = ctx;

        // 事件线程：PLAYBACK_RESTART（播放开始，首帧已入列）作为渲染就绪信号
        _eventThreadStop = false;
        _eventThread = new Thread(EventLoop) { IsBackground = true, Name = "mpv-event" };
        _eventThread.Start();

        var loadRc = MpvNative.Command(_mpv, "loadfile", videoPath);
        MpvDiag($"loadfile rc={loadRc}");
        if (loadRc < 0)
        {
            _renderReadyTcs.TrySetResult(false);
            return;
        }

        // 超时兜底：2秒后无论如何标记完成
        Task.Delay(2000).ContinueWith(_ => _renderReadyTcs.TrySetResult(true));
    }

    private static readonly object _diagLock = new();
    private static void MpvDiag(string line)
    {
        try
        {
            lock (_diagLock)
                File.AppendAllText(
                    Path.Combine(Path.GetTempPath(), "craftsharp_mpv.log"),
                    $"{DateTime.Now:HH:mm:ss.fff} {line}\n");
        }
        catch { }
    }

    /// <summary>
    /// mpv 事件泵。每轮重新读 _mpv：Stop 清零后立即退出，
    /// 避免对已销毁上下文调用 wait_event。
    /// </summary>
    private void EventLoop()
    {
        while (!_eventThreadStop)
        {
            var ctx = _mpv;
            if (ctx == IntPtr.Zero) return;

            var ev = MpvNative.mpv_wait_event(ctx, 0.5);
            if (ev == IntPtr.Zero) return;

            var evt = Marshal.PtrToStructure<MpvNative.MpvEvent>(ev);
            if (evt.EventId == MpvNative.MpvEventPlaybackRestart)
                _renderReadyTcs?.TrySetResult(true);
            else if (evt.EventId == MpvNative.MpvEventLogMessageId)
            {
                var msg = Marshal.PtrToStructure<MpvNative.MpvEventLogMessage>(evt.Data);
                var text = Marshal.PtrToStringUTF8(msg.Text);
                if (!string.IsNullOrEmpty(text))
                    MpvDiag($"[mpv/{Marshal.PtrToStringUTF8(msg.Level)}] {text.TrimEnd()}");
            }
            else if (evt.EventId == MpvNative.MpvEventShutdown)
                return;
        }
    }

    /// <summary>
    /// 对当前 libmpv 实例热切换视频（零窗口重建）。返回 false 表示实例已失效需重建。
    /// </summary>
    public Task<bool> SwitchVideoAsync(string videoPath)
    {
        var ctx = _mpv;
        if (ctx == IntPtr.Zero) return Task.FromResult(false);

        var ok = MpvNative.Command(ctx, "loadfile", videoPath) >= 0;
        if (!ok)
            Debug.WriteLine($"[Wallpaper] mpv loadfile failed: {videoPath}");
        return Task.FromResult(ok);
    }

    /// <summary>
    /// 播放实例是否就绪（可用于热切换）。
    /// </summary>
    public bool IsPlayerReady => _mpv != IntPtr.Zero;

    /// <summary>
    /// 暂停/恢复播放（遮挡时停解码省 CPU/GPU）。
    /// </summary>
    public Task SetPausedAsync(bool paused)
    {
        var ctx = _mpv;
        if (ctx != IntPtr.Zero)
        {
            if (MpvNative.mpv_set_property_string(ctx, "pause", paused ? "yes" : "no") < 0)
                Debug.WriteLine($"[Wallpaper] SetPaused({paused}) failed");
        }
        return Task.CompletedTask;
    }

    public Task<bool> WaitForRenderReadyAsync() =>
        _renderReadyTcs?.Task ?? Task.FromResult(true);

    public void Stop()
    {
        var ctx = _mpv;
        if (ctx == IntPtr.Zero) return;

        // 先停事件泵再销毁上下文，杜绝 wait_event 触达已释放内存。
        // 500ms 轮询超时 + wakeup 保证 Join 几乎必然即时返回
        _eventThreadStop = true;
        MpvNative.mpv_wakeup(ctx);
        _eventThread?.Join(2000);
        _eventThread = null;

        _mpv = IntPtr.Zero;
        MpvNative.mpv_terminate_destroy(ctx);
    }

    public void SetVolume(double volume, bool muted)
    {
        // mpv 启动时已 audio=no，壁纸无音频需求
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
