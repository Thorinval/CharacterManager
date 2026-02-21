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

public class ClassementsTests : TestContext
{
    private readonly Mock<IHistoriqueClassementService> _historique = new();
    private readonly Mock<IPmlExportService> _export = new();
    private readonly Mock<IPmlImportService> _import = new();
    private readonly Mock<IModalService> _modal = new();
    private readonly Mock<IClientLocalizationService> _loc = new();

    public ClassementsTests()
    {
        this.AddTestAuthorization().SetAuthorized("tester");

        _loc.Setup(l => l.GetKeyValue(It.IsAny<string>())).Returns<string>(k => k);
        _loc.SetupGet(l => l.CurrentLanguage).Returns("fr");

        Services.AddSingleton(_historique.Object);
        Services.AddSingleton(_export.Object);
        Services.AddSingleton(_import.Object);
        Services.AddSingleton(_modal.Object);
        Services.AddSingleton(_loc.Object);

        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.SetupVoid("downloadFile", _ => true);
        JSInterop.Setup<bool>("confirm", _ => true).SetResult(true);
        JSInterop.SetupVoid("alert", _ => true);
    }

    [Fact]
    public void Empty_state_shows_alert()
    {
        _historique.Setup(h => h.GetHistoriqueAsync()).ReturnsAsync(new List<HistoriqueClassement>());

        var cut = RenderComponent<CascadingAuthenticationState>(p => p.AddChildContent<Classements>());

        cut.WaitForAssertion(() => Assert.Contains("ranking.empty", cut.Markup));
    }

    [Fact]
    public void Renders_rows_and_exports_file()
    {
        var histo = new HistoriqueClassement
        {
            Id = 1,
            DateEnregistrement = DateOnly.FromDateTime(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
            Ligue = 1,
            Score = 1234,
            PuissanceTotale = 5678,
            Commandant = new PersonnageClassement { Nom = "Cmd", Rang = 2, Niveau = 50, Puissance = 200 },
            Mercenaires = new List<PersonnageClassement>
            {
                new() { Nom = "Merc1", Puissance = 100, Rang = 1, Niveau = 30 },
                new() { Nom = "Merc2", Puissance = 110, Rang = 1, Niveau = 31 }
            },
            Androides = new List<PersonnageClassement>
            {
                new() { Nom = "Bot", Puissance = 90, Rang = 0, Niveau = 20 }
            },
            Pieces = new List<PieceHistorique>
            {
                new() { Nom = "Salle tactique", Niveau = 2, Selectionnee = true, AspectsStrategiques = new Aspect { Puissance = 3 }, HistoriqueClassementId = 1 }
            },
            Classements = new List<Classement>
            {
                new() { Type = TypeClassement.Nutaku, Valeur = 10 },
                new() { Type = TypeClassement.Top150, Valeur = 20 },
                new() { Type = TypeClassement.France, Valeur = 30 }
            }
        };

        _historique.Setup(h => h.GetHistoriqueAsync()).ReturnsAsync(new List<HistoriqueClassement> { histo });
        _export.Setup(e => e.ExportPmlAsync(It.IsAny<PmlExportOptions>())).ReturnsAsync(new byte[] { 1, 2, 3 });

        var cut = RenderComponent<CascadingAuthenticationState>(p => p.AddChildContent<Classements>());

        cut.WaitForAssertion(() => Assert.Contains("Merc1", cut.Markup));
        cut.WaitForAssertion(() => Assert.Contains("Bot", cut.Markup));
        cut.WaitForAssertion(() => Assert.Contains("Salle tactique", cut.Markup));

        cut.Find("button.btn-success").Click();

        _export.Verify(e => e.ExportPmlAsync(It.Is<PmlExportOptions>(o => o.IsExporting(PmlExportOptions.EXPORT_TYPE_HISTORIES))), Times.Once);
        Assert.Contains(JSInterop.Invocations, i => i.Identifier == "downloadFile");
    }
}