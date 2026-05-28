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

    private static async Task<byte[]> DownloadWithRetryAsync(string url, int maxRetries = 3)
    {
        for (var i = 0; i < maxRetries; i++)
        {
            try
            {
                return await _http.GetByteArrayAsync(url);
            }
            catch (HttpRequestException) when (i < maxRetries - 1) { }
        }
        return await _http.GetByteArrayAsync(url);
    }

    public async Task ApplyStaticWallpaper(WallpaperItem wallpaper, Action<string>? onSuccess = null, Action<string>? onError = null)
    {
        try
        {
            if (!Directory.Exists(WallpaperDir))
                Directory.CreateDirectory(WallpaperDir);

            var url = await GetOriginalUrlAsync(wallpaper);
            var ext = url.EndsWith(".jpg") || url.EndsWith(".jpeg") ? "jpg" : "webp";
            var filePath = Path.Combine(WallpaperDir, $"{wallpaper.Id}.{ext}");
            if (!File.Exists(filePath))
            {
                var bytes = await DownloadWithRetryAsync(url);
                await File.WriteAllBytesAsync(filePath, bytes);
            }

            DynamicWallpaperService.Instance.StopPlayback();
            DesktopWallpaperService.Instance.SetWallpaper(filePath);

            onSuccess?.Invoke(filePath);
        }
        catch (Exception ex)
        {
            onError?.Invoke(ex.Message);
        }
    }

    public async Task ApplyDynamicWallpaper(WallpaperItem wallpaper, Action<string>? onSuccess = null, Action<string>? onError = null)
    {
        try
        {
            if (!Directory.Exists(WallpaperDir))
                Directory.CreateDirectory(WallpaperDir);

            var videoUrl = wallpaper.PreviewVideoUrl;
            if (string.IsNullOrEmpty(videoUrl))
            {
                onError?.Invoke("No video URL available.");
                return;
            }

            var filePath = Path.Combine(WallpaperDir, $"{wallpaper.Id}.mp4");

            if (!File.Exists(filePath))
            {
                await DownloadFileAsync(videoUrl, filePath, onError);
                if (!File.Exists(filePath))
                    return;
            }

            // 先设置静态壁纸作为回退，这样程序关闭后桌面不会变黑
            // SetWallpaper 会销毁 WorkerW，必须先设完并等待重建，再启动动态壁纸
            if (DynamicWallpaperService.Instance.IsPlaying)
                DynamicWallpaperService.Instance.StopPlayback();

            var previewExt = wallpaper.PreviewUrl.EndsWith(".jpg") || wallpaper.PreviewUrl.EndsWith(".jpeg") ? "jpg" : "webp";
            var previewPath = Path.Combine(WallpaperDir, $"{wallpaper.Id}.{previewExt}");
            if (!File.Exists(previewPath))
            {
                try
                {
                    var previewBytes = await DownloadWithRetryAsync(wallpaper.PreviewUrl);
                    await File.WriteAllBytesAsync(previewPath, previewBytes);
                }
                catch { }
            }
            if (File.Exists(previewPath))
            {
                DesktopWallpaperService.Instance.SetWallpaper(previewPath);
                // 等待 WorkerW 销毁重建完成
                await Task.Delay(500);
            }

            await DynamicWallpaperService.Instance.StartPlaybackAsync(filePath);

            onSuccess?.Invoke(filePath);
        }
        catch (Exception ex)
        {
            onError?.Invoke(ex.Message);
        }
    }

    public async Task<string> GetOriginalUrlAsync(WallpaperItem wallpaper)
    {
        try
        {
            var detail = await McBlockApiClient.Instance.GetWallpaperDetailAsync(wallpaper.Id);
            return detail.OriginalUrl ?? wallpaper.PreviewUrl;
        }
        catch
        {
            return wallpaper.PreviewUrl;
        }
    }

    public async Task<byte[]> DownloadBytesAsync(string url)
    {
        return await DownloadWithRetryAsync(url);
    }

    public async Task DownloadFileAsync(string url, string localPath, Action<string>? onError = null)
    {
        try
        {
            if (!Directory.Exists(WallpaperDir))
                Directory.CreateDirectory(WallpaperDir);

            if (!File.Exists(localPath))
            {
                var bytes = await DownloadWithRetryAsync(url);
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
