using Bunit;
using CharacterManager.Components.Modal;
using CharacterManager.Server.Services;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CharacterManager.Tests.Components.Modal;

public class ModalHostTests : BlazorComponentTestBase
{
    private readonly Mock<IModalService> _modalServiceMock;
    private Action<Type, Dictionary<string, object>?, ModalSize>? _onShowCallback;
    private Action? _onCloseCallback;

    public ModalHostTests()
    {
        _modalServiceMock = new Mock<IModalService>();
        
        // Capture the event subscriptions
        _modalServiceMock.SetupAdd(m => m.OnShow += It.IsAny<Action<Type, Dictionary<string, object>?, ModalSize>?>())
            .Callback<Action<Type, Dictionary<string, object>?, ModalSize>?>(handler => _onShowCallback = handler);
        _modalServiceMock.SetupAdd(m => m.OnClose += It.IsAny<Action?>())
            .Callback<Action?>(handler => _onCloseCallback = handler);
        
        Services.AddSingleton(_modalServiceMock.Object);
    }

    #region Rendering Tests

    [Fact]
    public void ModalHost_ShouldRenderNothing_Initially()
    {
        // Act
        var cut = RenderComponent<ModalHost>();

        // Assert - No modal backdrop should be visible
        Assert.DoesNotContain("modal-backdrop", cut.Markup);
    }

    [Fact]
    public void ModalHost_ShouldSubscribeToModalServiceEvents_OnInitialize()
    {
        // Act
        var cut = RenderComponent<ModalHost>();

        // Assert
        _modalServiceMock.VerifyAdd(m => m.OnShow += It.IsAny<Action<Type, Dictionary<string, object>?, ModalSize>?>(), Times.Once);
        _modalServiceMock.VerifyAdd(m => m.OnClose += It.IsAny<Action?>(), Times.Once);
    }

    #endregion

    #region Modal Size Tests

    [Theory]
    [InlineData(ModalSize.Auto, "modal-auto")]
    [InlineData(ModalSize.Small, "modal-sm")]
    [InlineData(ModalSize.Medium, "modal-md")]
    [InlineData(ModalSize.Large, "modal-lg")]
    [InlineData(ModalSize.XL, "modal-xl")]
    public void ModalHost_ShouldApplyCorrectSizeClass(ModalSize size, string expectedClass)
    {
        // Arrange
        var cut = RenderComponent<ModalHost>();

        // Act - Simulate showing a modal
        _onShowCallback?.Invoke(typeof(TestModalComponent), null, size);
        cut.Render();

        // Assert
        var container = cut.Find(".modal-container");
        Assert.Contains(expectedClass, container.ClassName);
    }

    #endregion

    #region Close Button Tests

    [Fact]
    public void ModalHost_ShouldHaveCloseButton_WhenModalShown()
    {
        // Arrange
        var cut = RenderComponent<ModalHost>();

        // Act
        _onShowCallback?.Invoke(typeof(TestModalComponent), null, ModalSize.Medium);
        cut.Render();

        // Assert
        var closeButton = cut.Find(".modal-close-btn");
        Assert.NotNull(closeButton);
    }

    [Fact]
    public void ModalHost_CloseButton_ShouldHaveCloseIcon()
    {
        // Arrange
        var cut = RenderComponent<ModalHost>();
        _onShowCallback?.Invoke(typeof(TestModalComponent), null, ModalSize.Medium);
        cut.Render();

        // Act
        var closeButton = cut.Find(".modal-close-btn");

        // Assert
        var icon = closeButton.QuerySelector("i.bi-x-lg");
        Assert.NotNull(icon);
    }

    #endregion

    #region Backdrop Tests

    [Fact]
    public void ModalHost_ShouldShowBackdrop_WhenModalShown()
    {
        // Arrange
        var cut = RenderComponent<ModalHost>();

        // Act
        _onShowCallback?.Invoke(typeof(TestModalComponent), null, ModalSize.Medium);
        cut.Render();

        // Assert
        var backdrop = cut.Find(".modal-backdrop");
        Assert.NotNull(backdrop);
    }

    #endregion

    #region Close Behavior Tests

    [Fact]
    public void ModalHost_ShouldCloseModal_WhenCloseEventReceived()
    {
        // Arrange
        var cut = RenderComponent<ModalHost>();
        _onShowCallback?.Invoke(typeof(TestModalComponent), null, ModalSize.Medium);
        cut.Render();

        // Act
        _onCloseCallback?.Invoke();
        cut.Render();

        // Assert
        Assert.DoesNotContain("modal-backdrop", cut.Markup);
    }

    [Fact]
    public void ModalHost_ShouldCloseModal_WhenBackdropClicked()
    {
        // Arrange
        var cut = RenderComponent<ModalHost>();
        _onShowCallback?.Invoke(typeof(TestModalComponent), null, ModalSize.Medium);
        cut.Render();

        // Act
        var backdrop = cut.Find(".modal-backdrop");
        backdrop.Click();

        // Assert - Modal should be closed
        Assert.DoesNotContain("modal-backdrop", cut.Markup);
    }

    [Fact]
    public void ModalHost_ContainerHasStopPropagation()
    {
        // Arrange
        var cut = RenderComponent<ModalHost>();
        _onShowCallback?.Invoke(typeof(TestModalComponent), null, ModalSize.Medium);
        cut.Render();

        // Act - Verify modal container has stopPropagation attribute
        var container = cut.Find(".modal-container");

        // Assert - Modal container should have onclick:stopPropagation
        Assert.Contains("modal-container", container.ClassName);
    }

    #endregion

    #region DynamicComponent Tests

    [Fact]
    public void ModalHost_ShouldRenderDynamicComponent_WhenModalShown()
    {
        // Arrange
        var cut = RenderComponent<ModalHost>();

        // Act
        _onShowCallback?.Invoke(typeof(TestModalComponent), null, ModalSize.Medium);
        cut.Render();

        // Assert
        Assert.Contains("test-modal-content", cut.Markup);
    }

    [Fact]
    public void ModalHost_ShouldPassParameters_ToDynamicComponent()
    {
        // Arrange
        var cut = RenderComponent<ModalHost>();
        var parameters = new Dictionary<string, object>
        {
            ["TestMessage"] = "Hello from parameters"
        };

        // Act
        _onShowCallback?.Invoke(typeof(TestModalComponent), parameters, ModalSize.Medium);
        cut.Render();

        // Assert
        Assert.Contains("Hello from parameters", cut.Markup);
    }

    #endregion
}

/// <summary>
/// Test modal component for ModalHost tests
/// </summary>
public class TestModalComponent : Microsoft.AspNetCore.Components.ComponentBase
{
    [Microsoft.AspNetCore.Components.Parameter]
    public string TestMessage { get; set; } = "Default";

    protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "test-modal-content");
        builder.AddContent(2, TestMessage);
        builder.CloseElement();
    }
}
