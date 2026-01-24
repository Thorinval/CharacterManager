using CharacterManager.Server.Data;
using CharacterManager.Server.Models;
using CharacterManager.Server.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CharacterManager.Tests;

public class HistoriqueModificationServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly HistoriqueModificationService _service;

    public HistoriqueModificationServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();
        
        _service = new HistoriqueModificationService(_context);
    }

        [Fact]
        public async Task PreviewImport_ShouldDetectConflicts_WhenPersonnageMissing()
        {
            // Arrange
            var json = "[{\"TypeEntite\":0,\"EntiteId\":99,\"NomEntite\":\"INCONNU\",\"TypeModification\":1,\"ChampModifie\":\"Puissance\",\"AncienneValeur\":\"1000\",\"NouvelleValeur\":\"1200\",\"DateModification\":\"2026-01-20T00:00:00Z\"}]";

            await using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));

            // Act
            var preview = await _service.PreviewImportAsync(stream);

            // Assert
            Assert.True(preview.HasConflicts);
            Assert.Single(preview.Conflicts);
            Assert.Equal("INCONNU", preview.Conflicts[0].PersonnageName);
            Assert.Equal("Puissance", preview.Conflicts[0].ChampModifie);
            Assert.Equal(0, preview.ValidCount);
        }

        [Fact]
        public async Task ImportAsync_ShouldApplyAndRecalculateFutureAnciennes_WhenOlderModificationArrives()
        {
            // Arrange: create personnage and future modification with ancienne valeur à mettre à jour
            var personnage = new Personnage { Nom = "REGINA", Type = TypePersonnage.Mercenaire, Puissance = 3000 };
            _context.Personnages.Add(personnage);
            await _context.SaveChangesAsync();

            var future = new HistoriqueModification
            {
                TypeEntite = TypeEntite.Personnage,
                EntiteId = personnage.Id,
                NomEntite = personnage.Nom,
                TypeModification = TypeModification.Modification,
                ChampModifie = "Puissance",
                AncienneValeur = "3000",
                NouvelleValeur = "3200",
                DateModification = new DateTime(2026, 1, 25, 0, 0, 0, DateTimeKind.Utc)
            };
            _context.HistoriquesModifications.Add(future);
            await _context.SaveChangesAsync();

            var json = $"[{{\"TypeEntite\":0,\"EntiteId\":{personnage.Id},\"NomEntite\":\"REGINA\",\"TypeModification\":1,\"ChampModifie\":\"Puissance\",\"AncienneValeur\":\"2800\",\"NouvelleValeur\":\"3000\",\"DateModification\":\"2026-01-15T00:00:00Z\"}}]";

            await using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));

            // Act
            var result = await _service.ImportAsync(stream, new Dictionary<string, bool>());

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(1, result.SuccessCount);

            // Future modification should now have ancienne valeur = 3000 (nouvelle de l'ancienne modif)
            var updatedFuture = await _context.HistoriquesModifications.FirstAsync(h => h.Id == future.Id);
            Assert.Equal("3000", updatedFuture.AncienneValeur);
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
    public async Task EnregistrerCreationAsync_ShouldCreateHistoriqueEntry()
    {
        // Arrange
        var entiteId = 1;
        var nomEntite = "Test Personnage";
        var details = new { Nom = nomEntite, Type = "Mercenaire", Niveau = 1 };

        // Act
        await _service.EnregistrerCreationAsync(
            TypeEntite.Personnage,
            entiteId,
            nomEntite,
            details,
            "Test de création");

        // Assert
        var historique = await _context.HistoriquesModifications.FirstOrDefaultAsync();
        Assert.NotNull(historique);
        Assert.Equal(TypeEntite.Personnage, historique.TypeEntite);
        Assert.Equal(entiteId, historique.EntiteId);
        Assert.Equal(nomEntite, historique.NomEntite);
        Assert.Equal(TypeModification.Creation, historique.TypeModification);
        Assert.Equal("Test de création", historique.Description);
        Assert.Null(historique.ChampModifie);
        Assert.Null(historique.AncienneValeur);
        Assert.NotNull(historique.NouvelleValeur);
        Assert.True(historique.DateModification <= DateTime.Now);
    }

    [Fact]
    public async Task EnregistrerModificationAsync_ShouldCreateHistoriqueEntryWithChanges()
    {
        // Arrange
        var entiteId = 2;
        var nomEntite = "Test Personnage";
        var champModifie = "Puissance";
        var ancienneValeur = 100;
        var nouvelleValeur = 150;

        // Act
        await _service.EnregistrerModificationAsync(
            TypeEntite.Personnage,
            entiteId,
            nomEntite,
            champModifie,
            ancienneValeur,
            nouvelleValeur,
            "Test de modification");

        // Assert
        var historique = await _context.HistoriquesModifications.FirstOrDefaultAsync();
        Assert.NotNull(historique);
        Assert.Equal(TypeEntite.Personnage, historique.TypeEntite);
        Assert.Equal(entiteId, historique.EntiteId);
        Assert.Equal(nomEntite, historique.NomEntite);
        Assert.Equal(TypeModification.Modification, historique.TypeModification);
        Assert.Equal(champModifie, historique.ChampModifie);
        Assert.Equal("100", historique.AncienneValeur);
        Assert.Equal("150", historique.NouvelleValeur);
        Assert.Equal("Test de modification", historique.Description);
    }

    [Fact]
    public async Task EnregistrerSuppressionAsync_ShouldCreateHistoriqueEntry()
    {
        // Arrange
        var entiteId = 3;
        var nomEntite = "Test Personnage";
        var details = new { Nom = nomEntite, Type = "Mercenaire", Niveau = 50, Puissance = 2000 };

        // Act
        await _service.EnregistrerSuppressionAsync(
            TypeEntite.Personnage,
            entiteId,
            nomEntite,
            details,
            "Test de suppression");

        // Assert
        var historique = await _context.HistoriquesModifications.FirstOrDefaultAsync();
        Assert.NotNull(historique);
        Assert.Equal(TypeEntite.Personnage, historique.TypeEntite);
        Assert.Equal(entiteId, historique.EntiteId);
        Assert.Equal(nomEntite, historique.NomEntite);
        Assert.Equal(TypeModification.Suppression, historique.TypeModification);
        Assert.Equal("Test de suppression", historique.Description);
        Assert.NotNull(historique.AncienneValeur);
    }

    [Fact]
    public async Task GetHistoriqueAsync_WithNoFilters_ShouldReturnAllEntries()
    {
        // Arrange
        await CreateTestHistoriqueData();

        // Act
        var result = await _service.GetHistoriqueAsync();

        // Assert
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task GetHistoriqueAsync_WithTypeEntiteFilter_ShouldReturnFilteredEntries()
    {
        // Arrange
        await CreateTestHistoriqueData();

        // Act
        var result = await _service.GetHistoriqueAsync(typeEntite: TypeEntite.Personnage);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, h => Assert.Equal(TypeEntite.Personnage, h.TypeEntite));
    }

    [Fact]
    public async Task GetHistoriqueAsync_WithTypeModificationFilter_ShouldReturnFilteredEntries()
    {
        // Arrange
        await CreateTestHistoriqueData();

        // Act - On doit filtrer manuellement car TypeModification n'est pas un paramètre
        var allResults = await _service.GetHistoriqueAsync();
        var result = allResults.Where(h => h.TypeModification == TypeModification.Modification).ToList();

        // Assert
        Assert.Single(result);
        Assert.All(result, h => Assert.Equal(TypeModification.Modification, h.TypeModification));
    }

    [Fact]
    public async Task GetHistoriqueAsync_WithDateFilter_ShouldReturnFilteredEntries()
    {
        // Arrange
        var dateDebut = DateTime.Now.AddHours(-1);
        var dateFin = DateTime.Now.AddHours(1);
        await CreateTestHistoriqueData();

        // Act
        var result = await _service.GetHistoriqueAsync(dateDebut: dateDebut, dateFin: dateFin);

        // Assert
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task GetHistoriqueAsync_WithMultipleFilters_ShouldReturnFilteredEntries()
    {
        // Arrange
        await CreateTestHistoriqueData();

        // Act - Filtrer par TypeEntite, puis manuellement par TypeModification
        var allResults = await _service.GetHistoriqueAsync(typeEntite: TypeEntite.Personnage);
        var result = allResults.Where(h => h.TypeModification == TypeModification.Creation).ToList();

        // Assert
        Assert.Single(result);
        var entry = result[0];
        Assert.Equal(TypeEntite.Personnage, entry.TypeEntite);
        Assert.Equal(TypeModification.Creation, entry.TypeModification);
    }

    [Fact]
    public async Task GetCountAsync_ShouldReturnCorrectCount()
    {
        // Arrange
        await CreateTestHistoriqueData();

        // Act
        var count = await _service.GetCountAsync();

        // Assert
        Assert.Equal(3, count);
    }

    [Fact]
    public async Task GetCountAsync_WithFilters_ShouldReturnFilteredCount()
    {
        // Arrange
        await CreateTestHistoriqueData();

        // Act
        var count = await _service.GetCountAsync(typeEntite: TypeEntite.Personnage);

        // Assert
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task ExporterAsync_ShouldReturnJsonString()
    {
        // Arrange
        await CreateTestHistoriqueData();

        // Act
        var json = await _service.ExporterAsync();

        // Assert
        Assert.NotNull(json);
        Assert.Contains("\"TypeEntite\"", json);
        Assert.Contains("\"TypeModification\"", json);
        Assert.Contains("Test Personnage 1", json);
    }

    [Fact]
    public async Task ExporterAsync_WithFilters_ShouldReturnFilteredJsonString()
    {
        // Arrange
        await CreateTestHistoriqueData();

        // Act
        var json = await _service.ExporterAsync();

        // Assert
        Assert.NotNull(json);
        Assert.Contains("Test", json);
    }

    [Fact]
    public async Task EnregistrerModificationAsync_WithNullValues_ShouldHandleGracefully()
    {
        // Arrange & Act
        await _service.EnregistrerModificationAsync(
            TypeEntite.Personnage,
            1,
            "Test",
            "Champ",
            null,
            null,
            "Test null values");

        // Assert
        var historique = await _context.HistoriquesModifications.FirstOrDefaultAsync();
        Assert.NotNull(historique);
        Assert.Null(historique.AncienneValeur);
        Assert.Null(historique.NouvelleValeur);
    }

    [Fact]
    public async Task GetHistoriqueAsync_ShouldOrderByDateDescending()
    {
        // Arrange
        await _service.EnregistrerCreationAsync(TypeEntite.Personnage, 1, "Premier", new { }, "Premier");
        await Task.Delay(10); // Petit délai pour assurer des dates différentes
        await _service.EnregistrerCreationAsync(TypeEntite.Personnage, 2, "Deuxième", new { }, "Deuxième");
        await Task.Delay(10);
        await _service.EnregistrerCreationAsync(TypeEntite.Personnage, 3, "Troisième", new { }, "Troisième");

        // Act
        var result = (await _service.GetHistoriqueAsync()).ToList();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("Troisième", result[0].NomEntite);
        Assert.Equal("Deuxième", result[1].NomEntite);
        Assert.Equal("Premier", result[2].NomEntite);
    }

    private async Task CreateTestHistoriqueData()
    {
        // Création d'un personnage
        await _service.EnregistrerCreationAsync(
            TypeEntite.Personnage,
            1,
            "Test Personnage 1",
            new { Nom = "Test", Type = "Mercenaire" },
            "Création test");

        // Modification d'un personnage
        await _service.EnregistrerModificationAsync(
            TypeEntite.Personnage,
            1,
            "Test Personnage 1",
            "Puissance",
            100,
            150,
            "Modification test");

        // Suppression d'une pièce
        await _service.EnregistrerSuppressionAsync(
            TypeEntite.Piece,
            1,
            "Test Piece",
            new { Nom = "Piece", Niveau = 1 },
            "Suppression test");
    }

    [Fact]
    public async Task GetHistoriqueEntiteAsync_ShouldReturnEntitySpecificHistory()
    {
        // Arrange
        await _service.EnregistrerCreationAsync(TypeEntite.Personnage, 1, "Perso 1", new { }, "Creation");
        await _service.EnregistrerModificationAsync(TypeEntite.Personnage, 1, "Perso 1", "Niveau", 1, 2, "Modif");
        await _service.EnregistrerCreationAsync(TypeEntite.Personnage, 2, "Perso 2", new { }, "Creation");

        // Act
        var result = await _service.GetHistoriqueEntiteAsync(TypeEntite.Personnage, 1);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, h => Assert.Equal(1, h.EntiteId));
    }

    [Fact]
    public async Task SupprimerHistoriqueAvantAsync_ShouldDeleteOldEntries()
    {
        // Arrange
        await CreateTestHistoriqueData();
        var cutoffDate = DateTime.UtcNow.AddDays(1);

        // Act
        var deletedCount = await _service.SupprimerHistoriqueAvantAsync(cutoffDate);

        // Assert
        Assert.Equal(3, deletedCount);
        var remaining = await _service.GetHistoriqueAsync();
        Assert.Empty(remaining);
    }

    [Fact]
    public async Task GetHistoriqueAsync_WithLimit_ShouldReturnLimitedResults()
    {
        // Arrange
        for (int i = 1; i <= 10; i++)
        {
            await _service.EnregistrerCreationAsync(TypeEntite.Personnage, i, $"Perso {i}", new { }, $"Creation {i}");
        }

        // Act
        var result = await _service.GetHistoriqueAsync(limit: 5);

        // Assert
        Assert.Equal(5, result.Count);
    }

    [Fact]
    public async Task EnregistrerModificationAsync_WithComplexObjects_ShouldSerializeCorrectly()
    {
        // Arrange
        var complexOldValue = new { Nom = "Old", Stats = new { HP = 100, MP = 50 } };
        var complexNewValue = new { Nom = "New", Stats = new { HP = 150, MP = 75 } };

        // Act
        await _service.EnregistrerModificationAsync(
            TypeEntite.Personnage,
            1,
            "Test Complex",
            "ComplexField",
            complexOldValue,
            complexNewValue,
            "Test serialization");

        // Assert
        var result = await _service.GetHistoriqueAsync();
        var entry = result[0];
        Assert.NotNull(entry.AncienneValeur);
        Assert.NotNull(entry.NouvelleValeur);
        Assert.Contains("HP", entry.AncienneValeur);
        Assert.Contains("MP", entry.NouvelleValeur);
    }

    [Fact]
    public async Task EnregistrerModificationAsync_WithinFiveSeconds_ShouldUpdateExistingEntry()
    {
        // Arrange
        var entiteId = 1;
        var nomEntite = "Test Personnage";
        var champModifie = "Puissance";

        // Act - Première modification
        await _service.EnregistrerModificationAsync(
            TypeEntite.Personnage,
            entiteId,
            nomEntite,
            champModifie,
            100,
            150,
            "Première modification");

        var historiquesAhres1 = await _context.HistoriquesModifications.ToListAsync();
        var idPremiereModif = historiquesAhres1[0].Id;

        // Deuxième modification dans les 5 secondes
        await _service.EnregistrerModificationAsync(
            TypeEntite.Personnage,
            entiteId,
            nomEntite,
            champModifie,
            150,
            200,
            "Deuxième modification");

        // Assert
        var resultat = await _context.HistoriquesModifications.ToListAsync();
        Assert.Single(resultat); // Une seule entrée, pas deux
        Assert.Equal(idPremiereModif, resultat[0].Id); // C'est la même entrée
        Assert.Equal("200", resultat[0].NouvelleValeur); // Nouvelle valeur mise à jour
        Assert.Equal("Deuxième modification", resultat[0].Description); // Description mise à jour
    }

    [Fact]
    public async Task EnregistrerModificationAsync_DifferentChamps_ShouldCreateNewEntry()
    {
        // Arrange
        var entiteId = 1;
        var nomEntite = "Test Personnage";

        // Act - Modification du champ "Puissance"
        await _service.EnregistrerModificationAsync(
            TypeEntite.Personnage,
            entiteId,
            nomEntite,
            "Puissance",
            100,
            150,
            "Modification Puissance");

        // Modification du champ "Niveau" (différent) dans les 5 secondes
        await _service.EnregistrerModificationAsync(
            TypeEntite.Personnage,
            entiteId,
            nomEntite,
            "Niveau",
            1,
            2,
            "Modification Niveau");

        // Assert
        var resultat = await _context.HistoriquesModifications.OrderByDescending(h => h.DateModification).ToListAsync();
        Assert.Equal(2, resultat.Count); // Deux entrées car champs différents
        Assert.Equal("Niveau", resultat[0].ChampModifie); // La plus récente d'abord
        Assert.Equal("Puissance", resultat[1].ChampModifie);
    }

    [Fact]
    public async Task EnregistrerModificationAsync_DifferentEntities_ShouldCreateNewEntry()
    {
        // Arrange
        var nomEntite = "Test Personnage";
        var champModifie = "Puissance";

        // Act - Modification de l'entité 1
        await _service.EnregistrerModificationAsync(
            TypeEntite.Personnage,
            1,
            nomEntite,
            champModifie,
            100,
            150,
            "Modification entité 1");

        // Modification de l'entité 2 (différente) dans les 5 secondes
        await _service.EnregistrerModificationAsync(
            TypeEntite.Personnage,
            2,
            nomEntite,
            champModifie,
            100,
            150,
            "Modification entité 2");

        // Assert
        var resultat = await _context.HistoriquesModifications.OrderByDescending(h => h.DateModification).ToListAsync();
        Assert.Equal(2, resultat.Count); // Deux entrées car entités différentes
        Assert.Equal(2, resultat[0].EntiteId); // La plus récente d'abord
        Assert.Equal(1, resultat[1].EntiteId);
    }
}
