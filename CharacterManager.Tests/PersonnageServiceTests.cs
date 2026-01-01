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
        
        _service = new PersonnageService(_context);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }

    [Fact]
    public void Update_WithValidPersonnage_ShouldUpdateAllProperties()
    {
        // Arrange
        var existingPersonnage = new Personnage
        {
            Nom = "Ancien Nom",
            Rarete = Rarete.R,
            Niveau = 1,
            Type = TypePersonnage.Mercenaire,
            Rang = 1,
            Puissance = 10,
            PA = 5,
            PV = 20,
            Role = Role.Sentinelle,
            Faction = Faction.Syndicat,
            Description = "Ancienne description",
            Selectionne = false,
            TypeAttaque = TypeAttaque.Mêlée
        };

        _context.Personnages.Add(existingPersonnage);
        _context.SaveChanges();
        int personId = existingPersonnage.Id;

        var updatedPersonnage = new Personnage
        {
            Id = personId,
            Nom = "Nouveau Nom",
            Rarete = Rarete.SSR,
            Niveau = 50,
            Type = TypePersonnage.Commandant,
            Rang = 5,
            Puissance = 100,
            PA = 15,
            PV = 50,
            Role = Role.Combattante,
            Faction = Faction.Pacificateurs,
            Description = "Nouvelle description",
            Selectionne = true,
            TypeAttaque = TypeAttaque.Distance
        };

        // Act
        _service.Update(updatedPersonnage);

        // Assert
        var result = _context.Personnages.Find(personId);
        Assert.NotNull(result);
        Assert.Equal("Nouveau Nom", result.Nom);
        Assert.Equal(Rarete.SSR, result.Rarete);
        Assert.Equal(50, result.Niveau);
        Assert.Equal(TypePersonnage.Commandant, result.Type);
        Assert.Equal(5, result.Rang);
        Assert.Equal(100, result.Puissance);
        Assert.Equal(15, result.PA);
        Assert.Equal(50, result.PV);
        Assert.Equal(Role.Combattante, result.Role);
        Assert.Equal(Faction.Pacificateurs, result.Faction);
        Assert.Equal("/images/personnages/nouveau_nom.png", result.ImageUrlDetail);
        Assert.Equal("Nouvelle description", result.Description);
        Assert.True(result.Selectionne);
    }

    [Fact]
    public void Update_WithNonExistentPersonnage_ShouldDoNothing()
    {
        // Arrange
        var personnageToUpdate = new Personnage
        {
            Id = 999,
            Nom = "Non-existent"
        };

        // Act & Assert - Should not throw an exception
        _service.Update(personnageToUpdate);

        // Verify the non-existent personnage is not in the database
        var result = _context.Personnages.Find(999);
        Assert.Null(result);
    }

    [Fact]
    public void Update_ShouldNotModifyId()
    {
        // Arrange
        var existingPersonnage = new Personnage
        {
            Nom = "Original",
            Rarete = Rarete.R,
            Niveau = 1,
            Type = TypePersonnage.Mercenaire,
            Rang = 1,
            Puissance = 10,
            PA = 5,
            PV = 20,
            Role = Role.Sentinelle,
            Faction = Faction.Syndicat,
            Description = "desc",
            Selectionne = false
        };

        _context.Personnages.Add(existingPersonnage);
        _context.SaveChanges();
        int originalId = existingPersonnage.Id;

        var updatedPersonnage = new Personnage
        {
            Id = originalId,
            Nom = "Updated",
            Rarete = Rarete.SR,
            Niveau = 2,
            Type = TypePersonnage.Commandant,
            Rang = 2,
            Puissance = 20,
            PA = 10,
            PV = 25,
            Role = Role.Combattante,
            Faction = Faction.Pacificateurs,
            Description = "new desc",
            Selectionne = true
        };

        // Act
        _service.Update(updatedPersonnage);

        // Assert
        var result = _context.Personnages.Find(originalId);
        Assert.NotNull(result);
        Assert.Equal(originalId, result.Id);
    }

    [Fact]
    public void Update_WithPartialChanges_ShouldUpdateAllSpecifiedFields()
    {
        // Arrange
        var existingPersonnage = new Personnage
        {
            Nom = "Original Nom",
            Rarete = Rarete.R,
            Niveau = 1,
            Type = TypePersonnage.Mercenaire,
            Rang = 1,
            Puissance = 10,
            PA = 5,
            PV = 20,
            Role = Role.Sentinelle,
            Faction = Faction.Syndicat,
            Description = "desc",
            Selectionne = false
        };

        _context.Personnages.Add(existingPersonnage);
        _context.SaveChanges();
        int personId = existingPersonnage.Id;

        var updatedPersonnage = new Personnage
        {
            Id = personId,
            Nom = "Nouveau Nom",
            Rarete = Rarete.R,
            Niveau = 1,
            Type = TypePersonnage.Mercenaire,
            Rang = 1,
            Puissance = 10,
            PA = 5,
            PV = 20,
            Role = Role.Sentinelle,
            Faction = Faction.Syndicat,
            Description = "desc",
            Selectionne = false
        };

        // Act
        _service.Update(updatedPersonnage);

        // Assert
        var result = _context.Personnages.Find(personId);
        Assert.NotNull(result);
        Assert.Equal("Nouveau Nom", result.Nom);
        Assert.Equal(Rarete.R, result.Rarete);
        Assert.Equal(1, result.Niveau);
        Assert.Equal(TypePersonnage.Mercenaire, result.Type);
    }

    [Fact]
    public void Update_ShouldCallSaveChanges()
    {
        // Arrange
        var existingPersonnage = new Personnage
        {
            Nom = "Test",
            Rarete = Rarete.R,
            Niveau = 1,
            Type = TypePersonnage.Mercenaire,
            Rang = 1,
            Puissance = 10,
            PA = 5,
            PV = 20,
            Role = Role.Sentinelle,
            Faction = Faction.Syndicat,
            Description = "desc",
            Selectionne = false
        };

        _context.Personnages.Add(existingPersonnage);
        _context.SaveChanges();
        int personId = existingPersonnage.Id;

        var updatedPersonnage = new Personnage
        {
            Id = personId,
            Nom = "Updated Test",
            Rarete = Rarete.SR,
            Niveau = 10,
            Type = TypePersonnage.Commandant,
            Rang = 5,
            Puissance = 50,
            PA = 15,
            PV = 60,
            Role = Role.Combattante,
            Faction = Faction.Pacificateurs,
            Description = "new desc",
            Selectionne = true
        };

        // Act
        _service.Update(updatedPersonnage);

        // Assert - Verify the update was persisted in the database
        var persistedResult = _context.Personnages.Find(personId);
        Assert.NotNull(persistedResult);
        Assert.Equal("Updated Test", persistedResult.Nom);
        Assert.Equal(Rarete.SR, persistedResult.Rarete);
        Assert.Equal(10, persistedResult.Niveau);
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
            Description = "test",
            Selectionne = false,
            TypeAttaque = TypeAttaque.Mêlée
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
}
