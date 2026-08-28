using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CraftSharp.Helpers;
using CraftSharp.Models;
using ImageMagick;

namespace CraftSharp.Services.Wallpaper;

/// <summary>
/// 跨屏拼接裁切服务 - 将一张宽图按各显示器虚拟桌面矩形裁成每屏一份，
/// 输出精确物理分辨率的 JPG（规避 webp 依赖 WIC 解码器问题），
/// 系统侧以 Fill 方式呈现时不会再有二次缩放，杜绝跨屏接缝误差。
/// </summary>
public class SpanCropService
{
    public static SpanCropService Instance { get; } = new();

    private SpanCropService() { }

    private static string SpanRootDir => Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "assets", "wallpaper", "span");

    public async Task<List<(MonitorInfo Monitor, string CropPath)>> ProduceCropsAsync(
        string sourceImagePath,
        string wallpaperId,
        List<MonitorInfo> monitors,
        string topologyFingerprint)
    {
        var dir = Path.Combine(SpanRootDir, Sanitize(wallpaperId));
        var manifestPath = Path.Combine(dir, "manifest.txt");
        var targets = monitors
            .Select(m => (Monitor: m, CropPath: Path.Combine(dir, $"{Sanitize(m.DevicePath)}.jpg")))
            .ToList();

        // 缓存命中：指纹匹配且所有裁片存在
        if (File.Exists(manifestPath)
            && File.ReadAllText(manifestPath).Trim() == topologyFingerprint
            && targets.All(t => File.Exists(t.CropPath)))
        {
            return targets;
        }

        Directory.CreateDirectory(dir);
        var results = await Task.Run(() =>
        {
            var virtualBounds = VirtualBounds(monitors);
            int vw = virtualBounds.Right - virtualBounds.Left;
            int vh = virtualBounds.Bottom - virtualBounds.Top;

            var output = new List<(MonitorInfo, string)>();
            using var image = new MagickImage(sourceImagePath);
            double imgW = image.Width;
            double imgH = image.Height;
            if (imgW <= 0 || imgH <= 0)
                throw new InvalidOperationException($"Invalid image: {sourceImagePath}");

            // cover 缩放：等价 DWPOS_FILL
            double s = Math.Max(vw / imgW, vh / imgH);
            double ox = (imgW * s - vw) / 2;
            double oy = (imgH * s - vh) / 2;

            foreach (var (monitor, cropPath) in targets)
            {
                // 屏矩形相对虚拟屏原点
                double rx = monitor.Bounds.Left - virtualBounds.Left;
                double ry = monitor.Bounds.Top - virtualBounds.Top;
                int rw = monitor.Bounds.Right - monitor.Bounds.Left;
                int rh = monitor.Bounds.Bottom - monitor.Bounds.Top;

                int cropX = Math.Max(0, (int)Math.Round((rx + ox) / s));
                int cropY = Math.Max(0, (int)Math.Round((ry + oy) / s));
                int cropW = Math.Min((int)Math.Round(rw / s), (int)imgW - cropX);
                int cropH = Math.Min((int)Math.Round(rh / s), (int)imgH - cropY);
                if (cropW <= 0 || cropH <= 0)
                    throw new InvalidOperationException($"Crop rect out of bounds for {monitor.DevicePath}");

                using var clone = image.Clone();
                clone.Crop(new MagickGeometry(cropX, cropY, (uint)cropW, (uint)cropH));
                clone.Page = new MagickGeometry(0, 0, clone.Width, clone.Height);
                clone.Resize((uint)rw, (uint)rh);
                clone.Quality = 92;
                clone.Write(cropPath);
                output.Add((monitor, cropPath));
            }

            return output;
        });

        await File.WriteAllTextAsync(manifestPath, topologyFingerprint);
        CleanupStaleCropDirs(wallpaperId);
        Debug.WriteLine($"[SpanCrop] Produced {results.Count} crops for {wallpaperId} ({topologyFingerprint})");
        return results;
    }

    private static Win32Helper.RECT VirtualBounds(List<MonitorInfo> monitors) => new()
    {
        Left = monitors.Min(m => m.Bounds.Left),
        Top = monitors.Min(m => m.Bounds.Top),
        Right = monitors.Max(m => m.Bounds.Right),
        Bottom = monitors.Max(m => m.Bounds.Bottom)
    };

    /// <summary>
    /// 裁片生成后清理其他 wallpaperId 的残留目录，控制磁盘占用
    /// </summary>
    private static void CleanupStaleCropDirs(string keepWallpaperId)
    {
        try
        {
            var keep = Sanitize(keepWallpaperId);
            var root = SpanRootDir;
            if (!Directory.Exists(root)) return;
            foreach (var dir in Directory.GetDirectories(root))
            {
                if (Path.GetFileName(dir) != keep)
                    Directory.Delete(dir, recursive: true);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SpanCrop] Cleanup failed: {ex.Message}");
        }
    }

    private static string Sanitize(string value) =>
        Regex.Replace(value, "[^A-Za-z0-9_-]", "_");
}
