using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using CharacterManager.Components.Pages.Admin;
using CharacterManager.Scripts;
using CharacterManager.Server.Data;
using CharacterManager.Server.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CharacterManager.Tests.Components.Pages.Admin;

public class CleanupDuplicatesTests : TestContext
{
    private readonly ApplicationDbContext _dbContext;

    public CleanupDuplicatesTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _dbContext = new ApplicationDbContext(options);

        Services.AddSingleton<IApplicationDbContext>(_dbContext);
    }

    [Fact]
    public void Page_renders_warning_and_preview_button()
    {
        var cut = RenderComponent<CleanupDuplicates>();

        Assert.Contains("ATTENTION", cut.Markup);
        Assert.Contains("Prévisualiser", cut.Markup);
        Assert.DoesNotContain("Exécuter le nettoyage", cut.Markup);
    }

    [Fact]
    public async Task Preview_shows_no_duplicates_message()
    {
        var cut = RenderComponent<CleanupDuplicates>();

        var previewButton = cut.Find("button.btn.btn-primary");
        await cut.InvokeAsync(() => previewButton.Click());

        cut.WaitForAssertion(() => Assert.Contains("Aucun doublon trouvé", cut.Markup), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Preview_shows_duplicates_and_execute_button()
    {
        _dbContext.Personnages.Add(new Personnage { Id = 1, Nom = "Test" });
        _dbContext.Personnages.Add(new Personnage { Id = 2, Nom = "test" });
        await _dbContext.SaveChangesAsync();

        var cut = RenderComponent<CleanupDuplicates>();

        var previewButton = cut.Find("button.btn.btn-primary");
        await cut.InvokeAsync(() => previewButton.Click());

        cut.WaitForAssertion(() => Assert.Contains("groupe(s) de doublons trouvé(s)", cut.Markup), TimeSpan.FromSeconds(5));
        cut.WaitForAssertion(() => Assert.Contains("Exécuter le nettoyage", cut.Markup));
    }

    [Fact]
    public async Task Execute_cleanup_requires_confirm_and_removes_duplicates()
    {
        _dbContext.Personnages.Add(new Personnage { Id = 1, Nom = "Dupli" });
        _dbContext.Personnages.Add(new Personnage { Id = 2, Nom = "DUPLI" });
        await _dbContext.SaveChangesAsync();

        var confirmSetup = JSInterop.Setup<bool>("confirm", _ => true);
        confirmSetup.SetResult(true);

        var cut = RenderComponent<CleanupDuplicates>();

        var previewButton = cut.Find("button.btn.btn-primary");
        await cut.InvokeAsync(() => previewButton.Click());

        cut.WaitForAssertion(() => Assert.Contains("Exécuter le nettoyage", cut.Markup), TimeSpan.FromSeconds(5));

        var executeButton = cut.Find("button.btn.btn-danger");
        await cut.InvokeAsync(() => executeButton.Click());

        cut.WaitForAssertion(() => Assert.Contains("Nettoyage terminé", cut.Markup), TimeSpan.FromSeconds(5));
        Assert.Single(_dbContext.Personnages);
    }

    [Fact]
    public async Task Reset_allows_new_preview()
    {
        _dbContext.Personnages.Add(new Personnage { Id = 10, Nom = "NewDup" });
        _dbContext.Personnages.Add(new Personnage { Id = 11, Nom = "newdup" });
        await _dbContext.SaveChangesAsync();

        var confirm = JSInterop.Setup<bool>("confirm", _ => true);
        confirm.SetResult(true);

        var cut = RenderComponent<CleanupDuplicates>();

        var previewButton = cut.Find("button.btn.btn-primary");
        await cut.InvokeAsync(() => previewButton.Click());

        cut.WaitForAssertion(() => Assert.Contains("Exécuter le nettoyage", cut.Markup), TimeSpan.FromSeconds(5));

        var executeButton = cut.Find("button.btn.btn-danger");
        await cut.InvokeAsync(() => executeButton.Click());

        cut.WaitForAssertion(() => Assert.Contains("Nettoyage terminé", cut.Markup), TimeSpan.FromSeconds(5));

        var resetButton = cut.Find("button.btn.btn-primary");
        await cut.InvokeAsync(() => resetButton.Click());

        cut.WaitForAssertion(() => Assert.Contains("Prévisualiser", cut.Markup));
        Assert.DoesNotContain("Nettoyage terminé", cut.Markup);
    }
}
