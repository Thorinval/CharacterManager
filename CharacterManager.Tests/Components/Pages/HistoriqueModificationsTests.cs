using System;
using System.Linq;
using Bunit;
using CharacterManager.Components.Pages;
using CharacterManager.Server.Data;
using CharacterManager.Server.Models;
using CharacterManager.Server.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CharacterManager.Tests.Components.Pages;

public class HistoriqueModificationsTests : TestContext
{
    private readonly ApplicationDbContext _db;

    public HistoriqueModificationsTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(options);

        var service = new HistoriqueModificationService(_db);

        Services.AddSingleton(_db);
        Services.AddSingleton(service);

        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.Setup<bool>("confirm", _ => true).SetResult(true);
        JSInterop.SetupVoid("alert", _ => true);
        JSInterop.SetupVoid("downloadFile", _ => true);
    }

    [Fact]
    public void Empty_history_shows_info_alert()
    {
        var cut = RenderComponent<HistoriqueModifications>();

        cut.WaitForAssertion(() => Assert.Contains("Aucune modification enregistrée.", cut.Markup));
    }

    [Fact]
    public void Delete_selection_removes_entries()
    {
        _db.HistoriquesModifications.Add(new HistoriqueModification
        {
            Id = 1,
            TypeEntite = TypeEntite.Personnage,
            TypeModification = TypeModification.Creation,
            NomEntite = "Alpha",
            ChampModifie = "Niveau",
            AncienneValeur = "1",
            NouvelleValeur = "2",
            DateModification = DateTime.UtcNow,
            DateInsertion = DateTime.UtcNow,
            DateMiseAJour = DateTime.UtcNow
        });
        _db.SaveChanges();

        var cut = RenderComponent<HistoriqueModifications>();

        cut.WaitForAssertion(() => Assert.Contains("Alpha", cut.Markup));

        cut.Find("tbody input[type=checkbox]").Change(true);
        cut.WaitForAssertion(() => cut.FindAll("button").Any(b => b.TextContent.Contains("Supprimer la sélection")));
        cut.FindAll("button").Single(b => b.TextContent.Contains("Supprimer la sélection")).Click();

        cut.WaitForAssertion(() => Assert.Empty(_db.HistoriquesModifications.AsNoTracking()));

        var refreshed = RenderComponent<HistoriqueModifications>();
        refreshed.WaitForAssertion(() => Assert.Contains("Aucune modification enregistrée.", refreshed.Markup));
    }
}
