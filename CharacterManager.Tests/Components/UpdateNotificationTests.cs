using System;
using Bunit;
using CharacterManager.Components;
using CharacterManager.Server.Services;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CharacterManager.Tests.Components;

public class UpdateNotificationTests : TestContext
{
    private readonly Mock<IUpdateService> _updateServiceMock = new();

    public UpdateNotificationTests()
    {
        Services.AddSingleton(_updateServiceMock.Object);
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void Update_available_renders_banner()
    {
        var info = new UpdateInfo
        {
            CurrentVersion = "1.0.0",
            LatestVersion = "2.0.0",
            IsUpdateAvailable = true,
            DownloadUrl = "https://example.com/update.zip",
            ReleaseNotes = "Bug fixes",
            PublishedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        _updateServiceMock.Setup(s => s.CheckForUpdatesAsync()).ReturnsAsync(info);

        var cut = RenderComponent<UpdateNotification>();

        cut.WaitForAssertion(() => _updateServiceMock.Verify(s => s.CheckForUpdatesAsync(), Times.Once));
        cut.WaitForAssertion(() => Assert.Contains("Mise à jour disponible", cut.Markup));
        cut.WaitForAssertion(() => Assert.Contains("2.0.0", cut.Markup));
        cut.WaitForAssertion(() => Assert.Contains("1.0.0", cut.Markup));
    }

    [Fact]
    public void Download_button_opens_url()
    {
        var info = new UpdateInfo
        {
            CurrentVersion = "1.0.0",
            LatestVersion = "2.0.0",
            IsUpdateAvailable = true,
            DownloadUrl = "https://example.com/update.zip",
            PublishedAt = DateTime.UtcNow
        };

        _updateServiceMock.Setup(s => s.CheckForUpdatesAsync()).ReturnsAsync(info);
        JSInterop.SetupVoid("open");

        var cut = RenderComponent<UpdateNotification>();

        var downloadButton = cut.Find("button.btn.btn-sm.btn-success");
        downloadButton.Click();

        cut.WaitForAssertion(() => Assert.Equal(info.DownloadUrl, JSInterop.VerifyInvoke("open").Arguments[0]?.ToString()));
    }

    [Fact]
    public void Dismiss_hides_notification()
    {
        var info = new UpdateInfo
        {
            CurrentVersion = "1.0.0",
            LatestVersion = "2.0.0",
            IsUpdateAvailable = true,
            DownloadUrl = "https://example.com/update.zip",
            PublishedAt = DateTime.UtcNow
        };

        _updateServiceMock.Setup(s => s.CheckForUpdatesAsync()).ReturnsAsync(info);

        var cut = RenderComponent<UpdateNotification>();

        cut.Find("button.btn.btn-sm.btn-link").Click();

        cut.WaitForAssertion(() => Assert.DoesNotContain("Mise à jour disponible", cut.Markup, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ShowDetails_displays_modal_with_notes()
    {
        var info = new UpdateInfo
        {
            CurrentVersion = "1.0.0",
            LatestVersion = "2.0.0",
            IsUpdateAvailable = true,
            DownloadUrl = "https://example.com/update.zip",
            ReleaseNotes = "Line one\nLine two",
            PublishedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        _updateServiceMock.Setup(s => s.CheckForUpdatesAsync()).ReturnsAsync(info);

        var cut = RenderComponent<UpdateNotification>();

        cut.Find("button.btn.btn-sm.btn-light").Click();

        cut.WaitForAssertion(() => Assert.Contains("Line one", cut.Markup));
        cut.WaitForAssertion(() => Assert.Contains("Notes de version", cut.Markup));
    }

    [Fact]
    public void No_update_renders_nothing()
    {
        var info = new UpdateInfo
        {
            CurrentVersion = "1.0.0",
            LatestVersion = "1.0.0",
            IsUpdateAvailable = false,
            PublishedAt = DateTime.UtcNow
        };

        _updateServiceMock.Setup(s => s.CheckForUpdatesAsync()).ReturnsAsync(info);

        var cut = RenderComponent<UpdateNotification>();

        cut.WaitForAssertion(() => Assert.DoesNotContain("Mise à jour disponible", cut.Markup, StringComparison.OrdinalIgnoreCase));
    }
}
