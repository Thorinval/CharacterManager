using CharacterManager.Server.Services;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Xunit;

namespace CharacterManager.Tests;

public class LocalizationServiceTests : IDisposable
{
    private readonly string _i18nPath;
    private readonly Mock<ILogger<LocalizationService>> _loggerMock = new();

    public LocalizationServiceTests()
    {
        _i18nPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "i18n");
        Directory.CreateDirectory(_i18nPath);
    }

    [Fact]
    public async Task LoadLanguageAsync_ShouldLogWarningAndReturnEmpty_WhenFileMissing()
    {
        // Arrange
        var service = new LocalizationService(_loggerMock.Object);
        var missingFile = Path.Combine(_i18nPath, "missing-lang.json");
        if (File.Exists(missingFile))
        {
            File.Delete(missingFile);
        }

        // Act
        var resources = await service.LoadLanguageAsync("missing-lang");

        // Assert
        Assert.Empty(resources);
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("non trouvé")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task LoadLanguageAsync_ShouldCacheResultsUntilCleared()
    {
        // Arrange
        var service = new LocalizationService(_loggerMock.Object);
        var filePath = Path.Combine(_i18nPath, "cache-lang.json");
        await File.WriteAllTextAsync(filePath, "{\"greeting\":\"hello\"}");

        // Act
        var first = await service.LoadLanguageAsync("cache-lang");
        await File.WriteAllTextAsync(filePath, "{\"greeting\":\"changed\"}");
        var second = await service.LoadLanguageAsync("cache-lang");
        service.ClearCache();
        var third = await service.LoadLanguageAsync("cache-lang");

        // Assert
        Assert.Equal("hello", LocalizationService.GetString(first, "greeting"));
        Assert.Equal("hello", LocalizationService.GetString(second, "greeting")); // cached
        Assert.Equal("changed", LocalizationService.GetString(third, "greeting")); // reload after clear
    }

    [Fact]
    public void GetString_ShouldResolveNestedKeysAndFallback()
    {
        // Arrange
        var json = "{\"errors\":{\"notFound\":\"Missing\"},\"simple\":\"Value\"}";
        var resources = JsonSerializer.Deserialize<Dictionary<string, object>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        })!;

        // Act
        var nested = LocalizationService.GetString(resources, "errors.notFound");
        var simple = LocalizationService.GetString(resources, "simple");
        var missing = LocalizationService.GetString(resources, "errors.unknown");

        // Assert
        Assert.Equal("Missing", nested);
        Assert.Equal("Value", simple);
        Assert.Equal("errors.unknown", missing);
    }

    [Fact]
    public void GetAvailableLanguages_ShouldReturnDefaults()
    {
        // Arrange
        var service = new LocalizationService(_loggerMock.Object);

        // Act
        var languages = service.GetAvailableLanguages();

        // Assert
        Assert.Collection(languages,
            l => { Assert.Equal("fr", l.Code); Assert.Equal("Français", l.Name); },
            l => { Assert.Equal("en", l.Code); Assert.Equal("English", l.Name); });
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing && Directory.Exists(_i18nPath))
        {
            foreach (var file in Directory.GetFiles(_i18nPath, "*-lang.json"))
            {
                try
                {
                    File.Delete(file);
                }
                catch (IOException)
                {
                    // File may be locked by another test process - ignore
                }
            }
        }
    }
}
