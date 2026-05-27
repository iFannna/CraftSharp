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
        // 如果已有窗口且 IPC 可用，直接通过 loadfile 切换视频，无需重启 MPV
        if (_window != null && _window.IsIpcReady && _currentVideoPath != null)
        {
            await _window.SwitchVideoAsync(videoPath);
            _currentVideoPath = videoPath;
            return;
        }

        // 首次播放或 IPC 不可用时，走完整的窗口创建流程
        var oldWindow = _window;
        _window = null;

        _currentVideoPath = videoPath;
        _window = new DynamicWallpaperWindow();
        _window.CreateAndShow(primary: true, behindWindow: oldWindow?.Hwnd ?? IntPtr.Zero);
        _window.LoadAndPlay(videoPath);

        await _window.WaitForRenderReadyAsync();

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

        _window = new DynamicWallpaperWindow();
        _window.CreateAndShow(primary: true);
        _window.LoadAndPlay(settings.LocalFilePath);
        SetVolume(settings.VideoVolume, settings.VideoMuted);
    }
}
