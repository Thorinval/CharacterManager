using System.Collections.Generic;
using System.Linq;
using Bunit;
using Bunit.TestDoubles;
using CharacterManager.Components.Pages;
using CharacterManager.Server.Constants;
using CharacterManager.Server.Models;
using CharacterManager.Server.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CharacterManager.Tests.Components.Pages;

public class StatistiquesTests : TestContext
{
    private readonly Mock<IPersonnageService> _personnages = new();
    private readonly Mock<IStatistiquesService> _stats = new();
    private readonly Mock<IClientLocalizationService> _loc = new();

    public StatistiquesTests()
    {
        this.AddTestAuthorization().SetAuthorized("tester");

        _loc.Setup(l => l.GetKeyValue(It.IsAny<string>())).Returns<string>(k => k);
        _loc.SetupGet(l => l.CurrentLanguage).Returns("fr");

        Services.AddSingleton(_personnages.Object);
        Services.AddSingleton(_stats.Object);
        Services.AddSingleton(_loc.Object);

        JSInterop.Mode = JSRuntimeMode.Loose;
        var chartsModule = JSInterop.SetupModule("/js/charts.js");
        chartsModule.SetupVoid("createPieChart", _ => true);
        chartsModule.SetupVoid("createLineChart", _ => true);
        chartsModule.SetupVoid("createBarChart", _ => true);
    }

    [Fact]
    public void Empty_state_when_no_mercenaires()
    {
        _personnages.Setup(p => p.GetMercenaires(false)).Returns(new List<Personnage>());

        var cut = RenderComponent<CascadingAuthenticationState>(p => p.AddChildContent<Statistiques>());

        cut.WaitForAssertion(() => Assert.Contains("statistics.noData", cut.Markup));
    }

    [Fact]
    public void Charts_render_with_data_and_js_calls()
    {
        var mercenaires = new List<Personnage>
        {
            new() { Id = 1, Nom = "Cmd", TypeAttaque = TypeAttaque.Melee, Faction = Faction.Syndicat, Rang = 2, Puissance = 120, Niveau = 50 },
            new() { Id = 2, Nom = "Merc", TypeAttaque = TypeAttaque.Distance, Faction = Faction.Pacificateurs, Rang = 1, Puissance = 80, Niveau = 30 }
        };
        _personnages.Setup(p => p.GetMercenaires(false)).Returns(mercenaires);

        _stats.Setup(s => s.GetLevelEvolutionData()).Returns(new List<LevelEvolutionData>
        {
            new()
            {
                Date = new System.DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                LevelsByPersonnage = new Dictionary<string, int> { { "Cmd", 50 } }
            },
            new()
            {
                Date = new System.DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc),
                LevelsByPersonnage = new Dictionary<string, int> { { "Merc", 30 } }
            }
        });
        _stats.Setup(s => s.GetPersonnagesWithHistory(It.IsAny<List<LevelEvolutionData>>()))
            .Returns(new List<string> { "Cmd", "Merc" });
        var minLevel = 1;
        _stats.Setup(s => s.CreateChartDatasets(It.IsAny<List<LevelEvolutionData>>(), It.IsAny<List<string>>(), out minLevel))
            .Returns(new List<object> { new { label = "Levels", data = new List<int> { 50, 30 } } });
        _stats.Setup(s => s.GetSelectedTeamPowerEvolutionData()).Returns(new List<TeamPowerEvolutionData>
        {
            new() { Date = new System.DateOnly(2024, 1, 1), TotalPower = 100 }
        });
        _stats.Setup(s => s.GetBestTeamPowerEvolutionData()).Returns(new List<TeamPowerEvolutionData>());
        _stats.Setup(s => s.GetClassementEvolutionData()).Returns(new List<ClassementEvolutionData>
        {
            new() { Date = new System.DateOnly(2024, 1, 1), Classements = new Dictionary<TypeClassement, int> { { TypeClassement.Nutaku, 5 } } }
        });
        _stats.Setup(s => s.FormatDateWithDay(It.IsAny<System.DateTime>())).Returns<System.DateTime>(d => d.ToString("yyyy-MM-dd"));
        _stats.Setup(s => s.FormatDateForClassement(It.IsAny<System.DateOnly>())).Returns<System.DateOnly>(d => d.ToString("yyyy-MM-dd"));
        _stats.Setup(s => s.ColorWithAlpha(It.IsAny<string>(), It.IsAny<double>())).Returns<string, double>((c, _) => c);

        var cut = RenderComponent<CascadingAuthenticationState>(p => p.AddChildContent<Statistiques>());

        cut.WaitForAssertion(() => Assert.Contains("statistics.chartTitleAttackType", cut.Markup));
        Assert.Contains(JSInterop.Invocations, i => i.Identifier.Contains("createPieChart"));
    }
}
