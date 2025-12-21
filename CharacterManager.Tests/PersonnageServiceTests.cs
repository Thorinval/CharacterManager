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
}
