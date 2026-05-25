using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Windows.Media.Imaging;

namespace CraftSharp.Services.Wallpaper;

public class WallpaperImageCache
{
    private static readonly HttpClient _http = new();
    private readonly ConcurrentDictionary<string, BitmapImage> _cache = new();
    private const int MaxCacheSize = 200;

    public static WallpaperImageCache Instance { get; } = new();

    private WallpaperImageCache() { }

    public BitmapImage? GetFromCache(string url)
    {
        return _cache.TryGetValue(url, out var cached) ? cached : null;
    }

    public async Task<BitmapImage?> GetAsync(string url)
    {
        if (_cache.TryGetValue(url, out var cached))
            return cached;

        try
        {
            var bytes = await _http.GetByteArrayAsync(url);
            var image = new BitmapImage();
            using var stream = new MemoryStream(bytes);
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();

            if (_cache.Count >= MaxCacheSize)
            {
                var oldest = _cache.Keys.Take(_cache.Count / 2).ToList();
                foreach (var key in oldest)
                    _cache.TryRemove(key, out _);
            }

            _cache[url] = image;
            return image;
        }
        catch
        {
            return null;
        }
    }
}
