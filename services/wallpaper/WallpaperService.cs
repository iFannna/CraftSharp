using System.IO;
using System.Net.Http;
using System.Windows;
using CraftSharp.Models;

namespace CraftSharp.Services.Wallpaper;

public class WallpaperService
{
    private static readonly HttpClient _http = new();

    public static WallpaperService Instance { get; } = new();

    private WallpaperService() { }

    private static string WallpaperDir => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "wallpaper");

    public async Task ApplyStaticWallpaper(WallpaperItem wallpaper, Action<string>? onSuccess = null, Action<string>? onError = null)
    {
        try
        {
            if (!Directory.Exists(WallpaperDir))
                Directory.CreateDirectory(WallpaperDir);

            var filePath = Path.Combine(WallpaperDir, $"{wallpaper.Id}.webp");
            if (!File.Exists(filePath))
            {
                var bytes = await _http.GetByteArrayAsync(wallpaper.PreviewUrl);
                await File.WriteAllBytesAsync(filePath, bytes);
            }

            DesktopWallpaperService.Instance.SetWallpaper(filePath);

            if (Application.Current is App app)
            {
                var settings = app.GetAppSettings();
                if (settings != null)
                {
                    settings.Wallpaper.CurrentWallpaperId = wallpaper.Id;
                    settings.Wallpaper.CurrentType = "static";
                    settings.Wallpaper.LocalFilePath = filePath;
                    app.SaveSettings();
                }
            }

            onSuccess?.Invoke(filePath);
        }
        catch (Exception ex)
        {
            onError?.Invoke(ex.Message);
        }
    }

    public async Task DownloadFileAsync(string url, string localPath, Action<string>? onError = null)
    {
        try
        {
            if (!Directory.Exists(WallpaperDir))
                Directory.CreateDirectory(WallpaperDir);

            if (!File.Exists(localPath))
            {
                var bytes = await _http.GetByteArrayAsync(url);
                await File.WriteAllBytesAsync(localPath, bytes);
            }
        }
        catch (Exception ex)
        {
            onError?.Invoke(ex.Message);
        }
    }

    public void RestoreFromSettings()
    {
        if (Application.Current is not App app) return;
        var settings = app.GetAppSettings();
        if (settings == null) return;

        if (settings.Wallpaper.CurrentType == "static" &&
            !string.IsNullOrEmpty(settings.Wallpaper.LocalFilePath) &&
            File.Exists(settings.Wallpaper.LocalFilePath))
        {
            DesktopWallpaperService.Instance.SetWallpaper(settings.Wallpaper.LocalFilePath);
        }
    }

    public string GetWallpaperFilePath(WallpaperItem wallpaper)
    {
        return Path.Combine(WallpaperDir, $"{wallpaper.Id}.webp");
    }
}
