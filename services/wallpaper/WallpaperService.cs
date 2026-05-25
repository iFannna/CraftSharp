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

    public async Task ApplyStaticWallpaper(WallpaperItem wallpaper, string? imageUrl = null, Action<string>? onSuccess = null, Action<string>? onError = null)
    {
        try
        {
            if (!Directory.Exists(WallpaperDir))
                Directory.CreateDirectory(WallpaperDir);

            var url = imageUrl ?? wallpaper.PreviewUrl;
            var ext = url.EndsWith(".jpg") || url.EndsWith(".jpeg") ? "jpg" : "webp";
            var filePath = Path.Combine(WallpaperDir, $"{wallpaper.Id}.{ext}");
            if (!File.Exists(filePath))
            {
                var bytes = await _http.GetByteArrayAsync(url);
                await File.WriteAllBytesAsync(filePath, bytes);
            }

            DesktopWallpaperService.Instance.SetWallpaper(filePath);

            onSuccess?.Invoke(filePath);
        }
        catch (Exception ex)
        {
            onError?.Invoke(ex.Message);
        }
    }

    public async Task<byte[]> DownloadBytesAsync(string url)
    {
        return await _http.GetByteArrayAsync(url);
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

    public string GetWallpaperFilePath(WallpaperItem wallpaper)
    {
        return Path.Combine(WallpaperDir, $"{wallpaper.Id}.webp");
    }
}
