using CharacterManager.Server.Services;
using Xunit;

namespace CharacterManager.Tests;

public class LanguageContextServiceTests
{
    #region SetLanguageForUser Tests

    [Fact]
    public void SetLanguageForUser_ShouldStoreLanguage()
    {
        // Arrange
        var service = new LanguageContextService();

        // Act
        service.SetLanguageForUser("alice", "en");

        // Assert
        Assert.Equal("en", service.GetLanguageForUser("alice"));
    }

    [Fact]
    public void SetLanguageForUser_ShouldOverwriteExisting()
    {
        // Arrange
        var service = new LanguageContextService();
        service.SetLanguageForUser("alice", "en");

        // Act
        service.SetLanguageForUser("alice", "de");

        // Assert
        Assert.Equal("de", service.GetLanguageForUser("alice"));
    }

    [Fact]
    public void SetLanguageForUser_ShouldHandleNullUsername()
    {
        // Arrange
        var service = new LanguageContextService();

        // Act
        service.SetLanguageForUser(null!, "es");

        // Assert
        Assert.Equal("es", service.GetLanguageForUser(null));
    }

    #endregion

    #region GetLanguageForUser Tests

    [Fact]
    public void GetLanguageForUser_ShouldReturnDefault_WhenUserNotFound()
    {
        // Arrange
        var service = new LanguageContextService();

        // Act
        var result = service.GetLanguageForUser("unknown");

        // Assert
        Assert.Equal("fr", result); // default is "fr"
    }

    [Fact]
    public void GetLanguageForUser_ShouldReturnDefault_WhenNullUsername()
    {
        // Arrange
        var service = new LanguageContextService();

        // Act
        var result = service.GetLanguageForUser(null);

        // Assert
        Assert.Equal("fr", result);
    }

    [Fact]
    public void GetLanguageForUser_ShouldReturnStoredLanguage()
    {
        // Arrange
        var service = new LanguageContextService();
        service.SetLanguageForUser("bob", "it");

        // Act
        var result = service.GetLanguageForUser("bob");

        // Assert
        Assert.Equal("it", result);
    }

    #endregion

    #region SetDefaultLanguage Tests

    [Fact]
    public void SetDefaultLanguage_ShouldChangeDefault()
    {
        // Arrange
        var service = new LanguageContextService();

        // Act
        service.SetDefaultLanguage("en");

        // Assert
        Assert.Equal("en", service.GetDefaultLanguage());
    }

    [Fact]
    public void SetDefaultLanguage_ShouldAffectNewUsers()
    {
        // Arrange
        var service = new LanguageContextService();
        service.SetDefaultLanguage("de");

        // Act
        var result = service.GetLanguageForUser("newuser");

        // Assert
        Assert.Equal("de", result);
    }

    #endregion

    #region GetDefaultLanguage Tests

    [Fact]
    public void GetDefaultLanguage_ShouldReturnFrenchByDefault()
    {
        // Arrange
        var service = new LanguageContextService();

        // Act
        var result = service.GetDefaultLanguage();

        // Assert
        Assert.Equal("fr", result);
    }

    #endregion

    #region ClearCache Tests

    [Fact]
    public void ClearCache_ShouldRemoveAllUserLanguages()
    {
        // Arrange
        var service = new LanguageContextService();
        service.SetLanguageForUser("alice", "en");
        service.SetLanguageForUser("bob", "de");

        // Act
        service.ClearCache();

        // Assert - users should get default now
        Assert.Equal("fr", service.GetLanguageForUser("alice"));
        Assert.Equal("fr", service.GetLanguageForUser("bob"));
    }

    [Fact]
    public void ClearCache_ShouldNotAffectDefaultLanguage()
    {
        // Arrange
        var service = new LanguageContextService();
        service.SetDefaultLanguage("es");
        service.SetLanguageForUser("alice", "en");

        // Act
        service.ClearCache();

        // Assert
        Assert.Equal("es", service.GetDefaultLanguage());
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public async Task Service_ShouldBeThreadSafe()
    {
        // Arrange
        var service = new LanguageContextService();
        var tasks = new List<Task>();

        // Act - read and write from multiple threads
        for (int i = 0; i < 100; i++)
        {
            var username = $"user{i}";
            tasks.Add(Task.Run(() =>
            {
                service.SetLanguageForUser(username, "en");
                _ = service.GetLanguageForUser(username);
            }));
        }

        // Assert - should not throw
        await Task.WhenAll(tasks);
    }

    #endregion
}
