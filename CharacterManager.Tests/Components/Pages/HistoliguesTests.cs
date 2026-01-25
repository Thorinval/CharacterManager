using System;
using System.IO;
using System.Linq;
using Bunit;
using CharacterManager.Components.Pages;
using CharacterManager.Server.Data;
using CharacterManager.Server.Models;
using CharacterManager.Server.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace CharacterManager.Tests.Components.Pages;

public class HistoliguesTests : TestContext
{
    private readonly ApplicationDbContext _dbContext;

    public HistoliguesTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _dbContext = new ApplicationDbContext(options);

        var webRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(Path.Combine(webRoot, "i18n"));
        File.WriteAllText(Path.Combine(webRoot, "i18n", "fr.json"),
            "{\"leagueHistory\":{\"title\":\"title\",\"newEntry\":\"new\",\"emptyPrompt\":\"empty\",\"addFirst\":\"add\",\"table\":{\"date\":\"date\",\"league\":\"league\",\"notes\":\"notes\",\"actions\":\"actions\"},\"badge\":{\"eliteTop50\":\"Elite\",\"leagueN\":\"Ligue {0}\"},\"labels\":{\"dateMontee\":\"date\",\"ligue\":\"ligue\",\"select\":\"select\",\"notesOptional\":\"notes\",\"notesPlaceholder\":\"notes\"},\"modal\":{\"editTitle\":\"edit\",\"createTitle\":\"create\"},\"confirmDelete\":\"confirm\",\"import\":{\"selectPml\":\"select pml\",\"successCount\":\"{0}\",\"none\":\"none\",\"detailsPreview\":\"preview\"}},\"common\":{\"export\":\"export\",\"import\":\"import\",\"loading\":\"loading\",\"cancel\":\"cancel\",\"update\":\"update\",\"add\":\"add\",\"edit\":\"edit\",\"delete\":\"delete\"},\"errors\":{\"exportError\":\"export error\",\"importError\":\"import error\"}}");

        var env = new Mock<IWebHostEnvironment>();
        env.SetupGet(e => e.WebRootPath).Returns(webRoot);

        var languageContext = new LanguageContextService();
        var httpAccessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        var localizationService = new ClientLocalizationService(env.Object, NullLogger<ClientLocalizationService>.Instance, languageContext, httpAccessor);
        localizationService.InitializeAsync("fr").GetAwaiter().GetResult();

        Services.AddSingleton(_dbContext);
        Services.AddSingleton(new HistoriqueLigueService(_dbContext));
        Services.AddSingleton(new PmlExportService(_dbContext, NullLogger<PmlExportService>.Instance));
        Services.AddSingleton(new PmlImportService(_dbContext));
        Services.AddSingleton<IClientLocalizationService>(localizationService);
        Services.AddSingleton(languageContext);
        Services.AddSingleton<IHttpContextAccessor>(httpAccessor);
    }

    [Fact]
    public void Empty_state_rendered_when_no_history()
    {
        var cut = RenderComponent<Histoligues>();

        cut.WaitForAssertion(() => Assert.Contains("empty-state", cut.Markup));
    }

    [Fact]
    public void Can_create_new_historique_entry()
    {
        var cut = RenderComponent<Histoligues>();

        var addButton = cut.Find("button.btn.btn-primary");
        addButton.Click();

        cut.WaitForAssertion(() => Assert.Contains("modal-overlay", cut.Markup));

        var select = cut.Find("select.form-control");
        select.Change("2");

        var modalButtons = cut.FindAll("button.btn.btn-primary");
        modalButtons[modalButtons.Count - 1].Click();

        cut.WaitForAssertion(() => Assert.Single(_dbContext.HistoriquesLigue));
        Assert.Contains("ligue-2", cut.Markup);
    }

    [Fact]
    public void Delete_confirms_and_removes_entry()
    {
        _dbContext.HistoriquesLigue.Add(new HistoriqueLigue
        {
            DateMontee = new DateOnly(2024, 1, 1),
            Ligue = 5,
            Notes = "note"
        });
        _dbContext.SaveChanges();

        var confirm = JSInterop.Setup<bool>("confirm", _ => true);
        confirm.SetResult(true);

        var cut = RenderComponent<Histoligues>();

        cut.WaitForAssertion(() => Assert.Contains("ligue-5", cut.Markup));

        cut.Find("button.btn-delete").Click();

        cut.WaitForAssertion(() => Assert.Empty(_dbContext.HistoriquesLigue));
        Assert.DoesNotContain("ligue-5", cut.Markup);
    }

    [Fact]
    public void Export_triggers_file_download()
    {
        _dbContext.HistoriquesLigue.Add(new HistoriqueLigue
        {
            DateMontee = new DateOnly(2024, 2, 1),
            Ligue = 3
        });
        _dbContext.SaveChanges();

        JSInterop.SetupVoid("downloadFile", _ => true);

        var cut = RenderComponent<Histoligues>();

        var exportButton = cut.Find("button.btn.btn-success");
        exportButton.Click();

        Assert.Contains(JSInterop.Invocations, i => i.Identifier == "downloadFile");
    }
}
