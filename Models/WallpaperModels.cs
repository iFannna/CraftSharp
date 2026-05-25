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

public record WallpaperDetail(string OriginalUrl);

public class WallpaperSettings
{
    public string CurrentWallpaperId { get; set; } = "";
    public string CurrentType { get; set; } = "none";
    public string LocalFilePath { get; set; } = "";
}
