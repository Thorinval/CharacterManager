using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace CharacterManager.Services;

public class UpdateService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly string _currentVersion;

    public UpdateService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _currentVersion = configuration["AppInfo:Version"] ?? "1.0.0";
    }

    public async Task<UpdateInfo?> CheckForUpdatesAsync()
    {
        try
        {
            var githubRepo = _configuration["AppInfo:GitHubRepo"];
            if (string.IsNullOrEmpty(githubRepo))
                return null;

            // Appel à l'API GitHub pour récupérer la dernière release
            var url = $"https://api.github.com/repos/{githubRepo}/releases/latest";
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("CharacterManager");

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();
            var release = JsonSerializer.Deserialize<GitHubRelease>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (release == null)
                return null;

            var latestVersion = release.TagName?.TrimStart('v');
            if (string.IsNullOrEmpty(latestVersion))
                return null;

            // Comparer les versions
            var isNewer = IsNewerVersion(latestVersion, _currentVersion);

            return new UpdateInfo
            {
                CurrentVersion = _currentVersion,
                LatestVersion = latestVersion,
                IsUpdateAvailable = isNewer,
                ReleaseNotes = release.Body ?? "",
                DownloadUrl = release.Assets?.FirstOrDefault(a => a.Name?.EndsWith(".zip") == true)?.BrowserDownloadUrl ?? release.HtmlUrl ?? "",
                PublishedAt = release.PublishedAt
            };
        }
        catch (Exception)
        {
            return null;
        }
    }

    private bool IsNewerVersion(string latest, string current)
    {
        try
        {
            var latestParts = latest.Split('.').Select(int.Parse).ToArray();
            var currentParts = current.Split('.').Select(int.Parse).ToArray();

            for (int i = 0; i < Math.Max(latestParts.Length, currentParts.Length); i++)
            {
                var latestPart = i < latestParts.Length ? latestParts[i] : 0;
                var currentPart = i < currentParts.Length ? currentParts[i] : 0;

                if (latestPart > currentPart)
                    return true;
                if (latestPart < currentPart)
                    return false;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }
}

public class UpdateInfo
{
    public string CurrentVersion { get; set; } = "";
    public string LatestVersion { get; set; } = "";
    public bool IsUpdateAvailable { get; set; }
    public string ReleaseNotes { get; set; } = "";
    public string DownloadUrl { get; set; } = "";
    public DateTime PublishedAt { get; set; }
}

public class GitHubRelease
{
    public string? TagName { get; set; }
    public string? Name { get; set; }
    public string? Body { get; set; }
    public string? HtmlUrl { get; set; }
    public DateTime PublishedAt { get; set; }
    public List<GitHubAsset>? Assets { get; set; }
}

public class GitHubAsset
{
    public string? Name { get; set; }
    public string? BrowserDownloadUrl { get; set; }
    public long Size { get; set; }
}
