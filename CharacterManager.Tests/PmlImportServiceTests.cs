using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CharacterManager.Server.Data;
using CharacterManager.Server.Models;
using CharacterManager.Server.Services;
using CharacterManager.Server.Constants;
using CharacterManager.Tests.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Xunit;
using Moq;
using Microsoft.AspNetCore.Http;

namespace CharacterManager.Tests;

public class PmlImportServiceTests : IDisposable
{
  private readonly ApplicationDbContext _context;
  private readonly PmlImportService _pmlImportService;
  private readonly PmlExportService _pmlExportService;

  public PmlImportServiceTests()
  {
    var options = new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options;

    _context = new ApplicationDbContext(options);
    _context.Database.EnsureCreated();

    _pmlImportService = new PmlImportService(_context);
    var exportLoggerMock = new Mock<ILogger<PmlExportService>>();
    _pmlExportService = new PmlExportService(_context, exportLoggerMock.Object);

    SeedPersonnages();
  }

  private void SeedPersonnages()
  {
    _context.Personnages.Add(new Personnage
    {
      Nom = TestDataConstants.PersonnageNames.Regina,
      Type = TypePersonnage.Mercenaire,
      Rarete = Rarete.SSR,
      Niveau = 14,
      Rang = 2,
      Puissance = 3320,
      PA = 140,
      PV = 509,
      Role = Role.Sentinelle,
      Faction = Faction.Syndicat
    });

    _context.Personnages.Add(new Personnage
    {
      Nom = TestDataConstants.PersonnageNames.Isabella,
      Type = TypePersonnage.Androide,
      Rarete = Rarete.SSR,
      Niveau = 2,
      Rang = 0,
      Puissance = 835,
      PA = 0,
      PV = 20,
      Role = Role.Androide,
      Faction = Faction.Inconnu
    });

    _context.SaveChanges();
  }

  [Fact]
  public async Task ImportPmlAsync_ShouldBlockInventoryWhenExistingPersonnages()
  {
    // Arrange: base already seeded with 2 personnages
    var pmlContent = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<{ImportExportConstants.XmlElements.InventairePML} version=""1.0"" exportDate=""2025-12-20T15:30:00Z"">
  <{ImportExportConstants.XmlElements.Inventaire}>
    <{ImportExportConstants.XmlElements.Personnage}>
      <{ImportExportConstants.XmlElements.Nom}>{TestDataConstants.PersonnageNames.Nouveau}</{ImportExportConstants.XmlElements.Nom}>
      <{ImportExportConstants.XmlElements.Rarete}>SR</{ImportExportConstants.XmlElements.Rarete}>
      <{ImportExportConstants.XmlElements.Type}>{PersonnageConstants.Types.Mercenaire}</{ImportExportConstants.XmlElements.Type}>
      <{ImportExportConstants.XmlElements.Puissance}>1500</{ImportExportConstants.XmlElements.Puissance}>
      <{ImportExportConstants.XmlElements.PA}>100</{ImportExportConstants.XmlElements.PA}>
      <{ImportExportConstants.XmlElements.PV}>200</{ImportExportConstants.XmlElements.PV}>
      <{ImportExportConstants.XmlElements.Niveau}>5</{ImportExportConstants.XmlElements.Niveau}>
      <{ImportExportConstants.XmlElements.Rang}>1</{ImportExportConstants.XmlElements.Rang}>
      <{ImportExportConstants.XmlElements.Role}>Guerrière</{ImportExportConstants.XmlElements.Role}>
      <{ImportExportConstants.XmlElements.Faction}>Inconnu</{ImportExportConstants.XmlElements.Faction}>
      <{ImportExportConstants.XmlElements.Selectionne}>false</{ImportExportConstants.XmlElements.Selectionne}>
    </{ImportExportConstants.XmlElements.Personnage}>
  </{ImportExportConstants.XmlElements.Inventaire}>
</{ImportExportConstants.XmlElements.InventairePML}>";

    var stream = new MemoryStream(Encoding.UTF8.GetBytes(pmlContent));

    // Act
    var result = await _pmlImportService.ImportPmlAsync(stream, importInventory: true, importTemplates: false, importBestSquad: false, importHistories: false, importLeagueHistory: false);

    // Assert: should be blocked (success count = 0) because DB not empty
    Assert.False(result.IsSuccess);
    Assert.Equal(0, result.SuccessCount);
    Assert.Contains(result.Errors, e => e.Contains(TestDataConstants.ExpectedErrorMessages.InventoryImportBlocked));

    var existingCount = await _context.Personnages.CountAsync();
    Assert.Equal(2, existingCount); // no new insert
  }

  [Fact]
  public async Task ImportInventaire_ShouldCreateHistoriqueEntries_WhenHistoriqueServiceProvided()
  {
    // Arrange
    var options = new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options;
    await using var ctx = new ApplicationDbContext(options);
    var histoService = new HistoriqueModificationService(ctx);
    var service = new PmlImportService(ctx, histoService);

    var pmlContent = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<{ImportExportConstants.XmlElements.InventairePML} version=""1.0"" exportDate=""2026-01-24T00:00:00Z"">
  <{ImportExportConstants.XmlElements.Inventaire}>
    <{ImportExportConstants.XmlElements.Personnage}>
      <{ImportExportConstants.XmlElements.Nom}>{TestDataConstants.PersonnageNames.Alpha}</{ImportExportConstants.XmlElements.Nom}>
      <{ImportExportConstants.XmlElements.Rarete}>R</{ImportExportConstants.XmlElements.Rarete}>
      <{ImportExportConstants.XmlElements.Type}>{PersonnageConstants.Types.Mercenaire}</{ImportExportConstants.XmlElements.Type}>
      <{ImportExportConstants.XmlElements.Puissance}>900</{ImportExportConstants.XmlElements.Puissance}>
      <{ImportExportConstants.XmlElements.PA}>50</{ImportExportConstants.XmlElements.PA}>
      <{ImportExportConstants.XmlElements.PV}>120</{ImportExportConstants.XmlElements.PV}>
      <{ImportExportConstants.XmlElements.Niveau}>3</{ImportExportConstants.XmlElements.Niveau}>
      <{ImportExportConstants.XmlElements.Rang}>0</{ImportExportConstants.XmlElements.Rang}>
      <{ImportExportConstants.XmlElements.Role}>Guerrière</{ImportExportConstants.XmlElements.Role}>
      <{ImportExportConstants.XmlElements.Faction}>Inconnu</{ImportExportConstants.XmlElements.Faction}>
      <{ImportExportConstants.XmlElements.Selectionne}>true</{ImportExportConstants.XmlElements.Selectionne}>
    </{ImportExportConstants.XmlElements.Personnage}>
  </{ImportExportConstants.XmlElements.Inventaire}>
</{ImportExportConstants.XmlElements.InventairePML}>";

    await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(pmlContent));

    // Act
    var result = await service.ImportPmlAsync(stream);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.Equal(1, result.SuccessCount);

    var histos = await ctx.HistoriquesModifications.Where(h => h.TypeModification == TypeModification.Creation).ToListAsync();
    Assert.Single(histos);
    Assert.Equal(TestDataConstants.PersonnageNames.Alpha, histos[0].NomEntite);
    Assert.Equal(TypeEntite.Personnage, histos[0].TypeEntite);
    Assert.True(histos[0].EstImportation);
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

  ~PmlImportServiceTests()
  {
    Dispose(false);
  }

  [Fact]
  public async Task ImportPmlAsync_WithValidInventaire_ShouldImportPersonnages()
  {
    // Arrange
    var pmlContent = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<{ImportExportConstants.XmlElements.InventairePML} version=""1.0"" exportDate=""2025-12-20T15:30:00Z"">
  <{ImportExportConstants.XmlElements.Inventaire}>
    <{ImportExportConstants.XmlElements.Personnage}>
      <{ImportExportConstants.XmlElements.Nom}>{TestDataConstants.PersonnageNames.Belle}</{ImportExportConstants.XmlElements.Nom}>
      <{ImportExportConstants.XmlElements.Rarete}>SSR</{ImportExportConstants.XmlElements.Rarete}>
      <{ImportExportConstants.XmlElements.Type}>{PersonnageConstants.Types.Mercenaire}</{ImportExportConstants.XmlElements.Type}>
      <{ImportExportConstants.XmlElements.Puissance}>3090</{ImportExportConstants.XmlElements.Puissance}>
      <{ImportExportConstants.XmlElements.PA}>143</{ImportExportConstants.XmlElements.PA}>
      <{ImportExportConstants.XmlElements.PV}>330</{ImportExportConstants.XmlElements.PV}>
      <{ImportExportConstants.XmlElements.Niveau}>8</{ImportExportConstants.XmlElements.Niveau}>
      <{ImportExportConstants.XmlElements.Rang}>3</{ImportExportConstants.XmlElements.Rang}>
      <{ImportExportConstants.XmlElements.Role}>Sentinelle</{ImportExportConstants.XmlElements.Role}>
      <{ImportExportConstants.XmlElements.Faction}>Syndicat</{ImportExportConstants.XmlElements.Faction}>
      <{ImportExportConstants.XmlElements.Selectionne}>true</{ImportExportConstants.XmlElements.Selectionne}>
      <{ImportExportConstants.XmlElements.Description}>{TestDataConstants.PersonnageDescriptions.SSRCharacter}</{ImportExportConstants.XmlElements.Description}>
    </{ImportExportConstants.XmlElements.Personnage}>
  </{ImportExportConstants.XmlElements.Inventaire}>
</{ImportExportConstants.XmlElements.InventairePML}>";

    var stream = new MemoryStream(Encoding.UTF8.GetBytes(pmlContent));

    // Act
    var result = await _pmlImportService.ImportPmlAsync(stream);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.Equal(1, result.SuccessCount);

    var belle = await _context.Personnages.FirstOrDefaultAsync(p => p.Nom == TestDataConstants.PersonnageNames.Belle);
    Assert.NotNull(belle);
    Assert.Equal(Rarete.SSR, belle.Rarete);
    Assert.Equal(TypePersonnage.Mercenaire, belle.Type);
    Assert.Equal(3090, belle.Puissance);
  }

  [Fact]
  public async Task ImportPmlAsync_WithValidTemplate_ShouldImportTemplate()
  {
    // Arrange
    var pmlContent = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<{TemplateConstants.XmlElements.TemplatesPML} version=""1.0"" exportDate=""2025-12-20T15:30:00Z"">
  <{TemplateConstants.XmlElements.Template}>
    <{ImportExportConstants.XmlElements.Nom}>{TestDataConstants.TemplateNames.MonEquipe}</{ImportExportConstants.XmlElements.Nom}>
    <{ImportExportConstants.XmlElements.Description}>{TestDataConstants.TemplateNames.MonEquipeDescription}</{ImportExportConstants.XmlElements.Description}>
    <{ImportExportConstants.XmlElements.Personnage}>
      <{ImportExportConstants.XmlElements.Nom}>REGINA</{ImportExportConstants.XmlElements.Nom}>
      <{ImportExportConstants.XmlElements.Rarete}>SSR</{ImportExportConstants.XmlElements.Rarete}>
      <{ImportExportConstants.XmlElements.Puissance}>3320</{ImportExportConstants.XmlElements.Puissance}>
      <{ImportExportConstants.XmlElements.Niveau}>14</{ImportExportConstants.XmlElements.Niveau}>
    </{ImportExportConstants.XmlElements.Personnage}>
    <{ImportExportConstants.XmlElements.Personnage}>
      <{ImportExportConstants.XmlElements.Nom}>ISABELLA</{ImportExportConstants.XmlElements.Nom}>
      <{ImportExportConstants.XmlElements.Rarete}>SSR</{ImportExportConstants.XmlElements.Rarete}>
      <{ImportExportConstants.XmlElements.Puissance}>835</{ImportExportConstants.XmlElements.Puissance}>
      <{ImportExportConstants.XmlElements.Niveau}>2</{ImportExportConstants.XmlElements.Niveau}>
    </{ImportExportConstants.XmlElements.Personnage}>
  </{TemplateConstants.XmlElements.Template}>
</{TemplateConstants.XmlElements.TemplatesPML}>";

    var stream = new MemoryStream(Encoding.UTF8.GetBytes(pmlContent));

    // Act
    var result = await _pmlImportService.ImportPmlAsync(stream);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.Equal(2, result.SuccessCount);

    var template = await _context.Templates.FirstOrDefaultAsync(t => t.Nom == TestDataConstants.TemplateNames.MonEquipe);
    Assert.NotNull(template);
    Assert.Equal(TestDataConstants.TemplateNames.MonEquipeDescription, template.Description);
  }

  [Fact]
  public async Task ImportPmlAsync_WithMixedSections_ShouldImportBoth()
  {
    // Arrange
    var pmlContent = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<{ImportExportConstants.XmlElements.HistoriqueClassements} version=""1.0"" exportDate=""2025-12-20T15:30:00Z"">
  <{ImportExportConstants.XmlElements.Inventaire}>
    <{ImportExportConstants.XmlElements.Personnage}>
      <{ImportExportConstants.XmlElements.Nom}>{TestDataConstants.PersonnageNames.Katara}</{ImportExportConstants.XmlElements.Nom}>
      <{ImportExportConstants.XmlElements.Rarete}>SR</{ImportExportConstants.XmlElements.Rarete}>
      <{ImportExportConstants.XmlElements.Type}>{PersonnageConstants.Types.Mercenaire}</{ImportExportConstants.XmlElements.Type}>
      <{ImportExportConstants.XmlElements.Puissance}>2000</{ImportExportConstants.XmlElements.Puissance}>
      <{ImportExportConstants.XmlElements.PA}>100</{ImportExportConstants.XmlElements.PA}>
      <{ImportExportConstants.XmlElements.PV}>200</{ImportExportConstants.XmlElements.PV}>
      <{ImportExportConstants.XmlElements.Niveau}>5</{ImportExportConstants.XmlElements.Niveau}>
      <{ImportExportConstants.XmlElements.Rang}>1</{ImportExportConstants.XmlElements.Rang}>
      <{ImportExportConstants.XmlElements.Role}>Guerrière</{ImportExportConstants.XmlElements.Role}>
      <{ImportExportConstants.XmlElements.Faction}>Inconnu</{ImportExportConstants.XmlElements.Faction}>
      <{ImportExportConstants.XmlElements.Selectionne}>false</{ImportExportConstants.XmlElements.Selectionne}>
      <{ImportExportConstants.XmlElements.Description}>{TestDataConstants.PersonnageDescriptions.SRMercenary}</{ImportExportConstants.XmlElements.Description}>
    </{ImportExportConstants.XmlElements.Personnage}>
  </{ImportExportConstants.XmlElements.Inventaire}>
  <{ImportExportConstants.XmlElements.Templates}>
    <{TemplateConstants.XmlElements.Template}>
      <{ImportExportConstants.XmlElements.Nom}>{TestDataConstants.TemplateNames.TestTeam}</{ImportExportConstants.XmlElements.Nom}>
      <{ImportExportConstants.XmlElements.Description}>{TestDataConstants.TemplateNames.TestTeamDescription}</{ImportExportConstants.XmlElements.Description}>
      <{ImportExportConstants.XmlElements.Personnage}>
        <{ImportExportConstants.XmlElements.Nom}>REGINA</{ImportExportConstants.XmlElements.Nom}>
        <{ImportExportConstants.XmlElements.Rarete}>SSR</{ImportExportConstants.XmlElements.Rarete}>
        <{ImportExportConstants.XmlElements.Puissance}>3320</{ImportExportConstants.XmlElements.Puissance}>
        <{ImportExportConstants.XmlElements.Niveau}>14</{ImportExportConstants.XmlElements.Niveau}>
      </{ImportExportConstants.XmlElements.Personnage}>
    </{TemplateConstants.XmlElements.Template}>
  </{ImportExportConstants.XmlElements.Templates}>
</{ImportExportConstants.XmlElements.HistoriqueClassements}>";

    var stream = new MemoryStream(Encoding.UTF8.GetBytes(pmlContent));

    // Act
    var result = await _pmlImportService.ImportPmlAsync(stream);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.Equal(2, result.SuccessCount); // 1 personnage + 1 template

    var katara = await _context.Personnages.FirstOrDefaultAsync(p => p.Nom == TestDataConstants.PersonnageNames.Katara);
    Assert.NotNull(katara);

    var testTeam = await _context.Templates.FirstOrDefaultAsync(t => t.Nom == TestDataConstants.TemplateNames.TestTeam);
    Assert.NotNull(testTeam);
  }

  [Fact]
  public async Task ImportPmlAsync_WithBestSquad_ShouldImportAllRoles()
  {
    // Arrange
    var pmlContent = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<{ImportExportConstants.XmlElements.CharacterManagerPML} version=""1.0"" exportDate=""2025-12-20T15:30:00Z"">
  <{SquadConstants.XmlElements.MeilleurEscouade}>
    <{PersonnageConstants.Types.Mercenaire}>
      <{ImportExportConstants.XmlElements.Nom}>{TestDataConstants.PersonnageNames.Alya}</{ImportExportConstants.XmlElements.Nom}>
      <{ImportExportConstants.XmlElements.Rarete}>SR</{ImportExportConstants.XmlElements.Rarete}>
      <{ImportExportConstants.XmlElements.Type}>{PersonnageConstants.Types.Mercenaire}</{ImportExportConstants.XmlElements.Type}>
      <{ImportExportConstants.XmlElements.Puissance}>1500</{ImportExportConstants.XmlElements.Puissance}>
      <{ImportExportConstants.XmlElements.PA}>90</{ImportExportConstants.XmlElements.PA}>
      <{ImportExportConstants.XmlElements.PV}>220</{ImportExportConstants.XmlElements.PV}>
      <{ImportExportConstants.XmlElements.Niveau}>7</{ImportExportConstants.XmlElements.Niveau}>
      <{ImportExportConstants.XmlElements.Rang}>2</{ImportExportConstants.XmlElements.Rang}>
      <{ImportExportConstants.XmlElements.Role}>Sentinelle</{ImportExportConstants.XmlElements.Role}>
      <{ImportExportConstants.XmlElements.Faction}>Syndicat</{ImportExportConstants.XmlElements.Faction}>
      <{ImportExportConstants.XmlElements.Selectionne}>true</{ImportExportConstants.XmlElements.Selectionne}>
    </{PersonnageConstants.Types.Mercenaire}>
    <{PersonnageConstants.Types.Commandant}>
      <{ImportExportConstants.XmlElements.Nom}>{TestDataConstants.PersonnageNames.Commandra}</{ImportExportConstants.XmlElements.Nom}>
      <{ImportExportConstants.XmlElements.Rarete}>SSR</{ImportExportConstants.XmlElements.Rarete}>
      <{ImportExportConstants.XmlElements.Type}>{PersonnageConstants.Types.Commandant}</{ImportExportConstants.XmlElements.Type}>
      <{ImportExportConstants.XmlElements.Puissance}>4200</{ImportExportConstants.XmlElements.Puissance}>
      <{ImportExportConstants.XmlElements.PA}>200</{ImportExportConstants.XmlElements.PA}>
      <{ImportExportConstants.XmlElements.PV}>800</{ImportExportConstants.XmlElements.PV}>
      <{ImportExportConstants.XmlElements.Niveau}>20</{ImportExportConstants.XmlElements.Niveau}>
      <{ImportExportConstants.XmlElements.Rang}>4</{ImportExportConstants.XmlElements.Rang}>
      <{ImportExportConstants.XmlElements.Role}>Commandant</{ImportExportConstants.XmlElements.Role}>
      <{ImportExportConstants.XmlElements.Faction}>Pacificateurs</{ImportExportConstants.XmlElements.Faction}>
      <{ImportExportConstants.XmlElements.Selectionne}>false</{ImportExportConstants.XmlElements.Selectionne}>
    </{PersonnageConstants.Types.Commandant}>
    <{PersonnageConstants.Types.Androide}>
      <{ImportExportConstants.XmlElements.Nom}>{TestDataConstants.PersonnageNames.Omega}</{ImportExportConstants.XmlElements.Nom}>
      <{ImportExportConstants.XmlElements.Rarete}>SSR</{ImportExportConstants.XmlElements.Rarete}>
      <{ImportExportConstants.XmlElements.Type}>{PersonnageConstants.Types.Androide}</{ImportExportConstants.XmlElements.Type}>
      <{ImportExportConstants.XmlElements.Puissance}>3100</{ImportExportConstants.XmlElements.Puissance}>
      <{ImportExportConstants.XmlElements.PA}>50</{ImportExportConstants.XmlElements.PA}>
      <{ImportExportConstants.XmlElements.PV}>180</{ImportExportConstants.XmlElements.PV}>
      <{ImportExportConstants.XmlElements.Niveau}>10</{ImportExportConstants.XmlElements.Niveau}>
      <{ImportExportConstants.XmlElements.Rang}>2</{ImportExportConstants.XmlElements.Rang}>
      <{ImportExportConstants.XmlElements.Role}>Androide</{ImportExportConstants.XmlElements.Role}>
      <{ImportExportConstants.XmlElements.Faction}>HommesLibres</{ImportExportConstants.XmlElements.Faction}>
      <{ImportExportConstants.XmlElements.Selectionne}>false</{ImportExportConstants.XmlElements.Selectionne}>
    </{PersonnageConstants.Types.Androide}>
  </{SquadConstants.XmlElements.MeilleurEscouade}>
</{ImportExportConstants.XmlElements.CharacterManagerPML}>";

    var stream = new MemoryStream(Encoding.UTF8.GetBytes(pmlContent));

    // Act
    var result = await _pmlImportService.ImportPmlAsync(stream);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.Equal(3, result.SuccessCount);

    Assert.NotNull(await _context.Personnages.FirstOrDefaultAsync(p => p.Nom == TestDataConstants.PersonnageNames.Alya && p.Type == TypePersonnage.Mercenaire));
    Assert.NotNull(await _context.Personnages.FirstOrDefaultAsync(p => p.Nom == TestDataConstants.PersonnageNames.Commandra && p.Type == TypePersonnage.Commandant));
    Assert.NotNull(await _context.Personnages.FirstOrDefaultAsync(p => p.Nom == TestDataConstants.PersonnageNames.Omega && p.Type == TypePersonnage.Androide));
  }

  // Test désactivé : HistoriqueEscouade est obsolète, remplacé par HistoriqueClassement

  [Fact]
  public async Task ImportPmlAsync_WithFileName_ShouldPersistLastImportedName()
  {
    // Arrange
    var pmlContent = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<{ImportExportConstants.XmlElements.InventairePML} version=""1.0"" exportDate=""2025-12-20T15:30:00Z"">
  <{ImportExportConstants.XmlElements.Inventaire}>
    <{ImportExportConstants.XmlElements.Personnage}>
      <{ImportExportConstants.XmlElements.Nom}>{TestDataConstants.PersonnageNames.Nova}</{ImportExportConstants.XmlElements.Nom}>
      <{ImportExportConstants.XmlElements.Rarete}>SR</{ImportExportConstants.XmlElements.Rarete}>
      <{ImportExportConstants.XmlElements.Type}>{PersonnageConstants.Types.Mercenaire}</{ImportExportConstants.XmlElements.Type}>
      <{ImportExportConstants.XmlElements.Puissance}>1200</{ImportExportConstants.XmlElements.Puissance}>
      <{ImportExportConstants.XmlElements.PA}>60</{ImportExportConstants.XmlElements.PA}>
      <{ImportExportConstants.XmlElements.PV}>150</{ImportExportConstants.XmlElements.PV}>
      <{ImportExportConstants.XmlElements.Niveau}>6</{ImportExportConstants.XmlElements.Niveau}>
      <{ImportExportConstants.XmlElements.Rang}>2</{ImportExportConstants.XmlElements.Rang}>
      <{ImportExportConstants.XmlElements.Role}>Combattante</{ImportExportConstants.XmlElements.Role}>
      <{ImportExportConstants.XmlElements.Faction}>Syndicat</{ImportExportConstants.XmlElements.Faction}>
      <{ImportExportConstants.XmlElements.Selectionne}>true</{ImportExportConstants.XmlElements.Selectionne}>
    </{ImportExportConstants.XmlElements.Personnage}>
  </{ImportExportConstants.XmlElements.Inventaire}>
</{ImportExportConstants.XmlElements.InventairePML}>";

    var stream = new MemoryStream(Encoding.UTF8.GetBytes(pmlContent));

    // Act
    var result = await _pmlImportService.ImportPmlAsync(stream, fileName: TestDataConstants.FileNames.TestImportFile);

    // Assert
    Assert.True(result.IsSuccess);
    var lastFile = await _pmlImportService.GetLastImportedFileName();
    Assert.Equal(TestDataConstants.FileNames.TestImportFile, lastFile);
  }

  [Fact]
  public async Task ExporterInventairePmlAsync_ShouldExportPersonnages()
  {
    // Arrange
    var personnages = await _context.Personnages.ToListAsync();

    // Act
    var pmlBytes = await _pmlExportService.ExporterInventairePmlAsync(personnages);

    // Assert
    Assert.NotNull(pmlBytes);
    Assert.True(pmlBytes.Length > 0);

    var content = Encoding.UTF8.GetString(pmlBytes);
    Assert.Contains($"<{ImportExportConstants.XmlElements.Inventaire}>", content);
    Assert.Contains(TestDataConstants.PersonnageNames.Regina, content);
    Assert.Contains(TestDataConstants.PersonnageNames.Isabella, content);
  }

  [Fact]
  public async Task ExporterInventairePmlAsync_ShouldIncludeLucieHouseWhenPresent()
  {
    // Arrange
    var lucieHouse = new LucieHouse();
    lucieHouse.Pieces.Add(new Piece
    {
      Nom = TestDataConstants.LucieHousePieceNames.SalleduTrone,
      Niveau = TestDataConstants.NumericValues.LucieHouseNiveauLevel3,
      Selectionnee = true,
      AspectsTactiques = new Aspect { Bonus = { TestDataConstants.LucieHouseAspects.Degats }, Puissance = TestDataConstants.NumericValues.LuciePuissanceTactique12 },
      AspectsStrategiques = new Aspect { Bonus = { TestDataConstants.LucieHouseAspects.PV }, Puissance = TestDataConstants.NumericValues.LuciePuissanceStrategique7 }
    });

    _context.LucieHouses.Add(lucieHouse);
    await _context.SaveChangesAsync();

    var personnages = await _context.Personnages.ToListAsync();

    // Act
    var pmlBytes = await _pmlExportService.ExporterInventairePmlAsync(personnages);

    // Assert
    var content = Encoding.UTF8.GetString(pmlBytes);
    Assert.Contains(LucieHouseConstants.XmlElements.LucieHouse, content);
    Assert.Contains(TestDataConstants.LucieHousePieceNames.SalleduTrone, content);
    Assert.Contains(LucieHouseConstants.XmlElements.PuissanceTactique, content);
    Assert.Contains(LucieHouseConstants.XmlElements.PuissanceStrategique, content);
  }

  [Fact]
  public async Task ExporterTemplatesPmlAsync_ShouldExportTemplates()
  {
    // Arrange
    var personnageIds = await _context.Personnages.Select(p => p.Id).ToListAsync();
    var template = new Template
    {
      Nom = TestDataConstants.TemplateNames.ExportTest,
      Description = TestDataConstants.TemplateNames.ExportTestDescription
    };
    template.SetPersonnageIds(personnageIds);

    // Act
    var pmlBytes = await _pmlExportService.ExporterTemplatesPmlAsync(new[] { template });

    // Assert
    Assert.NotNull(pmlBytes);
    Assert.True(pmlBytes.Length > 0);

    var content = Encoding.UTF8.GetString(pmlBytes);
    Assert.Contains($"<{TemplateConstants.XmlElements.Template}>", content);
    Assert.Contains(TestDataConstants.TemplateNames.ExportTest, content);
    Assert.Contains(TestDataConstants.TemplateNames.ExportTestDescription, content);
  }

  [Fact]
  public async Task ExportPmlAsync_ShouldIncludeLucieHouseSection()
  {
    // Arrange
    var lucieHouse = new LucieHouse();
    lucieHouse.Pieces.Add(new Piece
    {
      Nom = TestDataConstants.LucieHousePieceNames.Atelier,
      Niveau = TestDataConstants.NumericValues.LucieHouseNiveauLevel2,
      Selectionnee = false,
      AspectsTactiques = new Aspect { Bonus = { TestDataConstants.LucieHouseAspects.Crit }, Puissance = TestDataConstants.NumericValues.LuciePuissanceTactique5 },
      AspectsStrategiques = new Aspect { Puissance = TestDataConstants.NumericValues.LuciePuissanceStrategique3 }
    });

    _context.LucieHouses.Add(lucieHouse);
    await _context.SaveChangesAsync();

    // Act
    var exportOptions = new PmlExportOptions();
    exportOptions.AddExportType(PmlExportOptions.EXPORT_TYPE_INVENTORY);
    var pmlBytes = await _pmlExportService.ExportPmlAsync(exportOptions);

    // Assert
    var content = Encoding.UTF8.GetString(pmlBytes);
    Assert.Contains(LucieHouseConstants.XmlElements.LucieHouse, content);
    Assert.Contains(TestDataConstants.LucieHousePieceNames.Atelier, content);
    Assert.Contains(LucieHouseConstants.XmlElements.PuissanceTactique, content);
    Assert.Contains(LucieHouseConstants.XmlElements.PuissanceStrategique, content);
  }

  [Fact]
  public async Task ImportPmlAsync_WithEmptyFile_ShouldReturnError()
  {
    // Arrange
    var pmlContent = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<{ImportExportConstants.XmlElements.InventairePML} version=""1.0"" exportDate=""2025-12-20T15:30:00Z"">
</{ImportExportConstants.XmlElements.InventairePML}>";

    var stream = new MemoryStream(Encoding.UTF8.GetBytes(pmlContent));

    // Act
    var result = await _pmlImportService.ImportPmlAsync(stream);

    // Assert
    Assert.False(result.IsSuccess);
    Assert.Equal(0, result.SuccessCount);
  }

  [Fact]
  public async Task ImportPmlAsync_WithLucieHouse_ShouldPersistPieces()
  {
    // Arrange
    var pmlContent = $@"<?xml version=""1.0"" encoding=""utf-8""?>
      <{ImportExportConstants.XmlElements.InventairePML} version=""1.0"" exportDate=""2025-12-20T15:30:00Z""> 
      <{ImportExportConstants.XmlElements.Inventaire}>
  <{LucieHouseConstants.XmlElements.LucieHouse}>
    <{LucieHouseConstants.XmlElements.Piece}>
      <{ImportExportConstants.XmlElements.Nom}>{TestDataConstants.LucieHousePieceNames.Hall}</{ImportExportConstants.XmlElements.Nom}>
      <{ImportExportConstants.XmlElements.Niveau}>{TestDataConstants.NumericValues.LucieHouseNiveauLevel4}</{ImportExportConstants.XmlElements.Niveau}>
      <{LucieHouseConstants.XmlElements.PuissanceTactique}>{TestDataConstants.NumericValues.LuciePuissanceTactique120}</{LucieHouseConstants.XmlElements.PuissanceTactique}>
      <{LucieHouseConstants.XmlElements.PuissanceStrategique}>{TestDataConstants.NumericValues.LuciePuissanceStrategique30}</{LucieHouseConstants.XmlElements.PuissanceStrategique}>
      <{LucieHouseConstants.XmlElements.Selectionnee}>true</{LucieHouseConstants.XmlElements.Selectionnee}>
      <{LucieHouseConstants.XmlElements.BonusTactiques}>
      </{LucieHouseConstants.XmlElements.BonusTactiques}>
    </{LucieHouseConstants.XmlElements.Piece}>
    <{LucieHouseConstants.XmlElements.Piece}>
      <{ImportExportConstants.XmlElements.Nom}>{TestDataConstants.LucieHousePieceNames.Bibliotheque}</{ImportExportConstants.XmlElements.Nom}>
      <{ImportExportConstants.XmlElements.Niveau}>{TestDataConstants.NumericValues.LucieHouseNiveauLevel2}</{ImportExportConstants.XmlElements.Niveau}>
      <{LucieHouseConstants.XmlElements.PuissanceTactique}>{TestDataConstants.NumericValues.LuciePuissanceTactique10}</{LucieHouseConstants.XmlElements.PuissanceTactique}>
      <{LucieHouseConstants.XmlElements.PuissanceStrategique}>{TestDataConstants.NumericValues.LuciePuissanceStrategique30}</{LucieHouseConstants.XmlElements.PuissanceStrategique}>
      <{LucieHouseConstants.XmlElements.Selectionnee}>false</{LucieHouseConstants.XmlElements.Selectionnee}>
      <{LucieHouseConstants.XmlElements.BonusStrategiques}>
      </{LucieHouseConstants.XmlElements.BonusStrategiques}>
    </{LucieHouseConstants.XmlElements.Piece}>
  </{LucieHouseConstants.XmlElements.LucieHouse}>
</{ImportExportConstants.XmlElements.Inventaire}>
</{ImportExportConstants.XmlElements.InventairePML}>";

    var stream = new MemoryStream(Encoding.UTF8.GetBytes(pmlContent));

    // Act
    var result = await _pmlImportService.ImportPmlAsync(stream);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.Equal(2, result.SuccessCount);

    var lucieHouse = await _context.LucieHouses.Include(l => l.Pieces).FirstOrDefaultAsync();
    Assert.NotNull(lucieHouse);
    Assert.Equal(2, lucieHouse.Pieces.Count);
    Assert.Contains(lucieHouse.Pieces, p => p.Nom == TestDataConstants.LucieHousePieceNames.Hall && p.Niveau== TestDataConstants.NumericValues.LucieHouseNiveauLevel4);
    Assert.Contains(lucieHouse.Pieces, p => p.Nom == TestDataConstants.LucieHousePieceNames.Bibliotheque && p.Niveau== TestDataConstants.NumericValues.LucieHouseNiveauLevel2);
  }

  [Fact]
  public async Task GetLastImportedFileName_ShouldReturnLastImportedFileName()
  {
    // Arrange
    var settings = new AppSettings
    {
      IsAdultModeEnabled = true,
      Language = "fr",
      LastImportedFileName = "test_export.pml",
      LastImportedDate = DateTime.UtcNow
    };
    _context.AppSettings.Add(settings);
    await _context.SaveChangesAsync();

    // Act
    var result = await _pmlImportService.GetLastImportedFileName();

    // Assert
    Assert.Equal(TestDataConstants.FileNames.TestExportFile, result);
  }

  [Fact]
  public async Task GetLastImportedFileName_WithoutAppSettings_ShouldReturnNull()
  {
    // Act
    var result = await _pmlImportService.GetLastImportedFileName();

    // Assert
    Assert.Null(result);
  }

  [Fact]
  public async Task GetLastImportedDateAsync_ShouldReturnLastImportedDate()
  {
    // Arrange
    var now = DateTime.UtcNow;
    var settings = new AppSettings
    {
      IsAdultModeEnabled = true,
      Language = "fr",
      LastImportedFileName = "test_export.pml",
      LastImportedDate = now
    };
    _context.AppSettings.Add(settings);
    await _context.SaveChangesAsync();

    // Act
    var result = await _pmlImportService.GetLastImportedDateAsync();

    // Assert
    Assert.NotNull(result);
    Assert.Equal(now.Date, result.Value.Date);
  }

  [Fact]
  public async Task GetLastExportDate_ShouldReturnLastExportDate()
  {
    // Arrange
    var now = DateTime.UtcNow;
    var settings = new AppSettings
    {
      IsAdultModeEnabled = true,
      Language = "fr",
      LastExportDate = now
    };
    _context.AppSettings.Add(settings);
    await _context.SaveChangesAsync();

    // Act
    var result = await _pmlExportService.GetLastExportDate();

    // Assert
    Assert.NotNull(result);
    Assert.Equal(now.Date, result.Value.Date);
  }

  [Fact]
  public async Task ImportPmlAsync_ShouldSaveLastImportedFileName()
  {
    // Arrange
    var pmlContent = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<{ImportExportConstants.XmlElements.InventairePML} version=""1.0"" exportDate=""2025-12-20T15:30:00Z"">
  <{ImportExportConstants.XmlElements.Inventaire}>
    <{ImportExportConstants.XmlElements.Personnage}>
      <{ImportExportConstants.XmlElements.Nom}>{TestDataConstants.PersonnageNames.Test}</{ImportExportConstants.XmlElements.Nom}>
      <{ImportExportConstants.XmlElements.Rarete}>SR</{ImportExportConstants.XmlElements.Rarete}>
      <{ImportExportConstants.XmlElements.Type}>{PersonnageConstants.Types.Mercenaire}</{ImportExportConstants.XmlElements.Type}>
      <{ImportExportConstants.XmlElements.Puissance}>1000</{ImportExportConstants.XmlElements.Puissance}>
      <{ImportExportConstants.XmlElements.PA}>50</{ImportExportConstants.XmlElements.PA}>
      <{ImportExportConstants.XmlElements.PV}>100</{ImportExportConstants.XmlElements.PV}>
      <{ImportExportConstants.XmlElements.Niveau}>5</{ImportExportConstants.XmlElements.Niveau}>
      <{ImportExportConstants.XmlElements.Rang}>1</{ImportExportConstants.XmlElements.Rang}>
      <{ImportExportConstants.XmlElements.Role}>Combattante</{ImportExportConstants.XmlElements.Role}>
      <{ImportExportConstants.XmlElements.Faction}>Pacificateurs</{ImportExportConstants.XmlElements.Faction}>
      <{ImportExportConstants.XmlElements.Selectionne}>false</{ImportExportConstants.XmlElements.Selectionne}>
      <{ImportExportConstants.XmlElements.Description}>{TestDataConstants.PersonnageDescriptions.TestPersonnage}</{ImportExportConstants.XmlElements.Description}>
    </{ImportExportConstants.XmlElements.Personnage}>
  </{ImportExportConstants.XmlElements.Inventaire}>
</{ImportExportConstants.XmlElements.InventairePML}>";

    var stream = new MemoryStream(Encoding.UTF8.GetBytes(pmlContent));

    // Act
    var result = await _pmlImportService.ImportPmlAsync(stream, TestDataConstants.FileNames.ConfigTestFile);

    // Assert
    Assert.True(result.IsSuccess);
    var settings = await _context.AppSettings.FirstOrDefaultAsync();
    Assert.NotNull(settings);
    Assert.Equal(TestDataConstants.FileNames.ConfigTestFile, settings.LastImportedFileName);
  }

  [Fact]
  public async Task ImportPmlAsync_WithMultiplePersonnages_ShouldImportAll()
  {
    // Arrange: import empty DB with multiple personnages
    var options = new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options;
    await using var ctx = new ApplicationDbContext(options);
    var service = new PmlImportService(ctx);

    var pmlContent = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<{ImportExportConstants.XmlElements.InventairePML} version=""1.0"" exportDate=""2025-12-20T15:30:00Z"">
  <{ImportExportConstants.XmlElements.Inventaire}>
    <{ImportExportConstants.XmlElements.Personnage}>
      <{ImportExportConstants.XmlElements.Nom}>{TestDataConstants.PersonnageNames.Regina}</{ImportExportConstants.XmlElements.Nom}>
      <{ImportExportConstants.XmlElements.Rarete}>SSR</{ImportExportConstants.XmlElements.Rarete}>
      <{ImportExportConstants.XmlElements.Type}>{PersonnageConstants.Types.Mercenaire}</{ImportExportConstants.XmlElements.Type}>
      <{ImportExportConstants.XmlElements.Puissance}>{TestDataConstants.NumericValues.PuissanceNiveau3320}</{ImportExportConstants.XmlElements.Puissance}>
      <{ImportExportConstants.XmlElements.PA}>{TestDataConstants.NumericValues.PAValue143}</{ImportExportConstants.XmlElements.PA}>
      <{ImportExportConstants.XmlElements.PV}>{TestDataConstants.NumericValues.PVValue330}</{ImportExportConstants.XmlElements.PV}>
      <{ImportExportConstants.XmlElements.Niveau}>{TestDataConstants.NumericValues.NiveauLevel14}</{ImportExportConstants.XmlElements.Niveau}>
      <{ImportExportConstants.XmlElements.Rang}>{TestDataConstants.NumericValues.RangValue2}</{ImportExportConstants.XmlElements.Rang}>
      <{ImportExportConstants.XmlElements.Role}>{TestDataConstants.PersonnageRoles.Sentinelle}</{ImportExportConstants.XmlElements.Role}>
      <{ImportExportConstants.XmlElements.Faction}>Syndicat</{ImportExportConstants.XmlElements.Faction}>
      <{ImportExportConstants.XmlElements.Selectionne}>true</{ImportExportConstants.XmlElements.Selectionne}>
    </{ImportExportConstants.XmlElements.Personnage}>
    <{ImportExportConstants.XmlElements.Personnage}>
      <{ImportExportConstants.XmlElements.Nom}>{TestDataConstants.PersonnageNames.Isabella}</{ImportExportConstants.XmlElements.Nom}>
      <{ImportExportConstants.XmlElements.Rarete}>SSR</{ImportExportConstants.XmlElements.Rarete}>
      <{ImportExportConstants.XmlElements.Type}>{PersonnageConstants.Types.Androide}</{ImportExportConstants.XmlElements.Type}>
      <{ImportExportConstants.XmlElements.Puissance}>{TestDataConstants.NumericValues.PuissanceNiveau835}</{ImportExportConstants.XmlElements.Puissance}>
      <{ImportExportConstants.XmlElements.PA}>0</{ImportExportConstants.XmlElements.PA}>
      <{ImportExportConstants.XmlElements.PV}>{TestDataConstants.NumericValues.PVValue100}</{ImportExportConstants.XmlElements.PV}>
      <{ImportExportConstants.XmlElements.Niveau}>{TestDataConstants.NumericValues.NiveauLevel2}</{ImportExportConstants.XmlElements.Niveau}>
      <{ImportExportConstants.XmlElements.Rang}>{TestDataConstants.NumericValues.RangValue0}</{ImportExportConstants.XmlElements.Rang}>
      <{ImportExportConstants.XmlElements.Role}>{TestDataConstants.PersonnageRoles.Androide}</{ImportExportConstants.XmlElements.Role}>
      <{ImportExportConstants.XmlElements.Faction}>Inconnu</{ImportExportConstants.XmlElements.Faction}>
      <{ImportExportConstants.XmlElements.Selectionne}>false</{ImportExportConstants.XmlElements.Selectionne}>
    </{ImportExportConstants.XmlElements.Personnage}>
  </{ImportExportConstants.XmlElements.Inventaire}>
</{ImportExportConstants.XmlElements.InventairePML}>";

    var stream = new MemoryStream(Encoding.UTF8.GetBytes(pmlContent));

    // Act
    var result = await service.ImportPmlAsync(stream);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.Equal(2, result.SuccessCount);

    var all = await ctx.Personnages.ToListAsync();
    Assert.Equal(2, all.Count);
    Assert.Single(all, p => p.Nom == TestDataConstants.PersonnageNames.Regina);
    Assert.Single(all, p => p.Nom == TestDataConstants.PersonnageNames.Isabella);
  }

  [Fact]
  public async Task ImportPmlAsync_WithInvalidXml_ShouldHandleGracefully()
  {
    // Arrange
    var pmlContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<InvalidRoot>
  <Personnage>Invalid</Personnage>
</InvalidRoot>";

    var stream = new MemoryStream(Encoding.UTF8.GetBytes(pmlContent));

    // Act
    var result = await _pmlImportService.ImportPmlAsync(stream);

    // Assert: service handles invalid root gracefully (no personnages created)
    Assert.Equal(0, result.SuccessCount);
    var personnages = await _context.Personnages.ToListAsync();
    Assert.DoesNotContain(personnages, p => p.Nom == "Invalid");
  }

  [Fact]
  public async Task ImportPmlAsync_WithMissingRequiredFields_ShouldCreatePersonnageWithDefaults()
  {
    // Arrange: create fresh DB
    var options = new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options;
    await using var ctx = new ApplicationDbContext(options);
    var service = new PmlImportService(ctx);

    var pmlContent = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<{ImportExportConstants.XmlElements.InventairePML} version=""1.0"" exportDate=""2025-12-20T15:30:00Z"">
  <{ImportExportConstants.XmlElements.Inventaire}>
    <{ImportExportConstants.XmlElements.Personnage}>
      <{ImportExportConstants.XmlElements.Nom}>INCOMPLETE</{ImportExportConstants.XmlElements.Nom}>
    </{ImportExportConstants.XmlElements.Personnage}>
  </{ImportExportConstants.XmlElements.Inventaire}>
</{ImportExportConstants.XmlElements.InventairePML}>";

    var stream = new MemoryStream(Encoding.UTF8.GetBytes(pmlContent));

    // Act
    var result = await service.ImportPmlAsync(stream);

    // Assert: service creates personnage with defaults for missing fields
    Assert.True(result.IsSuccess);
    var personnages = await ctx.Personnages.ToListAsync();
    Assert.NotEmpty(personnages);
    Assert.Single(personnages, p => p.Nom == "INCOMPLETE");
  }

  [Fact]
  public async Task ExportPmlAsync_ShouldIncludeAllSections()
  {
    // Arrange: create complex test data
    var personnages = await _context.Personnages.ToListAsync();
    var templates = new List<Template>
    {
        new Template { Nom = TestDataConstants.TemplateNames.MonEquipe, Description = TestDataConstants.TemplateNames.MonEquipeDescription }
    };
    templates.First().SetPersonnageIds(personnages.Select(p => p.Id).ToList());

    // Act
    var exportOptions = new PmlExportOptions();
    exportOptions.AddExportType(PmlExportOptions.EXPORT_TYPE_INVENTORY);
    exportOptions.AddExportType(PmlExportOptions.EXPORT_TYPE_TEMPLATES);
    var pmlBytes = await _pmlExportService.ExportPmlAsync(exportOptions);

    // Assert
    Assert.NotNull(pmlBytes);
    Assert.True(pmlBytes.Length > 0);
    var content = Encoding.UTF8.GetString(pmlBytes);
    Assert.Contains($"<{ImportExportConstants.XmlElements.Inventaire}>", content);
  }

  [Fact]
  public async Task ImportPmlAsync_ShouldHandleEncodingCorrectly()
  {
    // Arrange: test with accented characters
    var options = new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options;
    await using var ctx = new ApplicationDbContext(options);
    var service = new PmlImportService(ctx);

    var pmlContent = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<{ImportExportConstants.XmlElements.InventairePML} version=""1.0"" exportDate=""2025-12-20T15:30:00Z"">
  <{ImportExportConstants.XmlElements.Inventaire}>
    <{ImportExportConstants.XmlElements.Personnage}>
      <{ImportExportConstants.XmlElements.Nom}>ÉLÉONORE</{ImportExportConstants.XmlElements.Nom}>
      <{ImportExportConstants.XmlElements.Rarete}>R</{ImportExportConstants.XmlElements.Rarete}>
      <{ImportExportConstants.XmlElements.Type}>{PersonnageConstants.Types.Mercenaire}</{ImportExportConstants.XmlElements.Type}>
      <{ImportExportConstants.XmlElements.Puissance}>{TestDataConstants.NumericValues.PuissanceNiveau1000}</{ImportExportConstants.XmlElements.Puissance}>
      <{ImportExportConstants.XmlElements.PA}>{TestDataConstants.NumericValues.PAValue50}</{ImportExportConstants.XmlElements.PA}>
      <{ImportExportConstants.XmlElements.PV}>{TestDataConstants.NumericValues.PVValue100}</{ImportExportConstants.XmlElements.PV}>
      <{ImportExportConstants.XmlElements.Niveau}>{TestDataConstants.NumericValues.NiveauLevel5}</{ImportExportConstants.XmlElements.Niveau}>
      <{ImportExportConstants.XmlElements.Rang}>{TestDataConstants.NumericValues.RangValue1}</{ImportExportConstants.XmlElements.Rang}>
      <{ImportExportConstants.XmlElements.Role}>{TestDataConstants.PersonnageRoles.Guerriere}</{ImportExportConstants.XmlElements.Role}>
      <{ImportExportConstants.XmlElements.Faction}>Inconnu</{ImportExportConstants.XmlElements.Faction}>
      <{ImportExportConstants.XmlElements.Selectionne}>false</{ImportExportConstants.XmlElements.Selectionne}>
    </{ImportExportConstants.XmlElements.Personnage}>
  </{ImportExportConstants.XmlElements.Inventaire}>
</{ImportExportConstants.XmlElements.InventairePML}>";

    var stream = new MemoryStream(Encoding.UTF8.GetBytes(pmlContent));

    // Act
    var result = await service.ImportPmlAsync(stream);

    // Assert
    Assert.True(result.IsSuccess);
    var imported = await ctx.Personnages.FirstOrDefaultAsync(p => p.Nom == "ÉLÉONORE");
    Assert.NotNull(imported);
  }}