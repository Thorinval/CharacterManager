using CharacterManager.Server.Data;
using CharacterManager.Server.Models;
using CharacterManager.Server.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Moq;
using Microsoft.AspNetCore.Http;

namespace CharacterManager.Tests;

public class PersonnageServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly PersonnageService _service;

    public PersonnageServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();

        // Create a real HistoriqueModificationService for integration tests
        var historiqueService = new HistoriqueModificationService(_context);
        _service = new PersonnageService(_context, historiqueService);
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

    [Fact]
    public void GetPuissanceLucieEscouade_WithSelectedPieces_ShouldCalculateCorrectly()
    {
        // Arrange
        var lucieHouse = new LucieHouse { Affection = 50 };
        _context.LucieHouses.Add(lucieHouse);
        _context.SaveChanges();

        var piece1 = new Piece
        {
            Nom = "Salle",
            Niveau = 1,
            Selectionnee = true,
            BonusTactiquesSerialized = "[]",
            BonusStrategiquesSerialized = "[]",
            AspectsTactiques = new() { Nom = "Tactiques", Puissance = 50, Bonus = new() },
            AspectsStrategiques = new() { Nom = "Strategiques", Puissance = 30, Bonus = new() }
        };

        var piece2 = new Piece
        {
            Nom = "Cuisine",
            Niveau = 2,
            Selectionnee = true,
            BonusTactiquesSerialized = "[]",
            BonusStrategiquesSerialized = "[]",
            AspectsTactiques = new() { Nom = "Tactiques", Puissance = 40, Bonus = new() },
            AspectsStrategiques = new() { Nom = "Strategiques", Puissance = 25, Bonus = new() }
        };

        var lucieHouse1 = _context.LucieHouses.First();
        piece1.GetType().GetProperty("LucieHouseId")?.SetValue(piece1, lucieHouse1.Id);
        piece2.GetType().GetProperty("LucieHouseId")?.SetValue(piece2, lucieHouse1.Id);

        _context.Pieces.AddRange(piece1, piece2);
        _context.SaveChanges();

        // Act
        int result = _service.GetPuissanceLucieEscouade();

        // Assert - 50 + 40 + (30 + 25) = 145
        Assert.Equal(145, result);
    }

    [Fact]
    public void GetPuissanceLucieEscouade_WithNoSelectedPieces_ShouldReturnZero()
    {
        // Arrange
        var lucieHouse = new LucieHouse { Affection = 50 };
        _context.LucieHouses.Add(lucieHouse);
        _context.SaveChanges();

        // Act
        int result = _service.GetPuissanceLucieEscouade();

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void GetPuissanceMaxLucieEscouade_ShouldCalculateTopTwoPiecesAndStrategic()
    {
        // Arrange
        var lucieHouse = new LucieHouse { Affection = 50 };
        _context.LucieHouses.Add(lucieHouse);
        _context.SaveChanges();

        // Top 2 pièces
        var piece1 = new Piece
        {
            Nom = "Gymnase",
            Niveau = 3,
            Selectionnee = false,
            BonusTactiquesSerialized = "[]",
            BonusStrategiquesSerialized = "[]",
            AspectsTactiques = new() { Nom = "Tactiques", Puissance = 100, Bonus = new() },
            AspectsStrategiques = new() { Nom = "Strategiques", Puissance = 50, Bonus = new() }
        };

        var piece2 = new Piece
        {
            Nom = "Bibliothèque",
            Niveau = 2,
            Selectionnee = false,
            BonusTactiquesSerialized = "[]",
            BonusStrategiquesSerialized = "[]",
            AspectsTactiques = new() { Nom = "Tactiques", Puissance = 80, Bonus = new() },
            AspectsStrategiques = new() { Nom = "Strategiques", Puissance = 40, Bonus = new() }
        };

        var piece3 = new Piece
        {
            Nom = "Chambre",
            Niveau = 1,
            Selectionnee = false,
            BonusTactiquesSerialized = "[]",
            BonusStrategiquesSerialized = "[]",
            AspectsTactiques = new() { Nom = "Tactiques", Puissance = 60, Bonus = new() },
            AspectsStrategiques = new() { Nom = "Strategiques", Puissance = 30, Bonus = new() }
        };

        var lucieHouse1 = _context.LucieHouses.First();
        piece1.GetType().GetProperty("LucieHouseId")?.SetValue(piece1, lucieHouse1.Id);
        piece2.GetType().GetProperty("LucieHouseId")?.SetValue(piece2, lucieHouse1.Id);
        piece3.GetType().GetProperty("LucieHouseId")?.SetValue(piece3, lucieHouse1.Id);

        _context.Pieces.AddRange(piece1, piece2, piece3);
        _context.SaveChanges();

        // Act
        int result = _service.GetPuissanceMaxLucieEscouade();

        // Assert - Top 2 tactiques (100 + 80) + stratégie totale (50 + 40 + 30) = 300
        Assert.Equal(300, result);
    }

    [Fact]
    public void GetPuissanceMaxLucieEscouade_WithNoPieces_ShouldReturnZero()
    {
        // Act
        int result = _service.GetPuissanceMaxLucieEscouade();

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void GetTopLucieRooms_ShouldReturnTopTwoByPuissanceTotale()
    {
        // Arrange
        var lucieHouse = new LucieHouse { Affection = 50 };
        _context.LucieHouses.Add(lucieHouse);
        _context.SaveChanges();

        var piece1 = new Piece
        {
            Nom = "Gymnase",
            Niveau = 3,
            Selectionnee = false,
            BonusTactiquesSerialized = "[]",
            BonusStrategiquesSerialized = "[]",
            AspectsTactiques = new() { Nom = "Tactiques", Puissance = 200, Bonus = new() },
            AspectsStrategiques = new() { Nom = "Strategiques", Puissance = 100, Bonus = new() }
        };

        var piece2 = new Piece
        {
            Nom = "Chambre",
            Niveau = 2,
            Selectionnee = false,
            BonusTactiquesSerialized = "[]",
            BonusStrategiquesSerialized = "[]",
            AspectsTactiques = new() { Nom = "Tactiques", Puissance = 150, Bonus = new() },
            AspectsStrategiques = new() { Nom = "Strategiques", Puissance = 75, Bonus = new() }
        };

        var piece3 = new Piece
        {
            Nom = "Cuisine",
            Niveau = 1,
            Selectionnee = false,
            BonusTactiquesSerialized = "[]",
            BonusStrategiquesSerialized = "[]",
            AspectsTactiques = new() { Nom = "Tactiques", Puissance = 100, Bonus = new() },
            AspectsStrategiques = new() { Nom = "Strategiques", Puissance = 50, Bonus = new() }
        };

        var lucieHouse1 = _context.LucieHouses.First();
        piece1.GetType().GetProperty("LucieHouseId")?.SetValue(piece1, lucieHouse1.Id);
        piece2.GetType().GetProperty("LucieHouseId")?.SetValue(piece2, lucieHouse1.Id);
        piece3.GetType().GetProperty("LucieHouseId")?.SetValue(piece3, lucieHouse1.Id);

        _context.Pieces.AddRange(piece1, piece2, piece3);
        _context.SaveChanges();

        // Act
        var result = _service.GetTopLucieRooms(2).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("Gymnase", result[0].Nom);
        Assert.Equal("Chambre", result[1].Nom);
    }

    [Fact]
    public void GetPuissanceMaxEscouade_ShouldIncludeLucieMaxPuissance()
    {
        // Arrange - Add a mercenary
        var mercenary = new Personnage
        {
            Nom = "Mercenaire Test",
            Rarete = Rarete.SSR,
            Niveau = 50,
            Type = TypePersonnage.Mercenaire,
            Rang = 1,
            Puissance = 100,
            PA = 10,
            PV = 50,
            Role = Role.Combattante,
            Faction = Faction.Syndicat,
            Selectionne = false,
            TypeAttaque = TypeAttaque.Melee
        };

        _context.Personnages.Add(mercenary);

        // Add Lucie house with pieces
        var lucieHouse = new LucieHouse { Affection = 50 };
        _context.LucieHouses.Add(lucieHouse);
        _context.SaveChanges();

        var piece1 = new Piece
        {
            Nom = "Gymnase",
            Niveau = 3,
            Selectionnee = false,
            BonusTactiquesSerialized = "[]",
            BonusStrategiquesSerialized = "[]",
            AspectsTactiques = new() { Nom = "Tactiques", Puissance = 50, Bonus = new() },
            AspectsStrategiques = new() { Nom = "Strategiques", Puissance = 25, Bonus = new() }
        };

        var lucieHouse1 = _context.LucieHouses.First();
        piece1.GetType().GetProperty("LucieHouseId")?.SetValue(piece1, lucieHouse1.Id);

        _context.Pieces.Add(piece1);
        _context.SaveChanges();

        // Act
        int result = _service.GetPuissanceMaxEscouade();

        // Assert - Should include mercenary (100) + Lucie (50 + 25) = 175
        Assert.Equal(175, result);
    }

    // ==================== ASYNC METHOD TESTS ====================

    [Fact]
    public async Task AddAsync_ShouldAddPersonnageAndRecordHistory()
    {
        // Arrange
        var personnage = new Personnage
        {
            Nom = "Nouveau Personnage",
            Rarete = Rarete.SR,
            Niveau = 10,
            Type = TypePersonnage.Mercenaire,
            Rang = 2,
            Puissance = 50,
            PA = 8,
            PV = 30,
            Role = Role.Combattante,
            Faction = Faction.Syndicat,
            Selectionne = false,
            TypeAttaque = TypeAttaque.Melee
        };

        // Act
        await _service.AddAsync(personnage);

        // Assert
        var saved = await _context.Personnages.FirstOrDefaultAsync(p => p.Nom == "Nouveau Personnage");
        Assert.NotNull(saved);
        Assert.Equal("Nouveau Personnage", saved.Nom);
        Assert.Equal(10, saved.Niveau);
        Assert.Equal(50, saved.Puissance);

        // Verify history was recorded
        var history = await _context.HistoriquesModifications
            .FirstOrDefaultAsync(h => h.TypeModification == TypeModification.Creation && h.NomEntite == "Nouveau Personnage");
        Assert.NotNull(history);
        Assert.Equal(TypeEntite.Personnage, history.TypeEntite);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdatePersonnageAndRecordHistory()
    {
        // Arrange
        var personnage = new Personnage
        {
            Nom = "Test Update",
            Rarete = Rarete.R,
            Niveau = 1,
            Type = TypePersonnage.Mercenaire,
            Rang = 1,
            Puissance = 100,
            PA = 5,
            PV = 20,
            Role = Role.Sentinelle,
            Faction = Faction.Syndicat,
            Selectionne = false,
            TypeAttaque = TypeAttaque.Melee
        };

        _context.Personnages.Add(personnage);
        await _context.SaveChangesAsync();
        int personId = personnage.Id;

        // Modify properties
        personnage.Niveau = 50;
        personnage.Puissance = 500;
        personnage.Rang = 5;

        // Act
        await _service.UpdateAsync(personnage);

        // Assert
        var updated = await _context.Personnages.FindAsync(personId);
        Assert.NotNull(updated);
        Assert.Equal(50, updated.Niveau);
        Assert.Equal(500, updated.Puissance);
        Assert.Equal(5, updated.Rang);

        // Verify history was recorded for each change
        var historyEntries = await _context.HistoriquesModifications
            .Where(h => h.EntiteId == personId && h.TypeModification == TypeModification.Modification)
            .ToListAsync();
        Assert.NotEmpty(historyEntries);
        Assert.Contains(historyEntries, h => h.ChampModifie == "Niveau");
        Assert.Contains(historyEntries, h => h.ChampModifie == "Puissance");
        Assert.Contains(historyEntries, h => h.ChampModifie == "Rang");
    }

    [Fact]
    public async Task UpdateAsync_WithNoChanges_ShouldNotRecordHistory()
    {
        // Arrange
        var personnage = new Personnage
        {
            Nom = "Test No Change",
            Rarete = Rarete.R,
            Niveau = 1,
            Type = TypePersonnage.Mercenaire,
            Rang = 1,
            Puissance = 100,
            PA = 5,
            PV = 20,
            Role = Role.Sentinelle,
            Faction = Faction.Syndicat,
            Selectionne = false,
            TypeAttaque = TypeAttaque.Melee
        };

        _context.Personnages.Add(personnage);
        await _context.SaveChangesAsync();
        int personId = personnage.Id;

        // Act - Update with same values
        await _service.UpdateAsync(personnage);

        // Assert - No history entries should be created
        var historyEntries = await _context.HistoriquesModifications
            .Where(h => h.EntiteId == personId && h.TypeModification == TypeModification.Modification)
            .ToListAsync();
        Assert.Empty(historyEntries);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentPersonnage_ShouldNotThrow()
    {
        // Arrange
        var personnage = new Personnage
        {
            Id = 999,
            Nom = "Non Existent"
        };

        // Act & Assert - Should not throw
        await _service.UpdateAsync(personnage);
        
        // Verify the personnage was not added to the database
        var result = await _context.Personnages.FindAsync(999);
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemovePersonnageAndRecordHistory()
    {
        // Arrange
        var personnage = new Personnage
        {
            Nom = "To Delete",
            Rarete = Rarete.SR,
            Niveau = 25,
            Type = TypePersonnage.Mercenaire,
            Rang = 3,
            Puissance = 200,
            PA = 10,
            PV = 40,
            Role = Role.Combattante,
            Faction = Faction.Syndicat,
            Selectionne = false,
            TypeAttaque = TypeAttaque.Distance
        };

        _context.Personnages.Add(personnage);
        await _context.SaveChangesAsync();
        int personId = personnage.Id;

        // Act
        await _service.DeleteAsync(personId);

        // Assert
        var deleted = await _context.Personnages.FindAsync(personId);
        Assert.Null(deleted);

        // Verify history was recorded
        var history = await _context.HistoriquesModifications
            .FirstOrDefaultAsync(h => h.EntiteId == personId && h.TypeModification == TypeModification.Suppression);
        Assert.NotNull(history);
        Assert.Equal("To Delete", history.NomEntite);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentId_ShouldNotThrow()
    {
        // Act & Assert - Should not throw
        await _service.DeleteAsync(999);
        
        // Verify no personnage exists with this ID
        var result = await _context.Personnages.FindAsync(999);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllPersonnages()
    {
        // Arrange
        var personnage1 = new Personnage
        {
            Nom = "Personnage 1",
            Rarete = Rarete.R,
            Niveau = 1,
            Type = TypePersonnage.Mercenaire,
            Rang = 1,
            Puissance = 50,
            PA = 5,
            PV = 20,
            Role = Role.Sentinelle,
            Faction = Faction.Syndicat,
            Selectionne = false
        };

        var personnage2 = new Personnage
        {
            Nom = "Personnage 2",
            Rarete = Rarete.SR,
            Niveau = 10,
            Type = TypePersonnage.Commandant,
            Rang = 2,
            Puissance = 100,
            PA = 10,
            PV = 40,
            Role = Role.Combattante,
            Faction = Faction.Pacificateurs,
            Selectionne = true
        };

        _context.Personnages.AddRange(personnage1, personnage2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnPersonnage()
    {
        // Arrange
        var personnage = new Personnage
        {
            Nom = "Test GetById",
            Rarete = Rarete.SSR,
            Niveau = 50,
            Type = TypePersonnage.Mercenaire,
            Rang = 5,
            Puissance = 1000,
            PA = 20,
            PV = 100,
            Role = Role.Combattante,
            Faction = Faction.Syndicat,
            Selectionne = true
        };

        _context.Personnages.Add(personnage);
        await _context.SaveChangesAsync();
        int personId = personnage.Id;

        // Act
        var result = await _service.GetByIdAsync(personId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test GetById", result.Nom);
        Assert.Equal(50, result.Niveau);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentId_ShouldReturnNull()
    {
        // Act
        var result = await _service.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateImageUrls()
    {
        // Arrange
        var personnage = new Personnage
        {
            Nom = "Test Image",
            Rarete = Rarete.R,
            Niveau = 1,
            Type = TypePersonnage.Mercenaire,
            Rang = 1,
            Puissance = 100,
            PA = 5,
            PV = 20,
            Role = Role.Sentinelle,
            Faction = Faction.Syndicat,
            Selectionne = false
        };

        _context.Personnages.Add(personnage);
        await _context.SaveChangesAsync();

        // Change name
        personnage.Nom = "New Name";

        // Act
        await _service.UpdateAsync(personnage);

        // Assert
        var updated = await _context.Personnages.FindAsync(personnage.Id);
        Assert.NotNull(updated);
        Assert.False(string.IsNullOrEmpty(updated.ImageUrlDetailStored));
        Assert.False(string.IsNullOrEmpty(updated.ImageUrlPreviewStored));
        Assert.False(string.IsNullOrEmpty(updated.ImageUrlSelectedStored));
    }

    [Fact]
    public async Task UpdateCapacitesAsync_ShouldUpdateCapacites()
    {
        // Arrange
        var personnage = new Personnage
        {
            Nom = "Test Capacites",
            Rarete = Rarete.R,
            Niveau = 1,
            Type = TypePersonnage.Mercenaire,
            Rang = 1,
            Puissance = 100,
            PA = 5,
            PV = 20,
            Role = Role.Sentinelle,
            Faction = Faction.Syndicat,
            Selectionne = false
        };

        var capacite1 = new Capacite { Nom = "Capacite 1", Description = "Test", Icon = "icon1" };
        var capacite2 = new Capacite { Nom = "Capacite 2", Description = "Test", Icon = "icon2" };

        _context.Personnages.Add(personnage);
        _context.Capacites.AddRange(capacite1, capacite2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.UpdateCapacitesAsync(personnage.Id, new[] { capacite1.Id, capacite2.Id });

        // Assert
        Assert.True(result);
        var updated = await _context.Personnages
            .Include(p => p.Capacites)
            .FirstOrDefaultAsync(p => p.Id == personnage.Id);
        Assert.NotNull(updated);
        Assert.Equal(2, updated.Capacites.Count);
    }

    [Fact]
    public async Task UpdateCapacitesAsync_WithNonExistentPersonnage_ShouldReturnFalse()
    {
        // Act
        var result = await _service.UpdateCapacitesAsync(999, new[] { 1, 2 });

        // Assert
        Assert.False(result);
    }
}
