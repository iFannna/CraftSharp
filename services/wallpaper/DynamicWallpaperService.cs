using System.IO;
using CraftSharp.Models;

namespace CraftSharp.Services.Wallpaper;

public class DynamicWallpaperService
{
    private Windows.Wallpaper.DynamicWallpaperWindow? _window;
    private string? _currentVideoPath;

    public static DynamicWallpaperService Instance { get; } = new();

    private DynamicWallpaperService() { }

    public void StartPlayback(string videoPath)
    {
        StopPlayback();

        _currentVideoPath = videoPath;
        _window = new Windows.Wallpaper.DynamicWallpaperWindow();
        _window.CreateAndShow(primary: true);
        _window.LoadAndPlay(videoPath);
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

        StartPlayback(settings.LocalFilePath);
        SetVolume(settings.VideoVolume, settings.VideoMuted);
    }
}
