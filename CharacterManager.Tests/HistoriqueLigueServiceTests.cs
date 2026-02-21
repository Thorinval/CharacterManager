using CharacterManager.Server.Data;
using CharacterManager.Server.Models;
using CharacterManager.Server.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CharacterManager.Tests;

public class HistoriqueLigueServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly HistoriqueLigueService _service;

    public HistoriqueLigueServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();
        _service = new HistoriqueLigueService(_context);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnEntriesOrderedByDateThenLigue()
    {
        // Arrange
        var entry1 = new HistoriqueLigue { DateMontee = new DateOnly(2025, 2, 1), Ligue = 1, Notes = "A" };
        var entry2 = new HistoriqueLigue { DateMontee = new DateOnly(2025, 2, 1), Ligue = 3, Notes = "B" };
        var entry3 = new HistoriqueLigue { DateMontee = new DateOnly(2024, 12, 1), Ligue = 2, Notes = "C" };
        _context.HistoriquesLigue.AddRange(entry1, entry2, entry3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Collection(result,
            item =>
            {
                Assert.Equal(3, item.Ligue);
                Assert.Equal(new DateOnly(2025, 2, 1), item.DateMontee);
            },
            item =>
            {
                Assert.Equal(1, item.Ligue);
                Assert.Equal(new DateOnly(2025, 2, 1), item.DateMontee);
            },
            item =>
            {
                Assert.Equal(2, item.Ligue);
                Assert.Equal(new DateOnly(2024, 12, 1), item.DateMontee);
            });
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnMatchingEntity()
    {
        // Arrange
        var historique = new HistoriqueLigue { DateMontee = new DateOnly(2025, 1, 15), Ligue = 5, Notes = "Initial" };
        _context.HistoriquesLigue.Add(historique);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetByIdAsync(historique.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(historique.Id, result!.Id);
        Assert.Equal(5, result.Ligue);
        Assert.Equal(new DateOnly(2025, 1, 15), result.DateMontee);
    }

    [Fact]
    public async Task AddAsync_ShouldPersistEntity()
    {
        // Arrange
        var historique = new HistoriqueLigue { DateMontee = new DateOnly(2025, 3, 10), Ligue = 7, Notes = "Added" };

        // Act
        var created = await _service.AddAsync(historique);

        // Assert
        Assert.True(created.Id > 0);
        var stored = await _context.HistoriquesLigue.FindAsync(created.Id);
        Assert.NotNull(stored);
        Assert.Equal(7, stored!.Ligue);
        Assert.Equal(new DateOnly(2025, 3, 10), stored.DateMontee);
    }

    [Fact]
    public async Task GetHighestLeagueAsync_ShouldReturnEliteWhenPresent()
    {
        // Arrange
        _context.HistoriquesLigue.AddRange(
            new HistoriqueLigue { DateMontee = new DateOnly(2025, 1, 1), Ligue = 12 },
            new HistoriqueLigue { DateMontee = new DateOnly(2025, 1, 2), Ligue = 50 },
            new HistoriqueLigue { DateMontee = new DateOnly(2025, 1, 3), Ligue = 8 });
        await _context.SaveChangesAsync();

        // Act
        var highest = await _service.GetHighestLeagueAsync();

        // Assert
        Assert.Equal(50, highest);
    }

    [Fact]
    public async Task GetHighestLeagueAsync_ShouldReturnLowestNumberWhenNoElite()
    {
        // Arrange
        _context.HistoriquesLigue.AddRange(
            new HistoriqueLigue { DateMontee = new DateOnly(2025, 1, 1), Ligue = 12 },
            new HistoriqueLigue { DateMontee = new DateOnly(2025, 1, 2), Ligue = 5 },
            new HistoriqueLigue { DateMontee = new DateOnly(2025, 1, 3), Ligue = 18 });
        await _context.SaveChangesAsync();

        // Act
        var highest = await _service.GetHighestLeagueAsync();

        // Assert
        Assert.Equal(5, highest);
    }

    [Fact]
    public async Task GetHighestLeagueAsync_ShouldReturnNullWhenNoEntries()
    {
        // Act
        var highest = await _service.GetHighestLeagueAsync();

        // Assert
        Assert.Null(highest);
    }

    [Fact]
    public async Task UpdateAsync_ShouldModifyExistingEntry()
    {
        // Arrange
        var existing = new HistoriqueLigue { DateMontee = new DateOnly(2025, 2, 5), Ligue = 15, Notes = "Old" };
        _context.HistoriquesLigue.Add(existing);
        await _context.SaveChangesAsync();

        var updated = new HistoriqueLigue
        {
            Id = existing.Id,
            DateMontee = new DateOnly(2026, 2, 5),
            Ligue = 9,
            Notes = "Updated"
        };

        // Act
        var result = await _service.UpdateAsync(updated);

        // Assert
        Assert.True(result);
        var stored = await _context.HistoriquesLigue.FindAsync(existing.Id);
        Assert.NotNull(stored);
        Assert.Equal(new DateOnly(2026, 2, 5), stored!.DateMontee);
        Assert.Equal(9, stored.Ligue);
        Assert.Equal("Updated", stored.Notes);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnFalseWhenNotFound()
    {
        // Arrange
        var updated = new HistoriqueLigue
        {
            Id = 999,
            DateMontee = new DateOnly(2026, 2, 5),
            Ligue = 9,
            Notes = "Updated"
        };

        // Act
        var result = await _service.UpdateAsync(updated);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveEntry()
    {
        // Arrange
        var historique = new HistoriqueLigue { DateMontee = new DateOnly(2025, 4, 1), Ligue = 11, Notes = "Delete" };
        _context.HistoriquesLigue.Add(historique);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.DeleteAsync(historique.Id);

        // Assert
        Assert.True(result);
        Assert.Empty(_context.HistoriquesLigue);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnFalseWhenNotFound()
    {
        // Act
        var result = await _service.DeleteAsync(12345);

        // Assert
        Assert.False(result);
    }

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
