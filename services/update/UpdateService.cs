using System.Net.Http;
using System.Reflection;
using System.Text.Json.Serialization;

namespace CraftSharp.Services.Update;

public record GitHubRelease(string TagName, string HtmlUrl, string Body);

public record GitHubApiResponse(
    [property: JsonPropertyName("tag_name")] string TagName,
    [property: JsonPropertyName("html_url")] string HtmlUrl,
    [property: JsonPropertyName("body")] string Body);

public class UpdateService
{
    private static readonly HttpClient Http = new()
    {
        DefaultRequestHeaders = { { "User-Agent", "CraftSharp" } }
    };

    public static UpdateService Instance { get; } = new();
    private UpdateService() { }

    private const string RepoApiUrl = "https://api.github.com/repos/iFannna/CraftSharp/releases/latest";
    private const string ReleaseUrl = "https://github.com/iFannna/CraftSharp/releases/latest";

    public static Version CurrentVersion =>
        Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(0, 0, 0);

    public async Task<GitHubRelease?> CheckForUpdateAsync()
    {
        try
        {
            var json = await Http.GetStringAsync(RepoApiUrl);
            var apiResponse = System.Text.Json.JsonSerializer.Deserialize<GitHubApiResponse>(json);
            if (apiResponse == null) return null;

            var latestVersion = ParseVersion(apiResponse.TagName);
            if (latestVersion == null || latestVersion <= CurrentVersion) return null;

            return new GitHubRelease(apiResponse.TagName, apiResponse.HtmlUrl, apiResponse.Body);
        }
        catch
        {
            return null;
        }
    }

    public void OpenReleasePage() =>
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = ReleaseUrl,
            UseShellExecute = true
        });

    private static Version? ParseVersion(string tagName)
    {
        var v = tagName.TrimStart('v', 'V');
        return Version.TryParse(v, out var result) ? result : null;
    }
}
