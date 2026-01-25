using CharacterManager.Scripts;
using CharacterManager.Server.Data;
using CharacterManager.Server.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CharacterManager.Tests;

public class CleanupPersonnageDuplicatesTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly CleanupPersonnageDuplicates _service;

    public CleanupPersonnageDuplicatesTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();
        _service = new CleanupPersonnageDuplicates(_context);
    }

    #region ExecuteAsync DryRun Tests

    [Fact]
    public async Task ExecuteAsync_DryRun_ShouldNotModifyData()
    {
        // Arrange - create duplicates
        _context.Personnages.AddRange(
            new Personnage { Id = 1, Nom = "Alex" },
            new Personnage { Id = 2, Nom = "ALEX" },
            new Personnage { Id = 3, Nom = "alex" }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ExecuteAsync(dryRun: true);

        // Assert
        Assert.Equal(1, result.DuplicatesFound);
        Assert.Equal(0, result.PersonnagesDeleted);
        Assert.Equal(3, await _context.Personnages.CountAsync());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnZeroDuplicates_WhenNoDuplicates()
    {
        // Arrange
        _context.Personnages.AddRange(
            new Personnage { Id = 1, Nom = "Alex" },
            new Personnage { Id = 2, Nom = "Bob" },
            new Personnage { Id = 3, Nom = "Charlie" }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ExecuteAsync(dryRun: true);

        // Assert
        Assert.Equal(0, result.DuplicatesFound);
    }

    #endregion

    #region ExecuteAsync Real Run Tests

    [Fact]
    public async Task ExecuteAsync_ShouldDeleteDuplicates_WhenNotDryRun()
    {
        // Arrange - create duplicates (lower ID should be kept)
        _context.Personnages.AddRange(
            new Personnage { Id = 1, Nom = "Alex" },
            new Personnage { Id = 2, Nom = "ALEX" }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ExecuteAsync(dryRun: false);

        // Assert
        Assert.Equal(1, result.DuplicatesFound);
        Assert.Equal(1, result.PersonnagesDeleted);
        Assert.Equal(1, await _context.Personnages.CountAsync());
        
        var remaining = await _context.Personnages.FirstAsync();
        Assert.Equal(1, remaining.Id); // Lowest ID should be kept
    }

    [Fact]
    public async Task ExecuteAsync_ShouldKeepLowestId()
    {
        // Arrange
        _context.Personnages.AddRange(
            new Personnage { Id = 10, Nom = "Test" },
            new Personnage { Id = 5, Nom = "test" },
            new Personnage { Id = 20, Nom = "TEST" }
        );
        await _context.SaveChangesAsync();

        // Act
        await _service.ExecuteAsync(dryRun: false);

        // Assert
        var remaining = await _context.Personnages.SingleAsync();
        Assert.Equal(5, remaining.Id);
    }

    #endregion

    #region HistoriquesModifications Update Tests

    [Fact]
    public async Task ExecuteAsync_ShouldUpdateHistoriques_WhenDuplicatesDeleted()
    {
        // Arrange
        _context.Personnages.AddRange(
            new Personnage { Id = 1, Nom = "Alex" },
            new Personnage { Id = 2, Nom = "ALEX" }
        );
        _context.HistoriquesModifications.Add(new HistoriqueModification
        {
            TypeEntite = TypeEntite.Personnage,
            EntiteId = 2, // Points to duplicate that will be deleted
            TypeModification = TypeModification.Modification,
            DateModification = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ExecuteAsync(dryRun: false);

        // Assert
        Assert.Equal(1, result.HistoriquesUpdated);
        var historique = await _context.HistoriquesModifications.FirstAsync();
        Assert.Equal(1, historique.EntiteId); // Should now point to kept ID
    }

    #endregion

    #region Templates Update Tests

    [Fact]
    public async Task ExecuteAsync_ShouldUpdateTemplates_WhenDuplicatesDeleted()
    {
        // Arrange
        _context.Personnages.AddRange(
            new Personnage { Id = 1, Nom = "Alex" },
            new Personnage { Id = 2, Nom = "ALEX" }
        );
        var template = new Template
        {
            Nom = "Test Template",
            DateCreation = DateTime.UtcNow
        };
        template.SetPersonnageIds(new List<int> { 2 }); // References duplicate
        _context.Templates.Add(template);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ExecuteAsync(dryRun: false);

        // Assert
        Assert.Equal(1, result.TemplatesUpdated);
        var updatedTemplate = await _context.Templates.FirstAsync();
        Assert.Contains(1, updatedTemplate.GetPersonnageIds()); // Should now reference kept ID
    }

    #endregion

    #region Multiple Duplicate Groups Tests

    [Fact]
    public async Task ExecuteAsync_ShouldHandleMultipleGroups()
    {
        // Arrange - two separate duplicate groups
        _context.Personnages.AddRange(
            new Personnage { Id = 1, Nom = "Alex" },
            new Personnage { Id = 2, Nom = "ALEX" },
            new Personnage { Id = 3, Nom = "Bob" },
            new Personnage { Id = 4, Nom = "BOB" },
            new Personnage { Id = 5, Nom = "Charlie" } // No duplicate
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ExecuteAsync(dryRun: false);

        // Assert
        Assert.Equal(2, result.DuplicatesFound);
        Assert.Equal(2, result.PersonnagesDeleted);
        Assert.Equal(3, await _context.Personnages.CountAsync());
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task ExecuteAsync_ShouldHandleEmptyDatabase()
    {
        // Act
        var result = await _service.ExecuteAsync(dryRun: false);

        // Assert
        Assert.Equal(0, result.DuplicatesFound);
        Assert.Equal(0, result.PersonnagesDeleted);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldHandleSinglePersonnage()
    {
        // Arrange
        _context.Personnages.Add(new Personnage { Id = 1, Nom = "Solo" });
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ExecuteAsync(dryRun: false);

        // Assert
        Assert.Equal(0, result.DuplicatesFound);
        Assert.Equal(1, await _context.Personnages.CountAsync());
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
