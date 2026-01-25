using CharacterManager.Server.Services;
using Xunit;

namespace CharacterManager.Tests;

public class AdultModeNotificationServiceTests
{
    #region SetAdultMode Tests

    [Fact]
    public void SetAdultMode_ShouldUpdateState_WhenValueChanges()
    {
        // Arrange
        var service = new AdultModeNotificationService();
        Assert.True(service.IsAdultModeEnabled); // default is true

        // Act
        service.SetAdultMode(false);

        // Assert
        Assert.False(service.IsAdultModeEnabled);
    }

    [Fact]
    public void SetAdultMode_ShouldNotNotify_WhenValueSame()
    {
        // Arrange
        var service = new AdultModeNotificationService();
        var callCount = 0;
        service.Subscribe(_ => callCount++);

        // Act - set to same value (true -> true)
        service.SetAdultMode(true);

        // Assert
        Assert.Equal(0, callCount);
    }

    [Fact]
    public void SetAdultMode_ShouldNotifySubscribers_WhenValueChanges()
    {
        // Arrange
        var service = new AdultModeNotificationService();
        bool? receivedValue = null;
        service.Subscribe(value => receivedValue = value);

        // Act
        service.SetAdultMode(false);

        // Assert
        Assert.False(receivedValue);
    }

    #endregion

    #region Subscribe Tests

    [Fact]
    public void Subscribe_ShouldAddCallback()
    {
        // Arrange
        var service = new AdultModeNotificationService();
        var callCount = 0;
        Action<bool> callback = _ => callCount++;

        // Act
        service.Subscribe(callback);
        service.SetAdultMode(false);

        // Assert
        Assert.Equal(1, callCount);
    }

    [Fact]
    public void Subscribe_ShouldNotAddDuplicateCallback()
    {
        // Arrange
        var service = new AdultModeNotificationService();
        var callCount = 0;
        Action<bool> callback = _ => callCount++;

        // Act
        service.Subscribe(callback);
        service.Subscribe(callback); // duplicate
        service.SetAdultMode(false);

        // Assert
        Assert.Equal(1, callCount);
    }

    [Fact]
    public void Subscribe_ShouldSupportMultipleCallbacks()
    {
        // Arrange
        var service = new AdultModeNotificationService();
        var count1 = 0;
        var count2 = 0;

        // Act
        service.Subscribe(_ => count1++);
        service.Subscribe(_ => count2++);
        service.SetAdultMode(false);

        // Assert
        Assert.Equal(1, count1);
        Assert.Equal(1, count2);
    }

    #endregion

    #region Unsubscribe Tests

    [Fact]
    public void Unsubscribe_ShouldRemoveCallback()
    {
        // Arrange
        var service = new AdultModeNotificationService();
        var callCount = 0;
        Action<bool> callback = _ => callCount++;
        service.Subscribe(callback);

        // Act
        service.Unsubscribe(callback);
        service.SetAdultMode(false);

        // Assert
        Assert.Equal(0, callCount);
    }

    [Fact]
    public void Unsubscribe_ShouldNotThrow_WhenCallbackNotFound()
    {
        // Arrange
        var service = new AdultModeNotificationService();
        Action<bool> callback = _ => { };

        // Act & Assert - should not throw
        service.Unsubscribe(callback);
    }

    #endregion

    #region Notification Error Handling Tests

    [Fact]
    public void SetAdultMode_ShouldContinueNotifying_WhenCallbackThrows()
    {
        // Arrange
        var service = new AdultModeNotificationService();
        var secondCallbackCalled = false;

        service.Subscribe(_ => throw new Exception("Test exception"));
        service.Subscribe(_ => secondCallbackCalled = true);

        // Act
        service.SetAdultMode(false);

        // Assert - second callback should still be called
        Assert.True(secondCallbackCalled);
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public async Task Service_ShouldBeThreadSafe_WhenSubscribingAndNotifying()
    {
        // Arrange
        var service = new AdultModeNotificationService();
        var callCount = 0;
        
        // Subscribe 10 callbacks sequentially to ensure they're all registered
        for (int i = 0; i < 10; i++)
        {
            service.Subscribe(_ => Interlocked.Increment(ref callCount));
        }

        // Act - toggle from multiple threads in parallel
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => Task.Run(() => service.SetAdultMode(!service.IsAdultModeEnabled)))
            .ToArray();
        
        await Task.WhenAll(tasks);

        // Assert - should have been notified at least once without throwing
        Assert.True(callCount > 0);
    }

    #endregion
}
