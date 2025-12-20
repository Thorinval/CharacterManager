using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CharacterManager.Server.Data;
using CharacterManager.Server.Models;
using CharacterManager.Server.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Moq;

namespace CharacterManager.Tests;

public class PmlImportServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly PersonnageService _personnageService;
    private readonly PmlImportService _pmlImportService;

    public PmlImportServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();
        
        // Mock PersonnageImageConfigService
        var mockImageConfigService = new Mock<PersonnageImageConfigService>();
        
        _personnageService = new PersonnageService(_context, mockImageConfigService.Object);
        _pmlImportService = new PmlImportService(_personnageService, _context);

        SeedPersonnages();
    }

    private void SeedPersonnages()
    {
        _context.Personnages.Add(new Personnage
        {
            Nom = "REGINA",
            Type = TypePersonnage.Mercenaire,
            Rarete = Rarete.SSR,
            Niveau = 14,
            Rang = 2,
            Puissance = 3320,
            PA = 140,
            PV = 509,
            Role = Role.Sentinelle,
            Faction = Faction.Syndicat,
            ImageUrlDetail = "/images/regina.png",
            ImageUrlPreview = "/images/regina_small_portrait.png",
            ImageUrlSelected = "/images/regina_small_select.png",
            Description = "SSR Mercenaire"
        });

        _context.Personnages.Add(new Personnage
        {
            Nom = "ISABELLA",
            Type = TypePersonnage.Androïde,
            Rarete = Rarete.SSR,
            Niveau = 2,
            Rang = 0,
            Puissance = 835,
            PA = 0,
            PV = 20,
            Role = Role.Androide,
            Faction = Faction.Inconnu,
            ImageUrlDetail = "/images/isabella.png",
            ImageUrlPreview = "/images/isabella_small_portrait.png",
            ImageUrlSelected = "/images/isabella_small_select.png",
            Description = "SSR Androide"
        });

        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context?.Dispose();
    }

    [Fact]
    public async Task ImportPmlAsync_WithValidInventaire_ShouldImportPersonnages()
    {
        // Arrange
        var pmlContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<InventairePML version=""1.0"" exportDate=""2025-12-20T15:30:00Z"">
  <inventaire>
    <Personnage>
      <Nom>BELLE</Nom>
      <Rarete>SSR</Rarete>
      <Type>Mercenaire</Type>
      <Puissance>3090</Puissance>
      <PA>143</PA>
      <PV>330</PV>
      <Niveau>8</Niveau>
      <Rang>3</Rang>
      <Role>Sentinelle</Role>
      <Faction>Syndicat</Faction>
      <Selectionne>true</Selectionne>
      <Description>Personnage SSR</Description>
    </Personnage>
  </inventaire>
</InventairePML>";

        var stream = new MemoryStream(Encoding.UTF8.GetBytes(pmlContent));

        // Act
        var result = await _pmlImportService.ImportPmlAsync(stream);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.SuccessCount);

        var belle = _context.Personnages.FirstOrDefault(p => p.Nom == "BELLE");
        Assert.NotNull(belle);
        Assert.Equal(Rarete.SSR, belle.Rarete);
        Assert.Equal(TypePersonnage.Mercenaire, belle.Type);
        Assert.Equal(3090, belle.Puissance);
    }

    [Fact]
    public async Task ImportPmlAsync_WithValidTemplate_ShouldImportTemplate()
    {
        // Arrange
        var pmlContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<TemplatesPML version=""1.0"" exportDate=""2025-12-20T15:30:00Z"">
  <template>
    <Nom>Mon Équipe</Nom>
    <Description>Ma première équipe</Description>
    <Personnage>
      <Nom>REGINA</Nom>
      <Rarete>SSR</Rarete>
      <Puissance>3320</Puissance>
      <Niveau>14</Niveau>
    </Personnage>
    <Personnage>
      <Nom>ISABELLA</Nom>
      <Rarete>SSR</Rarete>
      <Puissance>835</Puissance>
      <Niveau>2</Niveau>
    </Personnage>
  </template>
</TemplatesPML>";

        var stream = new MemoryStream(Encoding.UTF8.GetBytes(pmlContent));

        // Act
        var result = await _pmlImportService.ImportPmlAsync(stream);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.SuccessCount);

        var template = _context.Templates.FirstOrDefault(t => t.Nom == "Mon Équipe");
        Assert.NotNull(template);
        Assert.Equal("Ma première équipe", template.Description);
    }

    [Fact]
    public async Task ImportPmlAsync_WithMixedSections_ShouldImportBoth()
    {
        // Arrange
        var pmlContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<HistoriqueEscouadePML version=""1.0"" exportDate=""2025-12-20T15:30:00Z"">
  <inventaire>
    <Personnage>
      <Nom>KATARA</Nom>
      <Rarete>SR</Rarete>
      <Type>Mercenaire</Type>
      <Puissance>2000</Puissance>
      <PA>100</PA>
      <PV>200</PV>
      <Niveau>5</Niveau>
      <Rang>1</Rang>
      <Role>Guerrière</Role>
      <Faction>Ordre</Faction>
      <Selectionne>false</Selectionne>
      <Description>SR Mercenaire</Description>
    </Personnage>
  </inventaire>
  <templates>
    <template>
      <Nom>Test Team</Nom>
      <Description>Équipe de test</Description>
      <Personnage>
        <Nom>REGINA</Nom>
        <Rarete>SSR</Rarete>
        <Puissance>3320</Puissance>
        <Niveau>14</Niveau>
      </Personnage>
    </template>
  </templates>
</HistoriqueEscouadePML>";

        var stream = new MemoryStream(Encoding.UTF8.GetBytes(pmlContent));

        // Act
        var result = await _pmlImportService.ImportPmlAsync(stream);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.SuccessCount); // 1 personnage + 1 template

        var katara = _context.Personnages.FirstOrDefault(p => p.Nom == "KATARA");
        Assert.NotNull(katara);

        var testTeam = _context.Templates.FirstOrDefault(t => t.Nom == "Test Team");
        Assert.NotNull(testTeam);
    }

    [Fact]
    public async Task ExporterInventairePmlAsync_ShouldExportPersonnages()
    {
        // Arrange
        var personnages = _context.Personnages.ToList();

        // Act
        var pmlBytes = await _pmlImportService.ExporterInventairePmlAsync(personnages);

        // Assert
        Assert.NotNull(pmlBytes);
        Assert.True(pmlBytes.Length > 0);

        var content = Encoding.UTF8.GetString(pmlBytes);
        Assert.Contains("<inventaire>", content);
        Assert.Contains("REGINA", content);
        Assert.Contains("ISABELLA", content);
    }

    [Fact]
    public async Task ExporterTemplatesPmlAsync_ShouldExportTemplates()
    {
        // Arrange
        var personnageIds = _context.Personnages.Select(p => p.Id).ToList();
        var template = new Template
        {
            Nom = "Export Test",
            Description = "Template for export"
        };
        template.SetPersonnageIds(personnageIds);

        // Act
        var pmlBytes = await _pmlImportService.ExporterTemplatesPmlAsync(new[] { template });

        // Assert
        Assert.NotNull(pmlBytes);
        Assert.True(pmlBytes.Length > 0);

        var content = Encoding.UTF8.GetString(pmlBytes);
        Assert.Contains("<template>", content);
        Assert.Contains("Export Test", content);
        Assert.Contains("Template for export", content);
    }

    [Fact]
    public async Task ImportPmlAsync_WithEmptyFile_ShouldReturnError()
    {
        // Arrange
        var pmlContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<InventairePML version=""1.0"" exportDate=""2025-12-20T15:30:00Z"">
</InventairePML>";

        var stream = new MemoryStream(Encoding.UTF8.GetBytes(pmlContent));

        // Act
        var result = await _pmlImportService.ImportPmlAsync(stream);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(0, result.SuccessCount);
    }
}
