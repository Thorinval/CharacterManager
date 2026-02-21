using Bunit;
using CharacterManager.Components.Modal;
using CharacterManager.Server.Services;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CharacterManager.Tests.Components.Modal;

public class ChangelogModalTests : BlazorComponentTestBase
{
    private readonly Mock<IModalService> _modalServiceMock;

    public ChangelogModalTests()
    {
        _modalServiceMock = new Mock<IModalService>();
        
        Services.AddSingleton(_modalServiceMock.Object);
        Services.AddSingleton<LanguageContextService>();
    }

    #region Rendering Tests

    [Fact]
    public void ChangelogModal_ShouldRender()
    {
        // Act
        var cut = RenderComponent<ChangelogModal>();

        // Assert - Component renders without errors
        Assert.NotNull(cut);
        Assert.NotNull(cut.Markup);
    }

    [Fact]
    public void ChangelogModal_ShouldRenderMarkdownModal()
    {
        // Act
        var cut = RenderComponent<ChangelogModal>();

        // Assert - Should render MarkdownModal component
        Assert.True(cut.HasComponent<MarkdownModal>());
    }

    [Fact]
    public void ChangelogModal_ShouldPassCorrectFileName()
    {
        // Act
        var cut = RenderComponent<ChangelogModal>();

        // Assert
        var markdownModal = cut.FindComponent<MarkdownModal>();
        Assert.Equal("RELEASE_NOTES.md", markdownModal.Instance.FileName);
    }

    [Fact]
    public void ChangelogModal_ShouldPassCorrectIconName()
    {
        // Act
        var cut = RenderComponent<ChangelogModal>();

        // Assert
        var markdownModal = cut.FindComponent<MarkdownModal>();
        Assert.Equal("update", markdownModal.Instance.IconName);
    }

    [Fact]
    public void ChangelogModal_ShouldPassCorrectTitle()
    {
        // Act
        var cut = RenderComponent<ChangelogModal>();

        // Assert
        var markdownModal = cut.FindComponent<MarkdownModal>();
        Assert.Equal("Notes de version", markdownModal.Instance.Title);
    }

    [Fact]
    public void ChangelogModal_ShouldPassCorrectNotFoundMessage()
    {
        // Act
        var cut = RenderComponent<ChangelogModal>();

        // Assert
        var markdownModal = cut.FindComponent<MarkdownModal>();
        Assert.Equal("Aucun changelog trouvé.", markdownModal.Instance.NotFoundMessage);
    }

    [Fact]
    public void ChangelogModal_ShouldPassCorrectErrorMessage()
    {
        // Act
        var cut = RenderComponent<ChangelogModal>();

        // Assert
        var markdownModal = cut.FindComponent<MarkdownModal>();
        Assert.Equal("Erreur lors du chargement du changelog.", markdownModal.Instance.ErrorMessage);
    }

    [Fact]
    public void ChangelogModal_MarkdownModalHasCorrectParameters()
    {
        // Act
        var cut = RenderComponent<ChangelogModal>();
        var markdownModal = cut.FindComponent<MarkdownModal>();

        // Assert all parameters
        Assert.Equal("RELEASE_NOTES.md", markdownModal.Instance.FileName);
        Assert.Equal("update", markdownModal.Instance.IconName);
        Assert.Equal("Notes de version", markdownModal.Instance.Title);
        Assert.Equal("Aucun changelog trouvé.", markdownModal.Instance.NotFoundMessage);
        Assert.Equal("Erreur lors du chargement du changelog.", markdownModal.Instance.ErrorMessage);
    }

    [Fact]
    public void ChangelogModal_ShouldBeDisposable()
    {
        // Act
        var cut = RenderComponent<ChangelogModal>();

        // Assert - Component should dispose properly
        cut.Dispose();
        Assert.True(true); // If no exception, disposal succeeded
    }

    [Fact]
    public void ChangelogModal_ShouldRenderWithModalService()
    {
        // Act
        var cut = RenderComponent<ChangelogModal>();

        // Assert - Modal service is available
        Assert.NotNull(cut);
    }

    [Fact]
    public void ChangelogModal_ShouldNotThrow_OnRender()
    {
        // Act & Assert - Should not throw exception
        var cut = RenderComponent<ChangelogModal>();
        Assert.NotNull(cut.Markup);
    }

    [Fact]
    public void ChangelogModal_Close_CallsModalService()
    {
        // Arrange
        var cut = RenderComponent<ChangelogModal>();

        // Act - Call the Close method using reflection
        var closeMethod = cut.Instance?.GetType().GetMethod("Close", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        closeMethod?.Invoke(cut.Instance, null);

        // Assert - Modal service's Close should have been called
        _modalServiceMock.Verify(m => m.Close(), Times.Once);
    }

    #endregion
}
