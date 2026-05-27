using System.IO;
using System.Threading.Tasks;
using CraftSharp.Models;
using CraftSharp.Windows.Wallpaper;

namespace CraftSharp.Services.Wallpaper;

public class DynamicWallpaperService
{
    private DynamicWallpaperWindow? _window;
    private string? _currentVideoPath;

    public static DynamicWallpaperService Instance { get; } = new();

    private DynamicWallpaperService() { }

    public async Task StartPlaybackAsync(string videoPath)
    {
        var oldWindow = _window;
        _window = null;

        _currentVideoPath = videoPath;
        _window = new DynamicWallpaperWindow();
        _window.CreateAndShow(primary: true, behindWindow: oldWindow?.Hwnd ?? IntPtr.Zero);
        _window.LoadAndPlay(videoPath);

        // 等待新 mpv 渲染首帧，最长等 2 秒（超时会继续，避免卡死）
        await _window.WaitForRenderReadyAsync();

        // 新视频已就绪，关闭旧窗口——视觉上无感知
        oldWindow?.Close();
    }

    public void StopPlayback()
    {
        if (_window != null)
        {
            _window.Close();
            _window = null;
        }
        _currentVideoPath = null;
    }

    public void SetVolume(double volume, bool muted)
    {
        _window?.SetVolume(volume, muted);
    }

    public bool IsPlaying => _window != null;

    public void RestoreFromSettings(WallpaperSettings settings)
    {
        if (settings.CurrentType != "dynamic") return;
        if (string.IsNullOrEmpty(settings.LocalFilePath)) return;
        if (!File.Exists(settings.LocalFilePath)) return;

        // 启动恢复不需要异步等待，没有旧窗口需要处理
        _window = new DynamicWallpaperWindow();
        _window.CreateAndShow(primary: true);
        _window.LoadAndPlay(settings.LocalFilePath);
        SetVolume(settings.VideoVolume, settings.VideoMuted);
    }
}
