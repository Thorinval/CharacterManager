using CharacterManager.Services;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;
using Xunit;

namespace CharacterManager.Tests;

public class UpdateServiceTests
{
    private IConfiguration CreateConfiguration(string version = "1.0.0", string githubRepo = "owner/repo")
    {
        var inMemorySettings = new Dictionary<string, string>
        {
            {"AppInfo:Version", version},
            {"AppInfo:GitHubRepo", githubRepo}
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings!)
            .Build();
    }

    private HttpClient CreateMockHttpClient(HttpStatusCode statusCode, string responseContent)
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(responseContent)
            });

        return new HttpClient(mockHandler.Object);
    }

    [Fact]
    public async Task CheckForUpdatesAsync_WhenNoGitHubRepo_ReturnsNull()
    {
        // Arrange
        var configuration = CreateConfiguration(githubRepo: "");
        var httpClient = new HttpClient();
        var service = new UpdateService(httpClient, configuration);

        // Act
        var result = await service.CheckForUpdatesAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CheckForUpdatesAsync_WhenApiReturnsError_ReturnsNull()
    {
        // Arrange
        var configuration = CreateConfiguration();
        var httpClient = CreateMockHttpClient(HttpStatusCode.NotFound, "");
        var service = new UpdateService(httpClient, configuration);

        // Act
        var result = await service.CheckForUpdatesAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CheckForUpdatesAsync_WhenNewerVersionExists_ReturnsUpdateInfo()
    {
        // Arrange
        var configuration = CreateConfiguration("1.0.0");
        var releaseJson = JsonSerializer.Serialize(new
        {
            tag_name = "v2.0.0",
            name = "Version 2.0.0",
            body = "New features",
            html_url = "https://github.com/owner/repo/releases/tag/v2.0.0",
            published_at = DateTime.UtcNow,
            assets = new[]
            {
                new
                {
                    name = "app-v2.0.0.zip",
                    browser_download_url = "https://github.com/owner/repo/releases/download/v2.0.0/app.zip",
                    size = 1024
                }
            }
        });

        var httpClient = CreateMockHttpClient(HttpStatusCode.OK, releaseJson);
        var service = new UpdateService(httpClient, configuration);

        // Act
        var result = await service.CheckForUpdatesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsUpdateAvailable);
        Assert.Equal("1.0.0", result.CurrentVersion);
        Assert.Equal("2.0.0", result.LatestVersion);
    }

    [Fact]
    public async Task CheckForUpdatesAsync_WhenSameVersion_ReturnsNoUpdate()
    {
        // Arrange
        var configuration = CreateConfiguration("1.0.0");
        var releaseJson = JsonSerializer.Serialize(new
        {
            tag_name = "v1.0.0",
            name = "Version 1.0.0",
            body = "Current version",
            html_url = "https://github.com/owner/repo/releases/tag/v1.0.0",
            published_at = DateTime.UtcNow,
            assets = Array.Empty<object>()
        });

        var httpClient = CreateMockHttpClient(HttpStatusCode.OK, releaseJson);
        var service = new UpdateService(httpClient, configuration);

        // Act
        var result = await service.CheckForUpdatesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsUpdateAvailable);
        Assert.Equal("1.0.0", result.CurrentVersion);
        Assert.Equal("1.0.0", result.LatestVersion);
    }

    [Fact]
    public async Task CheckForUpdatesAsync_WhenOlderVersion_ReturnsNoUpdate()
    {
        // Arrange
        var configuration = CreateConfiguration("2.0.0");
        var releaseJson = JsonSerializer.Serialize(new
        {
            tag_name = "v1.0.0",
            name = "Version 1.0.0",
            body = "Old version",
            html_url = "https://github.com/owner/repo/releases/tag/v1.0.0",
            published_at = DateTime.UtcNow,
            assets = Array.Empty<object>()
        });

        var httpClient = CreateMockHttpClient(HttpStatusCode.OK, releaseJson);
        var service = new UpdateService(httpClient, configuration);

        // Act
        var result = await service.CheckForUpdatesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsUpdateAvailable);
        Assert.Equal("2.0.0", result.CurrentVersion);
        Assert.Equal("1.0.0", result.LatestVersion);
    }

    [Theory]
    [InlineData("1.0.0", "1.0.1", true)]
    [InlineData("1.0.0", "1.1.0", true)]
    [InlineData("1.0.0", "2.0.0", true)]
    [InlineData("1.1.0", "1.0.9", false)]
    [InlineData("2.0.0", "1.9.9", false)]
    [InlineData("1.5.3", "1.5.3", false)]
    public async Task CheckForUpdatesAsync_VersionComparison_WorksCorrectly(
        string currentVersion, string latestVersion, bool shouldUpdate)
    {
        // Arrange
        var configuration = CreateConfiguration(currentVersion);
        var releaseJson = JsonSerializer.Serialize(new
        {
            tag_name = $"v{latestVersion}",
            name = $"Version {latestVersion}",
            body = "Release notes",
            html_url = $"https://github.com/owner/repo/releases/tag/v{latestVersion}",
            published_at = DateTime.UtcNow,
            assets = Array.Empty<object>()
        });

        var httpClient = CreateMockHttpClient(HttpStatusCode.OK, releaseJson);
        var service = new UpdateService(httpClient, configuration);

        // Act
        var result = await service.CheckForUpdatesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(shouldUpdate, result.IsUpdateAvailable);
        Assert.Equal(currentVersion, result.CurrentVersion);
        Assert.Equal(latestVersion, result.LatestVersion);
    }

    [Fact]
    public async Task CheckForUpdatesAsync_ExtractsReleaseNotes()
    {
        // Arrange
        var configuration = CreateConfiguration("1.0.0");
        var releaseNotes = "- Feature 1\n- Feature 2\n- Bug fix";
        var releaseJson = JsonSerializer.Serialize(new
        {
            tag_name = "v2.0.0",
            body = releaseNotes,
            html_url = "https://github.com/owner/repo/releases/tag/v2.0.0",
            published_at = DateTime.UtcNow,
            assets = Array.Empty<object>()
        });

        var httpClient = CreateMockHttpClient(HttpStatusCode.OK, releaseJson);
        var service = new UpdateService(httpClient, configuration);

        // Act
        var result = await service.CheckForUpdatesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(releaseNotes, result.ReleaseNotes);
    }

    [Fact]
    public async Task CheckForUpdatesAsync_ExtractsDownloadUrl()
    {
        // Arrange
        var configuration = CreateConfiguration("1.0.0");
        var downloadUrl = "https://github.com/owner/repo/releases/download/v2.0.0/app-v2.0.0.zip";
        var releaseJson = JsonSerializer.Serialize(new
        {
            tag_name = "v2.0.0",
            html_url = "https://github.com/owner/repo/releases/tag/v2.0.0",
            published_at = DateTime.UtcNow,
            assets = new[]
            {
                new
                {
                    name = "app-v2.0.0.zip",
                    browser_download_url = downloadUrl,
                    size = 1024
                }
            }
        });

        var httpClient = CreateMockHttpClient(HttpStatusCode.OK, releaseJson);
        var service = new UpdateService(httpClient, configuration);

        // Act
        var result = await service.CheckForUpdatesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(downloadUrl, result.DownloadUrl);
    }
}
