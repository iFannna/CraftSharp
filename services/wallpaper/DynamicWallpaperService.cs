using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CraftSharp.Helpers;
using CraftSharp.Windows.Wallpaper;

namespace CraftSharp.Services.Wallpaper;

/// <summary>
/// 动态壁纸多实例管理：每个显示器一个宿主窗口 + mpv 进程，
/// 跨屏拼接模式为单窗口覆盖虚拟桌面（key 使用 SpanKey）。
/// </summary>
public class DynamicWallpaperService
{
    public const string SpanKey = "span";

    private readonly Dictionary<string, DynamicWallpaperWindow> _windows = new();
    private readonly Dictionary<string, string> _videoPaths = new();
    private readonly SemaphoreSlim _gate = new(1, 1);

    public static DynamicWallpaperService Instance { get; } = new();

    private DynamicWallpaperService() { }

    /// <summary>
    /// 在指定目标矩形上启动（或重建）一路动态壁纸
    /// </summary>
    public async Task StartPlaybackAsync(string key, string videoPath, Win32Helper.RECT bounds)
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            StopCore(key);

            var window = new DynamicWallpaperWindow();

            // 窗口创建必须走 UI 线程（消息泵 + STA）
            var ui = System.Windows.Application.Current?.Dispatcher;
            if (ui != null && !ui.CheckAccess())
                await ui.InvokeAsync(() => window.CreateAndShow(bounds)).Task.ConfigureAwait(false);
            else
                window.CreateAndShow(bounds);

            window.LoadAndPlay(videoPath);
            lock (_windows)
            {
                _windows[key] = window;
                _videoPaths[key] = videoPath;
            }
            await window.WaitForRenderReadyAsync().ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// 通过 IPC 对已有实例热切换视频（零窗口重建）。不可用返回 false。
    /// </summary>
    public async Task<bool> SwitchVideoAsync(string key, string videoPath)
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            DynamicWallpaperWindow? window;
            lock (_windows) window = _windows.GetValueOrDefault(key);
            if (window == null || !window.IsIpcReady) return false;

            string? current;
            lock (_windows) current = _videoPaths.GetValueOrDefault(key);
            if (current == null) return false;

            if (await window.SwitchVideoAsync(videoPath).ConfigureAwait(false))
            {
                lock (_windows) _videoPaths[key] = videoPath;
                return true;
            }
            return false;
        }
        finally
        {
            _gate.Release();
        }
    }

    public bool IsPlayingForKey(string key)
    {
        lock (_windows) return _windows.ContainsKey(key);
    }

    public bool IsPlaying
    {
        get { lock (_windows) return _windows.Count > 0; }
    }

    public IReadOnlyCollection<string> ActiveKeys
    {
        get { lock (_windows) return new List<string>(_windows.Keys); }
    }

    public void Stop(string key)
    {
        _gate.Wait();
        try { StopCore(key); }
        finally { _gate.Release(); }
    }

    public void StopAllPlayback()
    {
        _gate.Wait();
        try
        {
            foreach (var key in new List<string>(_windows.Keys))
                StopCore(key);
        }
        finally { _gate.Release(); }
    }

    private void StopCore(string key)
    {
        DynamicWallpaperWindow? window;
        lock (_windows)
        {
            if (!_windows.Remove(key, out window)) return;
            _videoPaths.Remove(key);
        }
        window?.Close();
    }

    public void SetVolume(double volume, bool muted)
    {
        List<DynamicWallpaperWindow> snapshot;
        lock (_windows) snapshot = new List<DynamicWallpaperWindow>(_windows.Values);
        foreach (var window in snapshot)
            window.SetVolume(volume, muted);
    }
}
