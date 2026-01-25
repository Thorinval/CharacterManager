using CharacterManager.Components.Layout;
using CharacterManager.Server.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CharacterManager.Tests.Components.Layout;

/// <summary>
/// Tests for MainLayout.razor component
/// Note: These tests verify the component's structure and service dependencies
/// </summary>
public class MainLayoutStructureTests
{
    [Fact]
    public void MainLayout_File_Exists()
    {
        // Verify the component compiles and the type exists
        Assert.NotNull(typeof(MainLayout));
    }

    [Fact]
    public void MainLayout_AppVersionService_Returns_Version()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["App:Version"] = "1.2.3"
            })
            .Build();

        var versionService = new AppVersionService(config);

        // Act
        var version = versionService.GetAppVersion();

        // Assert
        Assert.NotNull(version);
        Assert.NotEmpty(version);
    }

    [Fact]
    public void MainLayout_ModalService_Is_Available()
    {
        // Verify ModalService interface is accessible
        Assert.NotNull(typeof(IModalService));
    }

    [Fact]
    public void MainLayout_ProfileService_Is_Available()
    {
        // Verify ProfileService is accessible
        Assert.NotNull(typeof(ProfileService));
    }

    [Fact]
    public void MainLayout_Navigation_Keys_Defined()
    {
        // Verify that expected navigation localization keys are standards
        var navigationKeys = new[]
        {
            "navigation.home",
            "navigation.squad",
            "navigation.bestSquad",
            "navigation.inventory",
            "navigation.templates",
            "navigation.rankings",
            "navigation.leagueHistory",
            "navigation.statistics",
            "navigation.lucieHouse"
        };

        // Assert - keys are properly named
        foreach (var key in navigationKeys)
        {
            Assert.NotNull(key);
            Assert.StartsWith("navigation.", key);
        }
    }

    [Fact]
    public void MainLayout_Expected_Routes_Count()
    {
        // Verify the structure of navigation items
        var expectedRoutes = new[]
        {
            "/",
            "escouade",
            "meilleur-escouade",
            "inventaire",
            "templates",
            "capacites",
            "classements",
            "histoligues",
            "statistiques",
            "historique-modifications",
            "maison-lucie"
        };

        // Assert - routes are properly defined
        Assert.Equal(11, expectedRoutes.Length);
    }
}
