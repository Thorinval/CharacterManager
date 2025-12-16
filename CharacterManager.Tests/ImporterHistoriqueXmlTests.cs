using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CharacterManager.Server.Data;
using CharacterManager.Server.Models;
using CharacterManager.Server.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CharacterManager.Tests;

public class ImporterHistoriqueXmlTests : IDisposable
{
    private readonly ApplicationDbContext _context;

    public ImporterHistoriqueXmlTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();

        SeedPersonnages();
    }

    private void SeedPersonnages()
    {
        // Commandant
        _context.Personnages.Add(new Personnage { Nom = "HUNTER", Type = TypePersonnage.Commandant, Rarete = Rarete.Inconnu, Niveau = 1, Rang = 0, Puissance = 0, ImageUrlDetail = string.Empty, ImageUrlPreview = string.Empty, ImageUrlSelected = string.Empty, Description = string.Empty });
        // Mercenaires
        string[] mercs = new[] { "BELLE", "REGINA", "KITTY", "NATASHA", "NAOMI", "SKYE", "SUNMI", "RAVENNA" };
        foreach (var m in mercs)
        {
            _context.Personnages.Add(new Personnage { Nom = m, Type = TypePersonnage.Mercenaire, Rarete = Rarete.Inconnu, Niveau = 1, Rang = 0, Puissance = 0, ImageUrlDetail = string.Empty, ImageUrlPreview = string.Empty, ImageUrlSelected = string.Empty, Description = string.Empty });
        }
        // Androides
        string[] ands = new[] { "RUBY", "AUDREY", "ISABELLA" };
        foreach (var a in ands)
        {
            _context.Personnages.Add(new Personnage { Nom = a, Type = TypePersonnage.Androïde, Rarete = Rarete.Inconnu, Niveau = 1, Rang = 0, Puissance = 0, ImageUrlDetail = string.Empty, ImageUrlPreview = string.Empty, ImageUrlSelected = string.Empty, Description = string.Empty });
        }
        _context.SaveChanges();
    }

    [Fact]
    public async Task Import_ExempleXml_ShouldCreateOneHistorique_WithExpectedValues()
    {
        var service = new HistoriqueEscouadeService(_context);
        var path = Path.Combine("d:", "Devs", "CharacterManager", "exemple_export_classement.xml");
        Assert.True(File.Exists(path));

        using var fs = File.OpenRead(path);
        int count = await service.ImporterHistoriqueAsync(fs);
        Assert.Equal(1, count);

        var h = _context.HistoriquesEscouade.Single();
        Assert.Equal(26980, h.PuissanceTotal);

        var donnees = service.DeserializerEscouade(h.DonneesEscouadeJson);
        Assert.NotNull(donnees);
        Assert.Equal(4, donnees!.Ligue);
        Assert.Equal(12345, donnees!.Score);
        Assert.Equal(3268, donnees!.Nutaku);
        Assert.Equal(5227, donnees!.Top150);
        Assert.Equal(217, donnees!.Pays);
        Assert.Equal(3485, donnees!.LuciePuissance);

        // Commandant + 8 mercenaires + 3 androides
        Assert.NotNull(donnees!.Commandant);
        Assert.Equal(8, donnees!.Mercenaires.Count);
        Assert.Equal(3, donnees!.Androides.Count);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
