using Bunit;
using Bunit.TestDoubles;
using CharacterManager.Components.Pages;
using CharacterManager.Server.Data;
using CharacterManager.Server.Models;
using CharacterManager.Server.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CharacterManager.Tests.Components.Pages;

public class MeilleurEscouadeTests : TestContext
{
    private readonly Mock<IPersonnageService> _personnageServiceMock = new();
    private readonly Mock<IModalService> _modalServiceMock = new();
    private readonly Mock<IClientLocalizationService> _localizationMock = new();

    public MeilleurEscouadeTests()
    {
        var auth = this.AddTestAuthorization();
        auth.SetAuthorized("tester");

        _localizationMock.Setup(l => l.GetKeyValue(It.IsAny<string>())).Returns<string>(k => k);
        _localizationMock.SetupGet(l => l.CurrentLanguage).Returns("fr");

        Services.AddSingleton(_personnageServiceMock.Object);
        Services.AddSingleton(_modalServiceMock.Object);
        Services.AddSingleton(_localizationMock.Object);
        Services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection().Build());

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        Services.AddSingleton(new ApplicationDbContext(options));

        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void Renders_best_squad_and_opens_modal_on_click()
    {
        var commandant = new Personnage { Id = 1, Nom = "Cmd", Type = TypePersonnage.Commandant, Puissance = 200, Rang = 1, Selectionne = true };
        var merc1 = new Personnage { Id = 2, Nom = "Merc1", Type = TypePersonnage.Mercenaire, Puissance = 150, Selectionne = true };
        var merc2 = new Personnage { Id = 3, Nom = "Merc2", Type = TypePersonnage.Mercenaire, Puissance = 140, Selectionne = true };
        var andro = new Personnage { Id = 4, Nom = "Bot", Type = TypePersonnage.Androide, Puissance = 90, Selectionne = true };
        var piece = new Piece
        {
            Nom = "Salle tactique",
            Niveau = 4,
            Selectionnee = true,
            AspectsTactiques = new Aspect { Nom = "Tac", Puissance = 6 },
            AspectsStrategiques = new Aspect { Nom = "Strat", Puissance = 8 }
        };

        _personnageServiceMock.Setup(p => p.GetTopMercenaires(It.IsAny<int>())).Returns(new List<Personnage> { merc1, merc2 });
        _personnageServiceMock.Setup(p => p.GetTopAndroides(It.IsAny<int>())).Returns(new List<Personnage> { andro });
        _personnageServiceMock.Setup(p => p.GetTopCommandant()).Returns(commandant);
        _personnageServiceMock.Setup(p => p.GetPuissanceMaxEscouade()).Returns(999);
        _personnageServiceMock.Setup(p => p.GetTopLucieRooms(It.IsAny<int>())).Returns(new List<Piece> { piece });

        var cut = RenderComponent<CascadingAuthenticationState>(p => p.AddChildContent<MeilleurEscouade>());

        cut.WaitForAssertion(() => Assert.Contains("999", cut.Markup));
        cut.WaitForAssertion(() => Assert.Contains("Merc1", cut.Markup));
        cut.WaitForAssertion(() => Assert.Contains("Bot", cut.Markup));

        cut.Find(".page-header-banner").Click();

        cut.Find(".personnage-card.clickable").Click();

        _modalServiceMock.Verify(m => m.Open<CharacterManager.Components.Modal.DetailPersonnageModal>(
            It.Is<Dictionary<string, object>>(d => d.ContainsKey("PersonnageId") && (int)d["PersonnageId"] == commandant.Id),
            ModalSize.XL), Times.AtLeastOnce);
    }

    [Fact]
    public void Shows_lucie_alert_when_no_piece()
    {
        _personnageServiceMock.Setup(p => p.GetTopMercenaires(It.IsAny<int>())).Returns(new List<Personnage>());
        _personnageServiceMock.Setup(p => p.GetTopAndroides(It.IsAny<int>())).Returns(new List<Personnage>());
        _personnageServiceMock.Setup(p => p.GetTopCommandant()).Returns((Personnage?)null);
        _personnageServiceMock.Setup(p => p.GetPuissanceMaxEscouade()).Returns(0);
        _personnageServiceMock.Setup(p => p.GetTopLucieRooms(It.IsAny<int>())).Returns(new List<Piece>());

        var cut = RenderComponent<CascadingAuthenticationState>(p => p.AddChildContent<MeilleurEscouade>());

        cut.WaitForAssertion(() => Assert.Contains("Aucune pièce sélectionnée", cut.Markup));
    }
}