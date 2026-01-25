using Bunit;
using Bunit.TestDoubles;
using CharacterManager.Components.Pages;
using CharacterManager.Server.Constants;
using CharacterManager.Server.Data;
using CharacterManager.Server.Models;
using CharacterManager.Server.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace CharacterManager.Tests.Components.Pages;

public class EscouadeTests : TestContext
{
    private readonly Mock<IModalService> _modalMock = new();
    private readonly Mock<IHistoriqueModificationService> _historiqueMock = new();
    private readonly Mock<IClientLocalizationService> _localizationMock = new();

    public EscouadeTests()
    {
        var auth = this.AddTestAuthorization();
        auth.SetAuthorized("tester");
        auth.SetRoles("admin");

        _localizationMock.Setup(l => l.GetKeyValue(It.IsAny<string>())).Returns<string>(k => k);
        _localizationMock.SetupGet(l => l.CurrentLanguage).Returns("fr");

        Services.AddSingleton(_modalMock.Object);
        Services.AddSingleton(_historiqueMock.Object);
        Services.AddSingleton(_localizationMock.Object);
        Services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection().Build());

        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void Empty_squad_shows_info_alert()
    {
        using var db = CreateDbContext();
        RegisterPersonnageService(db);

        var cut = RenderComponent<CascadingAuthenticationState>(p => p.AddChildContent<Escouade>());

        cut.WaitForAssertion(() => Assert.Contains("squad.noMercenary", cut.Markup));
    }

    [Fact]
    public void Clicking_header_opens_commandant_modal()
    {
        using var db = CreateDbContext();

        var commandant = new Personnage
        {
            Id = 42,
            Nom = "Cmd",
            Type = TypePersonnage.Commandant,
            Puissance = 120,
            Rang = 2,
            Selectionne = true,
            Faction = Faction.Syndicat,
            Rarete = Rarete.SSR,
            Niveau = 50
        };

        var merc1 = new Personnage { Id = 5, Nom = "Merc1", Type = TypePersonnage.Mercenaire, Puissance = 60, Selectionne = true, Rarete = Rarete.SR, Niveau = 40, Faction = Faction.Pacificateurs };
        var merc2 = new Personnage { Id = 6, Nom = "Merc2", Type = TypePersonnage.Mercenaire, Puissance = 80, Selectionne = true, Rarete = Rarete.SSR, Niveau = 45, Faction = Faction.HommesLibres };
        var andro = new Personnage { Id = 7, Nom = "Bot", Type = TypePersonnage.Androide, Puissance = 30, Selectionne = true };

        db.Personnages.AddRange(commandant, merc1, merc2, andro);
        db.LucieHouses.Add(new LucieHouse
        {
            Pieces =
            [
                new Piece
                {
                    Nom = "Salle tactique",
                    Niveau = 3,
                    Selectionnee = true,
                    AspectsTactiques = new Aspect { Nom = "Tac", Puissance = 5 },
                    AspectsStrategiques = new Aspect { Nom = "Strat", Puissance = 7 }
                }
            ]
        });
        db.SaveChanges();

        RegisterPersonnageService(db);

        var cut = RenderComponent<CascadingAuthenticationState>(p => p.AddChildContent<Escouade>());

        cut.WaitForAssertion(() => Assert.Contains("Merc1", cut.Markup));
        cut.WaitForAssertion(() => Assert.Contains("Merc2", cut.Markup));
        cut.WaitForAssertion(() => Assert.Contains("Bot", cut.Markup));

        var expectedPuissance = commandant.Puissance + (commandant.Rang * 20) + merc1.Puissance + merc2.Puissance + andro.Puissance + 12;
        cut.WaitForAssertion(() => Assert.Contains(expectedPuissance.ToString(), cut.Markup));

        cut.Find(".page-header-banner").Click();

        _modalMock.Verify(m => m.Open<CharacterManager.Components.Modal.DetailPersonnageModal>(
            It.Is<Dictionary<string, object>>(d => d.ContainsKey("PersonnageId") && (int)d["PersonnageId"] == commandant.Id),
            ModalSize.XL), Times.Once);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private void RegisterPersonnageService(ApplicationDbContext db)
    {
        Services.AddSingleton(db);
        var service = new PersonnageService(db, _historiqueMock.Object, NullLogger<PersonnageService>.Instance);
        Services.AddSingleton<IPersonnageService>(service);
    }
}