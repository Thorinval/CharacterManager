using CharacterManager.Server.Data;
using CharacterManager.Server.Models;
using CharacterManager.Server.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CharacterManager.Tests;

public class CapaciteServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly CapaciteService _service;

    public CapaciteServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();
        _service = new CapaciteService(_context);
    }

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_ShouldReturnEmptyList_WhenNoCapacites()
    {
        // Act
        var result = await _service.GetAllAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnOrderedByNom()
    {
        // Arrange
        _context.Capacites.AddRange(
            new Capacite { Nom = "Zéro Gravité", Description = "Desc Z" },
            new Capacite { Nom = "Attaque Furtive", Description = "Desc A" },
            new Capacite { Nom = "Mur de Feu", Description = "Desc M" });
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("Attaque Furtive", result[0].Nom);
        Assert.Equal("Mur de Feu", result[1].Nom);
        Assert.Equal("Zéro Gravité", result[2].Nom);
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenNotFound()
    {
        // Act
        var result = await _service.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnCapacite_WhenFound()
    {
        // Arrange
        var capacite = new Capacite { Nom = "Bouclier", Description = "Protection" };
        _context.Capacites.Add(capacite);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetByIdAsync(capacite.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Bouclier", result!.Nom);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ShouldPersistCapacite()
    {
        // Arrange
        var capacite = new Capacite { Nom = "Nova", Description = "Explosion", Icon = "nova.png" };

        // Act
        var created = await _service.CreateAsync(capacite);

        // Assert
        Assert.True(created.Id > 0);
        var stored = await _context.Capacites.FindAsync(created.Id);
        Assert.NotNull(stored);
        Assert.Equal("Nova", stored!.Nom);
        Assert.Equal("Explosion", stored.Description);
        Assert.Equal("nova.png", stored.Icon);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenNomIsEmpty()
    {
        // Arrange
        var capacite = new Capacite { Nom = "", Description = "Test" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(capacite));
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenNomIsWhitespace()
    {
        // Arrange
        var capacite = new Capacite { Nom = "   ", Description = "Test" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(capacite));
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ShouldUpdateExistingCapacite()
    {
        // Arrange
        var capacite = new Capacite { Nom = "Old", Description = "OldDesc", Icon = "old.png" };
        _context.Capacites.Add(capacite);
        await _context.SaveChangesAsync();

        var updated = new Capacite { Nom = "New", Description = "NewDesc", Icon = "new.png" };

        // Act
        var result = await _service.UpdateAsync(capacite.Id, updated);

        // Assert
        Assert.Equal("New", result.Nom);
        Assert.Equal("NewDesc", result.Description);
        Assert.Equal("new.png", result.Icon);
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrow_WhenNotFound()
    {
        // Arrange
        var updated = new Capacite { Nom = "Test", Description = "Desc" };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateAsync(999, updated));
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrow_WhenNomIsEmpty()
    {
        // Arrange
        var capacite = new Capacite { Nom = "Old", Description = "OldDesc" };
        _context.Capacites.Add(capacite);
        await _context.SaveChangesAsync();

        var updated = new Capacite { Nom = "", Description = "NewDesc" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateAsync(capacite.Id, updated));
    }

    [Fact]
    public async Task UpdateAsync_ShouldUseEmptyString_WhenDescriptionIsNull()
    {
        // Arrange
        var capacite = new Capacite { Nom = "Test", Description = "OldDesc" };
        _context.Capacites.Add(capacite);
        await _context.SaveChangesAsync();

        var updated = new Capacite { Nom = "Test", Description = null!, Icon = null! };

        // Act
        var result = await _service.UpdateAsync(capacite.Id, updated);

        // Assert
        Assert.Equal(string.Empty, result.Description);
        Assert.Equal(string.Empty, result.Icon);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ShouldRemoveCapacite()
    {
        // Arrange
        var capacite = new Capacite { Nom = "ToDelete", Description = "Desc" };
        _context.Capacites.Add(capacite);
        await _context.SaveChangesAsync();
        var id = capacite.Id;

        // Act
        await _service.DeleteAsync(id);

        // Assert
        Assert.Null(await _context.Capacites.FindAsync(id));
    }

    [Fact]
    public async Task DeleteAsync_ShouldNotThrow_WhenNotFound()
    {
        // Act & Assert - should not throw
        await _service.DeleteAsync(999);
    }

    #endregion

    #region GetCount Tests

    [Fact]
    public void GetCount_ShouldReturnZero_WhenEmpty()
    {
        // Act
        var count = _service.GetCount();

        // Assert
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task GetCount_ShouldReturnCorrectCount()
    {
        // Arrange
        _context.Capacites.AddRange(
            new Capacite { Nom = "A", Description = "Desc" },
            new Capacite { Nom = "B", Description = "Desc" },
            new Capacite { Nom = "C", Description = "Desc" });
        await _context.SaveChangesAsync();

        // Act
        var count = _service.GetCount();

        // Assert
        Assert.Equal(3, count);
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
