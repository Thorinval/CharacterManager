using Bunit;
using CharacterManager.Components;
using Xunit;

namespace CharacterManager.Tests.Components;

public class ToastComponentTests : BlazorComponentTestBase
{
    #region Rendering Tests

    [Fact]
    public void Toast_ShouldRenderHidden_Initially()
    {
        // Act
        var cut = RenderComponent<Toast>();

        // Assert
        var container = cut.Find(".toast-container");
        Assert.Contains("display:none", container.GetAttribute("style"));
    }

    [Fact]
    public void Toast_ShouldRenderVisible_WhenShowCalled()
    {
        // Arrange
        var cut = RenderComponent<Toast>();

        // Act
        cut.Instance.Show("Test message");
        cut.Render(); // Re-render to update state

        // Assert
        var container = cut.Find(".toast-container");
        Assert.Contains("display:block", container.GetAttribute("style"));
    }

    [Fact]
    public void Toast_ShouldDisplayMessage_WhenShowCalled()
    {
        // Arrange
        var cut = RenderComponent<Toast>();

        // Act
        cut.Instance.Show("Hello World!");
        cut.Render();

        // Assert
        var message = cut.Find(".toast-message span");
        Assert.Equal("Hello World!", message.TextContent);
    }

    #endregion

    #region CSS Class Tests

    [Theory]
    [InlineData("success", "toast-success")]
    [InlineData("warning", "toast-warning")]
    [InlineData("error", "toast-error")]
    [InlineData("info", "toast-info")]
    [InlineData("unknown", "toast-info")] // Default fallback
    public void Toast_ShouldApplyCorrectCssClass_ForType(string type, string expectedClass)
    {
        // Arrange
        var cut = RenderComponent<Toast>();

        // Act
        cut.Instance.Show("Test", type);
        cut.Render();

        // Assert
        var messageDiv = cut.Find(".toast-message");
        Assert.Contains(expectedClass, messageDiv.ClassName);
    }

    #endregion

    #region Hide Tests

    [Fact]
    public void Toast_ShouldHide_WhenHideCalled()
    {
        // Arrange
        var cut = RenderComponent<Toast>();
        cut.Instance.Show("Test");
        cut.Render();

        // Act
        cut.Instance.Hide();
        cut.Render();

        // Assert
        var container = cut.Find(".toast-container");
        Assert.Contains("display:none", container.GetAttribute("style"));
    }

    [Fact]
    public void Toast_ShouldHide_WhenCloseButtonClicked()
    {
        // Arrange
        var cut = RenderComponent<Toast>();
        cut.Instance.Show("Test");
        cut.Render();

        // Act
        var closeButton = cut.Find(".btn-close");
        closeButton.Click();

        // Assert
        var container = cut.Find(".toast-container");
        Assert.Contains("display:none", container.GetAttribute("style"));
    }

    #endregion

    #region Structure Tests

    [Fact]
    public void Toast_ShouldHaveCloseButton()
    {
        // Act
        var cut = RenderComponent<Toast>();

        // Assert
        var closeButton = cut.Find(".btn-close");
        Assert.NotNull(closeButton);
    }

    [Fact]
    public void Toast_ShouldHaveToastContainer()
    {
        // Act
        var cut = RenderComponent<Toast>();

        // Assert
        var container = cut.Find(".toast-container");
        Assert.NotNull(container);
    }

    #endregion
}
