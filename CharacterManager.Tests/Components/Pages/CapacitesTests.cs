using Bunit;
using Bunit.TestDoubles;
using CharacterManager.Components.Pages;
using CharacterManager.Server.Models;
using CharacterManager.Server.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CharacterManager.Tests.Components.Pages;

public class CapacitesTests : TestContext
{
    private readonly Mock<ICapaciteService> _capaciteService = new();
    private readonly Mock<IPmlExportService> _exportService = new();
    private readonly Mock<IPmlImportService> _importService = new();
    private readonly Mock<IClientLocalizationService> _localization = new();

    public CapacitesTests()
    {
        var auth = this.AddTestAuthorization();
        auth.SetAuthorized("tester");

        _localization.Setup(l => l.GetKeyValue(It.IsAny<string>())).Returns<string>(k => k);
        _localization.SetupGet(l => l.CurrentLanguage).Returns("fr");

        Services.AddSingleton(_capaciteService.Object);
        Services.AddSingleton(_exportService.Object);
        Services.AddSingleton(_importService.Object);
        Services.AddSingleton(_localization.Object);

        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.SetupVoid("alert", _ => true);
    }

    [Fact]
    public void Empty_list_shows_empty_state()
    {
        _capaciteService.Setup(s => s.GetAllAsync()).ReturnsAsync(new List<Capacite>());

        var cut = RenderComponent<CascadingAuthenticationState>(p => p.AddChildContent<Capacites>());

        cut.WaitForAssertion(() => Assert.Contains("capacities.empty", cut.Markup));
        var exportButton = cut.Find("button.btn-success");
        Assert.Contains("disabled", exportButton.OuterHtml);
    }

    [Fact]
    public void Edit_export_and_delete_flow()
    {
        var initial = new List<Capacite>
        {
            new() { Id = 1, Nom = "Choc", Description = "Desc", Icon = "bolt" }
        };

        _capaciteService.SetupSequence(s => s.GetAllAsync())
            .ReturnsAsync(initial)
            .ReturnsAsync(initial)
            .ReturnsAsync(new List<Capacite>());

        _capaciteService.Setup(s => s.UpdateAsync(It.IsAny<int>(), It.IsAny<Capacite>()))
            .ReturnsAsync((int id, Capacite c) => c);

        _capaciteService.Setup(s => s.DeleteAsync(It.IsAny<int>()))
            .Returns(Task.CompletedTask);

        _exportService.Setup(s => s.ExporterCapacitesPmlAsync(It.IsAny<IEnumerable<Capacite>>()))
            .ReturnsAsync(new byte[] { 1, 2, 3 });

        JSInterop.SetupVoid("downloadFile", _ => true);
        JSInterop.Setup<bool>("confirm", _ => true).SetResult(true);

        var cut = RenderComponent<CascadingAuthenticationState>(p => p.AddChildContent<Capacites>());

        cut.WaitForAssertion(() => Assert.Contains("Choc", cut.Markup));

        cut.Find("button.btn-warning").Click();

        var nameInput = cut.Find("#nomInput");
        nameInput.Change("Choc+1");

        cut.Find("div.modal-content button.btn-primary").Click();

        _capaciteService.Verify(s => s.UpdateAsync(1, It.Is<Capacite>(c => c.Nom == "Choc+1")), Times.Once);

        var exportButton = cut.Find("button.btn-success");
        Assert.DoesNotContain("disabled", exportButton.OuterHtml);
        exportButton.Click();

        _exportService.Verify(s => s.ExporterCapacitesPmlAsync(It.Is<IEnumerable<Capacite>>(l => l.Any())), Times.Once);
        Assert.Contains(JSInterop.Invocations, i => i.Identifier == "downloadFile");

        cut.Find("button.btn-danger").Click();

        _capaciteService.Verify(s => s.DeleteAsync(1), Times.Once);
    }
}