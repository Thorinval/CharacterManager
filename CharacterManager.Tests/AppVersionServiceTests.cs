using CharacterManager.Server.Services;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace CharacterManager.Tests;

public class AppVersionServiceTests
{
    private IConfiguration CreateConfiguration(string version = "1.0.0", string name = "Test App", string author = "Test Author")
    {
        var inMemorySettings = new Dictionary<string, string>
        {
            {"AppInfo:Version", version},
            {"AppInfo:Name", name},
            {"AppInfo:Author", author},
            {"AppInfo:Description", "Test Description"}
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings!)
            .Build();
    }

    [Fact]
    public void GetAppVersion_ReturnsConfiguredVersion()
    {
        // Arrange
        var configuration = CreateConfiguration("2.5.1");
        var service = new AppVersionService(configuration);

        // Act
        var version = service.GetAppVersion();

        // Assert
        Assert.Equal("2.5.1", version);
    }

    [Fact]
    public void GetAppVersion_WhenNotConfigured_ReturnsDefault()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();
        var service = new AppVersionService(configuration);

        // Act
        var version = service.GetAppVersion();

        // Assert
        Assert.Equal("1.0.0", version);
    }

    [Fact]
    public void GetAppName_ReturnsConfiguredName()
    {
        // Arrange
        var configuration = CreateConfiguration(name: "Character Manager");
        var service = new AppVersionService(configuration);

        // Act
        var name = service.GetAppName();

        // Assert
        Assert.Equal("Character Manager", name);
    }

    [Fact]
    public void GetAppName_WhenNotConfigured_ReturnsDefault()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();
        var service = new AppVersionService(configuration);

        // Act
        var name = service.GetAppName();

        // Assert
        Assert.Equal("Character Manager", name);
    }

    [Fact]
    public void GetAuthor_ReturnsConfiguredAuthor()
    {
        // Arrange
        var configuration = CreateConfiguration(author: "Thorinval");
        var service = new AppVersionService(configuration);

        // Act
        var author = service.GetAuthor();

        // Assert
        Assert.Equal("Thorinval", author);
    }

    [Fact]
    public void GetDescription_ReturnsConfiguredDescription()
    {
        // Arrange
        var configuration = CreateConfiguration();
        var service = new AppVersionService(configuration);

        // Act
        var description = service.GetDescription();

        // Assert
        Assert.Equal("Test Description", description);
    }

    [Fact]
    public void GetBuildVersion_ReturnsPositiveNumber()
    {
        // Arrange
        var configuration = CreateConfiguration();
        var service = new AppVersionService(configuration);

        // Act
        var buildVersion = service.GetBuildVersion();

        // Assert
        Assert.NotNull(buildVersion);
        Assert.NotEmpty(buildVersion);
        // Le format est "Build {number}" ou un fallback basé sur la date
        // On vérifie juste que ce n'est pas vide et qu'il contient quelque chose
        Assert.True(buildVersion.Length > 0);
    }

    [Fact]
    public void GetGitCommitHash_ReturnsNonEmptyString()
    {
        // Arrange
        var configuration = CreateConfiguration();
        var service = new AppVersionService(configuration);

        // Act
        var commitHash = service.GetGitCommitHash();

        // Assert
        Assert.NotNull(commitHash);
        Assert.NotEmpty(commitHash);
    }

    [Fact]
    public void GetFullVersionString_ContainsVersionAndBuild()
    {
        // Arrange
        var configuration = CreateConfiguration("1.2.3");
        var service = new AppVersionService(configuration);

        // Act
        var fullVersion = service.GetFullVersionString();

        // Assert
        Assert.Contains("1.2.3", fullVersion);
        Assert.Contains("Build", fullVersion);
    }

    [Theory]
    [InlineData("1.0.0")]
    [InlineData("2.5.10")]
    [InlineData("10.20.30")]
    public void GetFullVersionString_FormatsCorrectly(string version)
    {
        // Arrange
        var configuration = CreateConfiguration(version);
        var service = new AppVersionService(configuration);

        // Act
        var fullVersion = service.GetFullVersionString();

        // Assert
        Assert.Matches(@"\d+\.\d+\.\d+ \(Build \d+, [a-f0-9]+\)", fullVersion);
    }
}
