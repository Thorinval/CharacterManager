using System;
using System.Collections.Generic;
using System.Linq;
using Bunit;
using Bunit.TestDoubles;
using CharacterManager.Components.Pages;
using CharacterManager.Server.Data;
using CharacterManager.Server.Models;
using CharacterManager.Server.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace CharacterManager.Tests.Components.Pages;

public class TemplatesTests : TestContext
{
    private readonly ApplicationDbContext _db;
    private readonly Mock<IClientLocalizationService> _loc = new();

    public TemplatesTests()
    {
        this.AddTestAuthorization().SetAuthorized("tester");

        var historique = new Mock<IHistoriqueModificationService>();

        _loc.Setup(l => l.GetKeyValue(It.IsAny<string>())).Returns<string>(k => k);
        _loc.SetupGet(l => l.CurrentLanguage).Returns("fr");

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(options);

        var personnageService = new PersonnageService(_db, historique.Object, NullLogger<PersonnageService>.Instance);
        var importService = new PmlImportService(_db, historique.Object);
        var exportService = new PmlExportService(_db, NullLogger<PmlExportService>.Instance);

        Services.AddSingleton(_db);
        Services.AddSingleton<IPersonnageService>(personnageService);
        Services.AddSingleton(importService);
        Services.AddSingleton<IPmlImportService>(importService);
        Services.AddSingleton(exportService);
        Services.AddSingleton<IPmlExportService>(exportService);
        Services.AddSingleton(historique.Object);

        _db.Database.EnsureCreated();
        Services.AddSingleton(_loc.Object);
        Services.AddSingleton<IClientLocalizationService>(_loc.Object);

        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.SetupVoid("downloadFile", _ => true);
        JSInterop.Setup<bool>("confirm", _ => true).SetResult(true);
    }

    private void ResetDatabase()
    {
        _db.Database.EnsureDeleted();
        _db.Database.EnsureCreated();
    }

    [Fact]
    public void Empty_templates_show_empty_message()
    {
        ResetDatabase();
        var cut = RenderComponent<CascadingAuthenticationState>(p => p.AddChildContent<Templates>());

        cut.WaitForAssertion(() => Assert.Contains("templates.empty", cut.Markup));
    }

    [Fact]
    public void Rename_template_updates_database()
    {
        ResetDatabase();
        var personnage = new Personnage { Id = 1, Nom = "Merc", Puissance = 10, Selectionne = true };
        _db.Personnages.Add(personnage);
        var template = new Template
        {
            Id = 10,
            Nom = "Base",
            Description = "Desc",
            DateCreation = DateTime.UtcNow,
            DateModification = DateTime.UtcNow,
            PuissanceTotal = 15
        };
        template.SetPersonnageIds(new List<int> { 1 });
        _db.Templates.Add(template);
        _db.SaveChanges();

        var cut = RenderComponent<CascadingAuthenticationState>(p => p.AddChildContent<Templates>());

        cut.WaitForAssertion(() => cut.Find("button.btn-outline-secondary").Click());
        cut.Find("input.form-control").Change("Renamed");
        cut.Find("button.btn-success").Click();

        cut.WaitForAssertion(() =>
        {
            var updated = _db.Templates.AsNoTracking().Single(t => t.Id == 10);
            Assert.Equal("Renamed", updated.Nom);
        });
    }

    [Fact]
    public void Export_template_triggers_download()
    {
        ResetDatabase();
        var template = new Template
        {
            Id = 20,
            Nom = "Exportable",
            DateCreation = DateTime.UtcNow,
            DateModification = DateTime.UtcNow,
            PuissanceTotal = 5
        };
        template.SetPersonnageIds(new List<int>());
        _db.Templates.Add(template);
        _db.SaveChanges();

        var cut = RenderComponent<CascadingAuthenticationState>(p => p.AddChildContent<Templates>());

        cut.WaitForAssertion(() => cut.Find("button.btn-info").Click());

        Assert.Contains(JSInterop.Invocations, i => i.Identifier == "downloadFile");
    }
}
