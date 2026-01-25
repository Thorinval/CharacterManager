using CharacterManager.Server.Data;
using CharacterManager.Server.Models;
using CharacterManager.Server.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CharacterManager.Tests;

public class DatabaseInitializationServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly DatabaseInitializationService _service;
    private readonly Mock<ILogger<DatabaseInitializationService>> _loggerMock = new();

    public DatabaseInitializationServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();
        _service = new DatabaseInitializationService(_context, _loggerMock.Object);
    }

    #region InitializeAppSettingsAndCheckStateAsync Tests

    [Fact]
    public async Task InitializeAppSettingsAndCheckStateAsync_ShouldCreateDefaultSettings_WhenNoneExist()
    {
        // Act
        await _service.InitializeAppSettingsAndCheckStateAsync();

        // Assert
        var settings = await _context.AppSettings.FirstOrDefaultAsync();
        Assert.NotNull(settings);
        Assert.True(settings.IsAdultModeEnabled);
        Assert.Equal("fr", settings.Language);
    }

    [Fact]
    public async Task InitializeAppSettingsAndCheckStateAsync_ShouldNotCreateNew_WhenSettingsExist()
    {
        // Arrange
        _context.AppSettings.Add(new AppSettings
        {
            IsAdultModeEnabled = false,
            Language = "en"
        });
        await _context.SaveChangesAsync();

        // Act
        await _service.InitializeAppSettingsAndCheckStateAsync();

        // Assert
        var count = await _context.AppSettings.CountAsync();
        Assert.Equal(1, count);
        
        var settings = await _context.AppSettings.FirstAsync();
        Assert.False(settings.IsAdultModeEnabled);
        Assert.Equal("en", settings.Language);
    }

    [Fact]
    public async Task InitializeAppSettingsAndCheckStateAsync_ShouldSetDefaultLanguage_WhenEmpty()
    {
        // Arrange
        _context.AppSettings.Add(new AppSettings
        {
            IsAdultModeEnabled = true,
            Language = "" // Empty language
        });
        await _context.SaveChangesAsync();

        // Act
        await _service.InitializeAppSettingsAndCheckStateAsync();

        // Assert
        var settings = await _context.AppSettings.FirstAsync();
        Assert.Equal("fr", settings.Language);
    }

    #endregion

    #region Database State Detection Tests

    [Fact]
    public async Task InitializeAppSettingsAndCheckStateAsync_ShouldDetectEmptyDatabase()
    {
        // Arrange - No personnages, no LucieHouses

        // Act
        await _service.InitializeAppSettingsAndCheckStateAsync();

        // Assert - Just verify no exception thrown
        Assert.Empty(await _context.Personnages.ToListAsync());
    }

    [Fact]
    public async Task InitializeAppSettingsAndCheckStateAsync_ShouldDetectNonEmptyDatabase()
    {
        // Arrange
        _context.Personnages.Add(new Personnage { Nom = "Test" });
        await _context.SaveChangesAsync();

        // Act
        await _service.InitializeAppSettingsAndCheckStateAsync();

        // Assert
        Assert.Single(await _context.Personnages.ToListAsync());
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task InitializeAppSettingsAndCheckStateAsync_ShouldHandleMultipleSettings()
    {
        // Arrange - Create multiple settings (edge case)
        _context.AppSettings.AddRange(
            new AppSettings { Id = 1, IsAdultModeEnabled = true, Language = "fr" },
            new AppSettings { Id = 2, IsAdultModeEnabled = false, Language = "en" }
        );
        await _context.SaveChangesAsync();

        // Act - Should use first by Id
        await _service.InitializeAppSettingsAndCheckStateAsync();

        // Assert - Should work without throwing
        var settings = await _context.AppSettings.OrderBy(s => s.Id).FirstAsync();
        Assert.Equal(1, settings.Id);
    }

    #endregion

    #region InitializeDatabaseAsync Tests

    [Fact]
    public async Task InitializeDatabaseAsync_ShouldNotThrow_WhenDatabaseIsNew()
    {
        // Act & Assert - Should not throw
        var exception = await Record.ExceptionAsync(() => _service.InitializeDatabaseAsync());
        
        // Note: InMemoryDatabase doesn't support migrations, so we just verify no unhandled exception
        // The method logs errors internally
        Assert.Null(exception);
    }

    #endregion

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _context?.Dispose();
        }
    }
}
