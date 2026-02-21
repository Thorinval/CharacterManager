using CharacterManager.Server.Services;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace CharacterManager.Tests;

public class ModalServiceTests
{
    #region Open Tests

    [Fact]
    public void Open_ShouldTriggerOnShowEvent()
    {
        // Arrange
        var service = new ModalService();
        Type? capturedType = null;
        Dictionary<string, object>? capturedParams = null;
        ModalSize capturedSize = ModalSize.Medium;

        service.OnShow += (type, parameters, size) =>
        {
            capturedType = type;
            capturedParams = parameters;
            capturedSize = size;
        };

        // Act
        service.Open<TestComponent>();

        // Assert
        Assert.Equal(typeof(TestComponent), capturedType);
        Assert.Null(capturedParams);
        Assert.Equal(ModalSize.Medium, capturedSize);
    }

    [Fact]
    public void Open_ShouldPassParameters()
    {
        // Arrange
        var service = new ModalService();
        Dictionary<string, object>? capturedParams = null;

        service.OnShow += (_, parameters, _) => capturedParams = parameters;

        var parameters = new Dictionary<string, object>
        {
            ["Title"] = "Test Title",
            ["Value"] = 42
        };

        // Act
        service.Open<TestComponent>(parameters);

        // Assert
        Assert.NotNull(capturedParams);
        Assert.Equal("Test Title", capturedParams!["Title"]);
        Assert.Equal(42, capturedParams["Value"]);
    }

    [Fact]
    public void Open_ShouldPassSize()
    {
        // Arrange
        var service = new ModalService();
        ModalSize capturedSize = ModalSize.Medium;

        service.OnShow += (_, _, size) => capturedSize = size;

        // Act
        service.Open<TestComponent>(size: ModalSize.Large);

        // Assert
        Assert.Equal(ModalSize.Large, capturedSize);
    }

    [Fact]
    public void Open_ShouldNotThrow_WhenNoSubscribers()
    {
        // Arrange
        var service = new ModalService();

        // Act & Assert - should not throw
        service.Open<TestComponent>();
    }

    #endregion

    #region Close Tests

    [Fact]
    public void Close_ShouldTriggerOnCloseEvent()
    {
        // Arrange
        var service = new ModalService();
        var closeCalled = false;

        service.OnClose += () => closeCalled = true;

        // Act
        service.Close();

        // Assert
        Assert.True(closeCalled);
    }

    [Fact]
    public void Close_ShouldNotThrow_WhenNoSubscribers()
    {
        // Arrange
        var service = new ModalService();

        // Act & Assert - should not throw
        service.Close();
    }

    #endregion

    #region Multiple Subscribers Tests

    [Fact]
    public void Open_ShouldNotifyAllSubscribers()
    {
        // Arrange
        var service = new ModalService();
        var count = 0;

        service.OnShow += (_, _, _) => count++;
        service.OnShow += (_, _, _) => count++;

        // Act
        service.Open<TestComponent>();

        // Assert
        Assert.Equal(2, count);
    }

    [Fact]
    public void Close_ShouldNotifyAllSubscribers()
    {
        // Arrange
        var service = new ModalService();
        var count = 0;

        service.OnClose += () => count++;
        service.OnClose += () => count++;

        // Act
        service.Close();

        // Assert
        Assert.Equal(2, count);
    }

    #endregion

    // Test component for modal service tests
    private class TestComponent : IComponent
    {
        public void Attach(RenderHandle renderHandle) { }
        public Task SetParametersAsync(ParameterView parameters) => Task.CompletedTask;
    }
}
