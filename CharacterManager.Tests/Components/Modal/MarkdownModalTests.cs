using Bunit;
using CharacterManager.Components.Modal;
using CharacterManager.Server.Services;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CharacterManager.Tests.Components.Modal;

public class MarkdownModalTests : BlazorComponentTestBase, IDisposable
{
    private readonly string _testDir;
    private bool _disposed;

    public MarkdownModalTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"MarkdownModalTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDir);
        
        Services.AddSingleton<LanguageContextService>();
    }

    #region Parameter Tests

    [Fact]
    public void MarkdownModal_ShouldAcceptFileNameParameter()
    {
        // Act
        var cut = RenderComponent<MarkdownModal>(parameters => parameters
            .Add(p => p.FileName, "test.md"));

        // Assert
        Assert.Equal("test.md", cut.Instance.FileName);
    }

    [Fact]
    public void MarkdownModal_ShouldAcceptIconNameParameter()
    {
        // Act
        var cut = RenderComponent<MarkdownModal>(parameters => parameters
            .Add(p => p.IconName, "star"));

        // Assert
        Assert.Equal("star", cut.Instance.IconName);
    }

    [Fact]
    public void MarkdownModal_ShouldAcceptTitleParameter()
    {
        // Act
        var cut = RenderComponent<MarkdownModal>(parameters => parameters
            .Add(p => p.Title, "Test Title"));

        // Assert
        Assert.Equal("Test Title", cut.Instance.Title);
    }

    [Fact]
    public void MarkdownModal_ShouldAcceptNotFoundMessageParameter()
    {
        // Act
        var cut = RenderComponent<MarkdownModal>(parameters => parameters
            .Add(p => p.NotFoundMessage, "Custom not found"));

        // Assert
        Assert.Equal("Custom not found", cut.Instance.NotFoundMessage);
    }

    [Fact]
    public void MarkdownModal_ShouldAcceptErrorMessageParameter()
    {
        // Act
        var cut = RenderComponent<MarkdownModal>(parameters => parameters
            .Add(p => p.ErrorMessage, "Custom error"));

        // Assert
        Assert.Equal("Custom error", cut.Instance.ErrorMessage);
    }

    #endregion

    #region Default Values Tests

    [Fact]
    public void MarkdownModal_ShouldHaveDefaultNotFoundMessage()
    {
        // Act
        var cut = RenderComponent<MarkdownModal>();

        // Assert
        Assert.Equal("Aucun fichier trouvé.", cut.Instance.NotFoundMessage);
    }

    [Fact]
    public void MarkdownModal_ShouldHaveDefaultErrorMessage()
    {
        // Act
        var cut = RenderComponent<MarkdownModal>();

        // Assert
        Assert.Equal("Erreur lors du chargement du fichier.", cut.Instance.ErrorMessage);
    }

    [Fact]
    public void MarkdownModal_ShouldHaveEmptyFileNameByDefault()
    {
        // Act
        var cut = RenderComponent<MarkdownModal>();

        // Assert
        Assert.Equal(string.Empty, cut.Instance.FileName);
    }

    [Fact]
    public void MarkdownModal_ShouldHaveEmptyIconNameByDefault()
    {
        // Act
        var cut = RenderComponent<MarkdownModal>();

        // Assert
        Assert.Equal(string.Empty, cut.Instance.IconName);
    }

    [Fact]
    public void MarkdownModal_ShouldHaveEmptyTitleByDefault()
    {
        // Act
        var cut = RenderComponent<MarkdownModal>();

        // Assert
        Assert.Equal(string.Empty, cut.Instance.Title);
    }

    #endregion

    #region Rendering Tests

    [Fact]
    public void MarkdownModal_ShouldRenderModalBody()
    {
        // Act
        var cut = RenderComponent<MarkdownModal>();

        // Assert
        var modalBody = cut.Find(".modal-body");
        Assert.NotNull(modalBody);
    }

    [Fact]
    public void MarkdownModal_ShouldRenderChangelogContent()
    {
        // Act
        var cut = RenderComponent<MarkdownModal>();

        // Assert
        var content = cut.Find(".changelog-content");
        Assert.NotNull(content);
    }

    [Fact]
    public void MarkdownModal_ShouldRenderTitle_WhenProvided()
    {
        // Act
        var cut = RenderComponent<MarkdownModal>(parameters => parameters
            .Add(p => p.Title, "Test Title"));

        // Assert
        Assert.Contains("Test Title", cut.Markup);
    }

    [Fact]
    public void MarkdownModal_ShouldRenderIcon_WhenProvided()
    {
        // Act
        var cut = RenderComponent<MarkdownModal>(parameters => parameters
            .Add(p => p.IconName, "star")
            .Add(p => p.Title, "Title"));

        // Assert
        var icon = cut.Find("span.msr");
        Assert.Contains("star", icon.TextContent);
    }

    [Fact]
    public void MarkdownModal_ShouldRenderDefaultIcon_WhenNoIconAndNoTitle()
    {
        // Act
        var cut = RenderComponent<MarkdownModal>();

        // Assert
        var icon = cut.Find("i.bi-info-circle");
        Assert.NotNull(icon);
    }

    #endregion

    public new void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected new virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                
                if (Directory.Exists(_testDir))
                {
                    try
                    {
                        Directory.Delete(_testDir, recursive: true);
                    }
                    catch
                    {
                        // Ignore cleanup errors
                    }
                }
            }
            _disposed = true;
        }
    }
}
