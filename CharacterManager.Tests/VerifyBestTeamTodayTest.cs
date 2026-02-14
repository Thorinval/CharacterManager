using System.IO;
using CharacterManager.Server.Data;
using CharacterManager.Server.Models;
using CharacterManager.Server.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CharacterManager.Tests;

public class VerifyBestTeamTodayTest
{
    private const string DbPath = @"d:\Devs\CharacterManager\CharacterManager\charactermanager.db";

    private static bool DbExists() => File.Exists(DbPath);

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite($"Data Source={DbPath}")
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public void Today_BestTeamPower_ShouldMatch_LiveCalculation()
    {
        if (!DbExists())
        {
            return;
        }

        using var context = CreateContext();
        var historiqueService = new HistoriqueModificationService(context);
        var personnageService = new PersonnageService(context, historiqueService, NullLogger<PersonnageService>.Instance);
        var statistiquesService = new StatistiquesService(context, personnageService);

        var bestTimeline = statistiquesService.GetBestTeamPowerEvolutionData();
        var today = DateOnly.FromDateTime(DateTime.Now);
        var todayPoint = bestTimeline.FirstOrDefault(p => p.Date == today);
        var liveBestPower = personnageService.GetPuissanceMaxEscouade();

        Assert.NotNull(todayPoint);
        Assert.Equal(liveBestPower, todayPoint!.TotalPower);
    }

    [Fact]
    public void BestTeamTimeline_ShouldNotBeEmpty_WhenDatabaseExists()
    {
        if (!DbExists())
        {
            return;
        }

        using var context = CreateContext();
        var historiqueService = new HistoriqueModificationService(context);
        var personnageService = new PersonnageService(context, historiqueService, NullLogger<PersonnageService>.Instance);
        var statistiquesService = new StatistiquesService(context, personnageService);

        var bestTimeline = statistiquesService.GetBestTeamPowerEvolutionData();

        Assert.NotNull(bestTimeline);
        Assert.NotEmpty(bestTimeline);
    }

    [Fact]
    public void BestTeamTimeline_ShouldContain_TodayOrYesterday_WhenDatabaseExists()
    {
        if (!DbExists())
        {
            return;
        }

        using var context = CreateContext();
        var historiqueService = new HistoriqueModificationService(context);
        var personnageService = new PersonnageService(context, historiqueService, NullLogger<PersonnageService>.Instance);
        var statistiquesService = new StatistiquesService(context, personnageService);

        var bestTimeline = statistiquesService.GetBestTeamPowerEvolutionData();
        var today = DateOnly.FromDateTime(DateTime.Now);
        var yesterday = today.AddDays(-1);

        var todayOrYesterday = bestTimeline.Any(p => p.Date == today || p.Date == yesterday);

        Assert.True(todayOrYesterday);
    }

    [Fact]
    public void BestTeamTimeline_DatesShouldBeInAscendingOrder()
    {
        if (!DbExists())
        {
            return;
        }

        using var context = CreateContext();
        var historiqueService = new HistoriqueModificationService(context);
        var personnageService = new PersonnageService(context, historiqueService, NullLogger<PersonnageService>.Instance);
        var statistiquesService = new StatistiquesService(context, personnageService);

        var bestTimeline = statistiquesService.GetBestTeamPowerEvolutionData();

        Assert.Equal(bestTimeline.OrderBy(p => p.Date), bestTimeline);
    }

    [Fact]
    public void BestTeamTimeline_PowerValuesShouldBeNonNegative()
    {
        if (!DbExists())
        {
            return;
        }

        using var context = CreateContext();
        var historiqueService = new HistoriqueModificationService(context);
        var personnageService = new PersonnageService(context, historiqueService, NullLogger<PersonnageService>.Instance);
        var statistiquesService = new StatistiquesService(context, personnageService);

        var bestTimeline = statistiquesService.GetBestTeamPowerEvolutionData();

        Assert.All(bestTimeline, point => Assert.True(point.TotalPower >= 0));
    }

    [Fact]
    public void Today_BestTeamPower_ShouldExistInTimeline()
    {
        if (!DbExists())
        {
            return;
        }

        using var context = CreateContext();
        var historiqueService = new HistoriqueModificationService(context);
        var personnageService = new PersonnageService(context, historiqueService, NullLogger<PersonnageService>.Instance);
        var statistiquesService = new StatistiquesService(context, personnageService);

        var bestTimeline = statistiquesService.GetBestTeamPowerEvolutionData();
        var today = DateOnly.FromDateTime(DateTime.Now);

        Assert.Contains(bestTimeline, p => p.Date == today);
    }
}
