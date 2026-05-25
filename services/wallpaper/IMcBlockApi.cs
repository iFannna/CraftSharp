using Refit;

namespace CraftSharp.Services.Wallpaper;

public interface IMcBlockApi
{
    [Get("/api/wallpapers")]
    Task<Models.McBlockResponse> GetWallpapers(
        [Query] string category,
        [Query] int page,
        [Query] int limit,
        [Query] string? sort = null,
        [Query] string? type = null);

    [Get("/api/wallpapers/{id}")]
    Task<Models.McBlockResponse<Models.WallpaperDetail>> GetWallpaperDetail(string id);
}
