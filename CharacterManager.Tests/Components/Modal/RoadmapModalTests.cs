using Bunit;
using CharacterManager.Components.Modal;
using CharacterManager.Server.Services;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CharacterManager.Tests.Components.Modal;

public class RoadmapModalTests : BlazorComponentTestBase
{
    private readonly Mock<IModalService> _modalServiceMock;

    public RoadmapModalTests()
    {
        _modalServiceMock = new Mock<IModalService>();
        
        Services.AddSingleton(_modalServiceMock.Object);
        Services.AddSingleton<LanguageContextService>();
    }

    #region Rendering Tests

    [Fact]
    public void RoadmapModal_ShouldRenderMarkdownModal()
    {
        // Act
        var cut = RenderComponent<RoadmapModal>();

        // Assert
        Assert.True(cut.HasComponent<MarkdownModal>());
    }

    [Fact]
    public void RoadmapModal_ShouldPassCorrectFileName()
    {
        // Act
        var cut = RenderComponent<RoadmapModal>();

        // Assert
        var markdownModal = cut.FindComponent<MarkdownModal>();
        Assert.Equal("ROADMAP.md", markdownModal.Instance.FileName);
    }

    [Fact]
    public void RoadmapModal_ShouldPassCorrectIconName()
    {
        // Act
        var cut = RenderComponent<RoadmapModal>();

        // Assert
        var markdownModal = cut.FindComponent<MarkdownModal>();
        Assert.Equal("flag", markdownModal.Instance.IconName);
    }

    [Fact]
    public void RoadmapModal_ShouldPassCorrectTitle()
    {
        // Act
        var cut = RenderComponent<RoadmapModal>();

        // Assert
        var markdownModal = cut.FindComponent<MarkdownModal>();
        Assert.Equal("Feuille de route", markdownModal.Instance.Title);
    }

    [Fact]
    public void RoadmapModal_ShouldPassCorrectNotFoundMessage()
    {
        // Act
        var cut = RenderComponent<RoadmapModal>();

        // Assert
        var markdownModal = cut.FindComponent<MarkdownModal>();
        Assert.Equal("Aucune roadmap trouvée.", markdownModal.Instance.NotFoundMessage);
    }

    [Fact]
    public void RoadmapModal_ShouldPassCorrectErrorMessage()
    {
        // Act
        var cut = RenderComponent<RoadmapModal>();

        // Assert
        var markdownModal = cut.FindComponent<MarkdownModal>();
        Assert.Equal("Erreur lors du chargement de la roadmap.", markdownModal.Instance.ErrorMessage);
    }

    [Fact]
    public void RoadmapModal_AllParametersAreCorrect()
    {
        // Act
        var cut = RenderComponent<RoadmapModal>();
        var markdownModal = cut.FindComponent<MarkdownModal>();

        // Assert all parameters at once
        Assert.Equal("ROADMAP.md", markdownModal.Instance.FileName);
        Assert.Equal("flag", markdownModal.Instance.IconName);
        Assert.Equal("Feuille de route", markdownModal.Instance.Title);
        Assert.Equal("Aucune roadmap trouvée.", markdownModal.Instance.NotFoundMessage);
        Assert.Equal("Erreur lors du chargement de la roadmap.", markdownModal.Instance.ErrorMessage);
    }

    [Fact]
    public void RoadmapModal_ShouldRender()
    {
        // Act
        var cut = RenderComponent<RoadmapModal>();

        // Assert
        Assert.NotNull(cut);
        Assert.NotNull(cut.Markup);
    }

    [Fact]
    public void RoadmapModal_ShouldBeDisposable()
    {
        // Act
        var cut = RenderComponent<RoadmapModal>();

        // Assert - Component should dispose properly
        cut.Dispose();
        Assert.True(true); // If no exception, disposal succeeded
    }

    [Fact]
    public void RoadmapModal_ShouldNotThrow_OnInitialization()
    {
        // Act & Assert - Should not throw exception during render
        var cut = RenderComponent<RoadmapModal>();
        Assert.True(cut.HasComponent<MarkdownModal>());
    }

    [Fact]
    public void RoadmapModal_Close_CallsModalService()
    {
        // Arrange
        var cut = RenderComponent<RoadmapModal>();

        // Act - Call the Close method using reflection
        var closeMethod = cut.Instance?.GetType().GetMethod("Close", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        closeMethod?.Invoke(cut.Instance, null);

        // Assert - Modal service's Close should have been called
        _modalServiceMock.Verify(m => m.Close(), Times.Once);
    }

    #endregion
}
