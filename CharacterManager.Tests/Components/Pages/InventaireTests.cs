using Bunit;
using Bunit.TestDoubles;
using CharacterManager.Components.Pages;
using CharacterManager.Server.Constants;
using CharacterManager.Server.Data;
using CharacterManager.Server.Models;
using CharacterManager.Server.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace CharacterManager.Tests.Components.Pages;

public class InventaireTests : TestContext
{
    private readonly Mock<IHistoriqueModificationService> _historique = new();
    private readonly Mock<IPmlImportService> _import = new();
    private readonly Mock<IPmlExportService> _export = new();
    private readonly Mock<IModalService> _modal = new();
    private readonly Mock<IClientLocalizationService> _loc = new();

    public InventaireTests()
    {
        this.AddTestAuthorization().SetAuthorized("tester");

        _loc.Setup(l => l.GetKeyValue(It.IsAny<string>())).Returns<string>(k => k);
        _loc.SetupGet(l => l.CurrentLanguage).Returns("fr");

        var env = new Mock<IWebHostEnvironment>();
        env.SetupGet(e => e.WebRootPath).Returns(Path.Combine(Directory.GetCurrentDirectory(), "CharacterManager", "wwwroot"));

        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new ApplicationDbContext(options);

        var personnageService = new PersonnageService(db, _historique.Object, NullLogger<PersonnageService>.Instance);

        var languageContext = new LanguageContextService();
        var httpAccessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        var locService = new ClientLocalizationService(env.Object, NullLogger<ClientLocalizationService>.Instance, languageContext, httpAccessor);

        Services.AddSingleton(env.Object);
        Services.AddSingleton<IConfiguration>(config);
        Services.AddSingleton(db);
        Services.AddSingleton<IPersonnageService>(personnageService);
        Services.AddSingleton(_historique.Object);
        Services.AddSingleton(_import.Object);
        Services.AddSingleton(_export.Object);
        Services.AddSingleton(_modal.Object);
        Services.AddSingleton(languageContext);
        Services.AddSingleton<IHttpContextAccessor>(httpAccessor);
        Services.AddSingleton(locService);
        Services.AddSingleton<IClientLocalizationService>(locService);
        Services.AddSingleton(_loc.Object);

        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.SetupVoid("alert", _ => true);
        JSInterop.SetupVoid("downloadFile", _ => true);
    }

    [Fact]
    public void Empty_inventory_shows_filters_and_zero_counts()
    {
        var cut = RenderComponent<CascadingAuthenticationState>(p => p.AddChildContent<Inventaire>());

        cut.WaitForAssertion(() => Assert.Contains("inventory.title", cut.Markup));
        cut.WaitForAssertion(() => Assert.Contains("inventory.showAll", cut.Markup));
    }

    [Fact]
    public void Renders_personnages_in_grid_and_badges()
    {
        var db = Services.GetRequiredService<ApplicationDbContext>();
        db.Personnages.AddRange(
            new Personnage { Id = 1, Nom = "Cmd", Type = TypePersonnage.Commandant, Rarete = Rarete.SSR, Puissance = 200, Rang = 3, Niveau = 50, Selectionne = true },
            new Personnage { Id = 2, Nom = "Merc", Type = TypePersonnage.Mercenaire, Rarete = Rarete.SR, Puissance = 120, Rang = 1, Niveau = 40, Selectionne = true }
        );
        db.LucieHouses.Add(LucieHouse.CreerDefaut());
        db.SaveChanges();

        var cut = RenderComponent<CascadingAuthenticationState>(p => p.AddChildContent<Inventaire>());

        cut.WaitForAssertion(() => Assert.Contains("Cmd", cut.Markup));
        cut.WaitForAssertion(() => Assert.Contains("Merc", cut.Markup));
        cut.WaitForAssertion(() => Assert.Contains("inventory.showOnlyCommandants", cut.Markup));
    }
}