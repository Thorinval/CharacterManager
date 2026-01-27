using CharacterManager.Server.Data;
using CharacterManager.Server.Models;
using CharacterManager.Server.Services;
using Microsoft.EntityFrameworkCore;
using CharacterManager.Server.Constants;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using System;

namespace CharacterManager.Tests;

public class LuciePowerTodayOverrideTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly PersonnageService _personnageService;
    private readonly StatistiquesService _service;

    public LuciePowerTodayOverrideTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();
        var historiqueService = new HistoriqueModificationService(_context);
        var loggerMock = new Mock<ILogger<PersonnageService>>();
        _personnageService = new PersonnageService(_context, historiqueService, loggerMock.Object);
        _service = new StatistiquesService(_context, _personnageService);
    }

    [Fact]
    public async System.Threading.Tasks.Task TodayValues_ShouldOverrideClassementAndWrongLucieHistory()
    {
        // Arrange: pieces with differing selected vs max tactical power
        var p1 = new Piece { Nom = "P1", Selectionnee = true, AspectsTactiques = new Aspect { Puissance = 10 }, AspectsStrategiques = new Aspect { Puissance = 50 } };
        var p2 = new Piece { Nom = "P2", Selectionnee = false, AspectsTactiques = new Aspect { Puissance = 20 }, AspectsStrategiques = new Aspect { Puissance = 50 } };
        var p3 = new Piece { Nom = "P3", Selectionnee = false, AspectsTactiques = new Aspect { Puissance = 30 }, AspectsStrategiques = new Aspect { Puissance = 50 } };
        _context.Pieces.AddRange(p1, p2, p3);

        // Premier classement in the past
        _context.HistoriquesClassement.Add(new HistoriqueClassement
        {
            DateEnregistrement = new DateOnly(2026, 1, 1),
            PuissanceTotale = 0
        });

        // Erroneous history for today: max equals selected
        var today = DateTime.Now.Date;
        var wrongSelected = 10 + 50 + 50 + 50; // GetPuissanceLucieEscouade (only selected tactique + all strategic)
        _context.HistoriquesModifications.AddRange(
            new HistoriqueModification
            {
                TypeEntite = TypeEntite.Piece,
                EntiteId = -1,
                NomEntite = "Lucie (Sélectionnée)",
                TypeModification = TypeModification.Modification,
                DateModification = today,
                DateInsertion = today,
                DateMiseAJour = today,
                ChampModifie = StatisticsConstants.HistoryFields.PuissanceLucieSelectionnee,
                AncienneValeur = "0",
                NouvelleValeur = wrongSelected.ToString(),
                Description = "Puissance Lucie (Sélectionnée)"
            },
            new HistoriqueModification
            {
                TypeEntite = TypeEntite.Piece,
                EntiteId = -2,
                NomEntite = "Lucie (Max)",
                TypeModification = TypeModification.Modification,
                DateModification = today,
                DateInsertion = today,
                DateMiseAJour = today,
                ChampModifie = StatisticsConstants.HistoryFields.PuissanceLucieMax,
                AncienneValeur = "0",
                NouvelleValeur = wrongSelected.ToString(),
                Description = "Puissance Lucie (Max)"
            }
        );

        // Also a classement for today containing the wrong total
        _context.HistoriquesClassement.Add(new HistoriqueClassement
        {
            DateEnregistrement = DateOnly.FromDateTime(today),
            PuissanceTotale = wrongSelected
        });

        await _context.SaveChangesAsync();

        // Act
        var best = _service.GetBestTeamPowerEvolutionData();
        var selected = _service.GetSelectedTeamPowerEvolutionData();

        var todayBest = best.FindLast(e => e.Date == DateOnly.FromDateTime(today));
        var todaySelected = selected.FindLast(e => e.Date == DateOnly.FromDateTime(today));

        // Expected values from live methods
        var expectedMax = _personnageService.GetPuissanceMaxEscouade();
        var expectedSelected = _personnageService.GetPuissanceEscouade();

        // Assert: today's values must be recalculated and differ from wrong history if applicable
        Assert.NotNull(todayBest);
        Assert.NotNull(todaySelected);
        Assert.Equal(expectedMax, todayBest!.TotalPower);
        Assert.Equal(expectedSelected, todaySelected!.TotalPower);
        Assert.NotEqual(wrongSelected, todayBest.TotalPower); // ensures override happened
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
