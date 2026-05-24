using Refit;
using CraftSharp.Models;

namespace CraftSharp.Services.Wallpaper;

public class McBlockApiClient
{
    private readonly IMcBlockApi _api;

    public static McBlockApiClient Instance { get; } = new();

    private McBlockApiClient()
    {
        _api = RestService.For<IMcBlockApi>("https://mcblock.top");
    }

    public async Task<McBlockResponse> GetWallpapersAsync(int page, int limit, string? sort = null, string? type = null)
    {
        return await _api.GetWallpapers("desktop", page, limit, sort, type);
    }
}
