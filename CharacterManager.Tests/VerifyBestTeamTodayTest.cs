using CharacterManager.Server.Data;
using CharacterManager.Server.Models;
using CharacterManager.Server.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CharacterManager.Tests;

public class VerifyBestTeamTodayTest
{
    [Fact]
    public void Today_BestTeamPower_ShouldMatch_LiveCalculation()
    {
        // Arrange: Connexion à la vraie BDD
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite("Data Source=d:\\Devs\\CharacterManager\\CharacterManager\\charactermanager.db")
            .Options;

        using var context = new ApplicationDbContext(options);
        var historiqueService = new HistoriqueModificationService(context);
        var personnageService = new PersonnageService(context, historiqueService, NullLogger<PersonnageService>.Instance);
        var statistiquesService = new StatistiquesService(context, personnageService);

        // Act: Obtenir la timeline et la valeur live
        var bestTimeline = statistiquesService.GetBestTeamPowerEvolutionData();
        var today = DateOnly.FromDateTime(DateTime.Now);
        var todayPoint = bestTimeline.FirstOrDefault(p => p.Date == today);
        var liveBestPower = personnageService.GetPuissanceMaxEscouade();

        // Assert
        Assert.NotNull(todayPoint);
        Assert.Equal(liveBestPower, todayPoint.TotalPower);
    }
}
