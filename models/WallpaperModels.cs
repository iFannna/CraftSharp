namespace CraftSharp.Models;

public record McBlockResponse<T>(T Data);
public record McBlockResponse(McBlockData Data);

public record McBlockData(List<WallpaperItem> Wallpapers, McBlockPagination Pagination);

public record McBlockPagination(int Page, int Limit, int Total, int TotalPages, bool HasNext, bool HasPrev);

public record WallpaperItem(
    string Id,
    string Title,
    string Description,
    string Type,
    string ThumbnailUrl,
    string PreviewUrl,
    string? PreviewVideoUrl,
    string? OriginalUrl,
    string Resolution,
    string AspectRatio,
    string DominantColor,
    string[] Tags,
    int Downloads,
    int Views,
    int Likes,
    int? Duration,
    bool Featured,
    bool IsLoop
);

public record WallpaperDetail(string OriginalUrl, string? PreviewUrl = null);

public class WallpaperSettings
{
    public string CurrentWallpaperId { get; set; } = "";
    public string CurrentType { get; set; } = "none";
    public string LocalFilePath { get; set; } = "";
    public double VideoVolume { get; set; } = 0.0;
    public bool VideoMuted { get; set; } = true;

    /// <summary>
    /// 多显示器模式：independent（每屏独立）| span（跨屏拼接）
    /// </summary>
    public string Mode { get; set; } = "independent";

    /// <summary>
    /// 每显示器壁纸条目，key 为显示器设备路径
    /// </summary>
    public Dictionary<string, MonitorWallpaperEntry> Monitors { get; set; } = new();

    /// <summary>
    /// 跨屏拼接模式的壁纸条目
    /// </summary>
    public SpanWallpaperEntry? Span { get; set; }
}

public class MonitorWallpaperEntry
{
    public string MonitorId { get; set; } = "";
    public string WallpaperId { get; set; } = "";
    public string Type { get; set; } = "none";
    /// <summary>静态：图片路径；动态：视频路径</summary>
    public string LocalFilePath { get; set; } = "";
    /// <summary>动态壁纸的静态回退图路径</summary>
    public string PreviewPath { get; set; } = "";
}

public class SpanWallpaperEntry
{
    public string WallpaperId { get; set; } = "";
    public string Type { get; set; } = "none";
    /// <summary>原始宽图/宽视频路径</summary>
    public string LocalFilePath { get; set; } = "";
    /// <summary>拼接裁切源图路径（静态=原图，动态=预览图）</summary>
    public string PreviewPath { get; set; } = "";
}
