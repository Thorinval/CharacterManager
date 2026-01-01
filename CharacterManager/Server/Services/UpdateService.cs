using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace CharacterManager.Server.Services;

public class UpdateService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly string _currentVersion;
    private readonly string _releasesPath;

    public UpdateService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _currentVersion = configuration["AppInfo:Version"] ?? "1.0.0";
        
        // Chemin relatif au répertoire de travail de l'application
        _releasesPath = Path.Combine(AppContext.BaseDirectory, "..", "Releases", "versions.json");
    }

    public async Task<UpdateInfo?> CheckForUpdatesAsync()
    {
        try
        {
            // Essayer d'abord le fichier local
            if (File.Exists(_releasesPath))
            {
                return await CheckLocalUpdatesAsync();
            }

            // Fallback sur GitHub si le fichier local n'existe pas
            return await CheckGitHubUpdatesAsync();
        }
        catch (Exception)
        {
            return null;
        }
    }

    private async Task<UpdateInfo?> CheckLocalUpdatesAsync()
    {
        try
        {
            var json = await File.ReadAllTextAsync(_releasesPath);
            var versionsList = JsonSerializer.Deserialize<VersionsList>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (versionsList?.Versions == null || versionsList.Versions.Count == 0)
                return null;

            var latestRelease = versionsList.Versions.FirstOrDefault();
            if (latestRelease == null)
                return null;

            var latestVersion = latestRelease.Version;
            if (string.IsNullOrEmpty(latestVersion))
                return null;

            var isNewer = IsNewerVersion(latestVersion, _currentVersion);

            // Résoudre les URLs relatives
            var baseUrl = Path.Combine(AppContext.BaseDirectory, "..", "Releases");
            var downloadUrl = Path.Combine(baseUrl, latestRelease.DownloadUrl ?? "");

            return new UpdateInfo
            {
                CurrentVersion = _currentVersion,
                LatestVersion = latestVersion,
                IsUpdateAvailable = isNewer,
                ReleaseNotes = latestRelease.ReleaseNotes ?? "",
                DownloadUrl = downloadUrl,
                PublishedAt = DateTime.Parse(latestRelease.ReleaseDate ?? DateTime.Now.ToString("yyyy-MM-dd"))
            };
        }
        catch (Exception)
        {
            return null;
        }
    }

    private async Task<UpdateInfo?> CheckGitHubUpdatesAsync()
    {
        try
        {
            var githubRepo = _configuration["AppInfo:GitHubRepo"];
            if (string.IsNullOrEmpty(githubRepo))
                return null;

            // Appel à l'API GitHub pour récupérer la dernière release
            var url = $"https://api.github.com/repos/{githubRepo}/releases/latest";
            
            // Ajouter le User-Agent seulement s'il n'est pas déjà présent
            if (_httpClient.DefaultRequestHeaders.UserAgent.Count == 0)
            {
                _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("CharacterManager");
            }

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();
            var release = JsonSerializer.Deserialize<GitHubRelease>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
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

public class VersionsList
{
    public List<ReleaseVersion>? Versions { get; set; }
    public string? LatestVersion { get; set; }
}

public class ReleaseVersion
{
    public string? Version { get; set; }
    public string? ReleaseDate { get; set; }
    public string? ReleaseNotes { get; set; }
    public string? DownloadUrl { get; set; }
    public string? ChecksumUrl { get; set; }
}
