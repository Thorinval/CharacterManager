using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Bunit.TestDoubles;
using CharacterManager.Components.Pages;
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

public class MaisonLucieTests : TestContext
{
    private readonly ApplicationDbContext _db;
    private readonly Mock<IHistoriqueModificationService> _historique = new();

    public MaisonLucieTests()
    {
        this.AddTestAuthorization().SetAuthorized("tester");

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(options);

        var env = new Mock<IWebHostEnvironment>();
        env.SetupGet(e => e.WebRootPath).Returns(Path.Combine(Directory.GetCurrentDirectory(), "CharacterManager", "wwwroot"));
        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var languageContext = new LanguageContextService();
        var httpAccessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        var localization = new ClientLocalizationService(env.Object, NullLogger<ClientLocalizationService>.Instance, languageContext, httpAccessor);

        var personnageService = new PersonnageService(_db, _historique.Object, NullLogger<PersonnageService>.Instance);
        var importService = new PmlImportService(_db, _historique.Object);
        var exportService = new PmlExportService(_db, NullLogger<PmlExportService>.Instance);

        Services.AddSingleton(_db);
        Services.AddSingleton<IPersonnageService>(personnageService);
        Services.AddSingleton(importService);
        Services.AddSingleton(exportService);
        Services.AddSingleton(_historique.Object);
        Services.AddSingleton(env.Object);
        Services.AddSingleton<IConfiguration>(config);
        Services.AddSingleton(languageContext);
        Services.AddSingleton<IHttpContextAccessor>(httpAccessor);
        Services.AddSingleton(localization);
        Services.AddSingleton<IClientLocalizationService>(localization);

        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.SetupVoid("alert", _ => true);
        JSInterop.SetupVoid("downloadFile", _ => true);
        _db.Database.EnsureCreated();
    }

    private void ResetDatabase()
    {
        _db.Database.EnsureDeleted();
        _db.Database.EnsureCreated();
    }

    [Fact]
    public void Empty_pieces_show_empty_state()
    {
        ResetDatabase();
        _db.LucieHouses.Add(new LucieHouse { Id = 1, Affection = 0 });
        _db.SaveChanges();

        var cut = RenderComponent<CascadingAuthenticationState>(p => p.AddChildContent<MaisonLucie>());

        var emptyState = cut.WaitForElement(".empty-state");
        Assert.NotNull(emptyState);
    }

    [Fact]
    public void Affection_edit_updates_value()
    {
        ResetDatabase();
        _db.LucieHouses.Add(new LucieHouse { Affection = 10 });
        _db.Pieces.Add(new Piece
        {
            Id = 101,
            Nom = "Salle tactique",
            Niveau = 1,
            Selectionnee = true,
            AspectsTactiques = new Aspect { Nom = "Tac", Puissance = 2, Bonus = new List<string>() },
            AspectsStrategiques = new Aspect { Nom = "Strat", Puissance = 3, Bonus = new List<string>() }
        });
        _db.SaveChanges();

        var cut = RenderComponent<CascadingAuthenticationState>(p => p.AddChildContent<MaisonLucie>());

        cut.WaitForAssertion(() => cut.Find("div.affection-badge").Click());
        cut.Find("div.affection-editor input").Change(42);
        cut.Find("div.affection-editor button.btn-primary").Click();

        cut.WaitForAssertion(() =>
        {
            var updated = _db.LucieHouses.AsNoTracking().Single();
            Assert.Equal(42, updated.Affection);
        });
    }

    [Fact]
    public async Task Piece_edit_persists_changes()
    {
        ResetDatabase();
        _db.LucieHouses.Add(new LucieHouse { Id = 1, Affection = 5 });
        var piece = new Piece
        {
            Id = 202,
            Nom = "Salle stratégique",
            Niveau = 2,
            Selectionnee = false,
            AspectsTactiques = new Aspect { Nom = "Tac", Puissance = 2, Bonus = new List<string>() },
            AspectsStrategiques = new Aspect { Nom = "Strat", Puissance = 4, Bonus = new List<string>() }
        };
        _db.Pieces.Add(piece);
        await _db.SaveChangesAsync();

        var cascade = RenderComponent<CascadingAuthenticationState>(p => p.AddChildContent<MaisonLucie>());
        var page = cascade.FindComponent<MaisonLucie>();

        var startEdit = page.Instance.GetType().GetMethod("StartPieceEdit", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var saveEdit = page.Instance.GetType().GetMethod("SavePieceEdit", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        await page.InvokeAsync(() => startEdit!.Invoke(page.Instance, new object[] { piece }));

        var draftField = page.Instance.GetType().GetField("pieceEditDraft", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var draft = (Piece)draftField!.GetValue(page.Instance)!;
        draft.Niveau = 5;
        draft.Selectionnee = true;

        await page.InvokeAsync(async () => await (Task)saveEdit!.Invoke(page.Instance, new object[] { piece })!);

        cascade.WaitForAssertion(() =>
        {
            var piece = _db.Pieces.AsNoTracking().Single(p => p.Id == 202);
            Assert.Equal(5, piece.Niveau);
            Assert.True(piece.Selectionnee);
        });
    }
}
