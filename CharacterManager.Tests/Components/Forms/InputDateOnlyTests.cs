using CharacterManager.Components.Forms;
using System.Reflection;
using Xunit;

namespace CharacterManager.Tests.Components.Forms;

public class InputDateOnlyTests
{
    [Fact]
    public void TryParseValueFromString_ValidDate_ReturnsTrue()
    {
        // Arrange
        var inputDateOnly = new InputDateOnly();
        var method = typeof(InputDateOnly).GetMethod("TryParseValueFromString",
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        var parameters = new object?[] { "2024-12-25", null, null };

        // Act
        var result = (bool)method!.Invoke(inputDateOnly, parameters)!;
        var parsedDate = (DateOnly)parameters[1]!;
        var errorMessage = (string?)parameters[2];

        // Assert
        Assert.True(result);
        Assert.Equal(new DateOnly(2024, 12, 25), parsedDate);
        Assert.Null(errorMessage);
    }

    [Fact]
    public void TryParseValueFromString_InvalidDate_ReturnsFalse()
    {
        // Arrange
        var inputDateOnly = new InputDateOnly();
        var method = typeof(InputDateOnly).GetMethod("TryParseValueFromString",
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        var parameters = new object?[] { "not-a-date", null, null };

        // Act
        var result = (bool)method!.Invoke(inputDateOnly, parameters)!;
        var errorMessage = (string?)parameters[2];

        // Assert
        Assert.False(result);
        Assert.Equal("Date invalide", errorMessage);
    }

    [Fact]
    public void TryParseValueFromString_EmptyString_ReturnsFalse()
    {
        // Arrange
        var inputDateOnly = new InputDateOnly();
        var method = typeof(InputDateOnly).GetMethod("TryParseValueFromString",
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        var parameters = new object?[] { "", null, null };

        // Act
        var result = (bool)method!.Invoke(inputDateOnly, parameters)!;
        var errorMessage = (string?)parameters[2];

        // Assert
        Assert.False(result);
        Assert.Equal("Date invalide", errorMessage);
    }

    [Fact]
    public void TryParseValueFromString_ValidDateDifferentFormat_ReturnsTrue()
    {
        // Arrange
        var inputDateOnly = new InputDateOnly();
        var method = typeof(InputDateOnly).GetMethod("TryParseValueFromString",
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        var parameters = new object?[] { "2025-01-25", null, null };

        // Act
        var result = (bool)method!.Invoke(inputDateOnly, parameters)!;
        var parsedDate = (DateOnly)parameters[1]!;

        // Assert
        Assert.True(result);
        Assert.Equal(new DateOnly(2025, 1, 25), parsedDate);
    }

    [Fact]
    public void TryParseValueFromString_LeapYearDate_ReturnsTrue()
    {
        // Arrange
        var inputDateOnly = new InputDateOnly();
        var method = typeof(InputDateOnly).GetMethod("TryParseValueFromString",
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        var parameters = new object?[] { "2024-02-29", null, null };

        // Act
        var result = (bool)method!.Invoke(inputDateOnly, parameters)!;
        var parsedDate = (DateOnly)parameters[1]!;

        // Assert
        Assert.True(result);
        Assert.Equal(new DateOnly(2024, 2, 29), parsedDate);
    }

    [Fact]
    public void TryParseValueFromString_InvalidLeapYearDate_ReturnsFalse()
    {
        // Arrange
        var inputDateOnly = new InputDateOnly();
        var method = typeof(InputDateOnly).GetMethod("TryParseValueFromString",
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        var parameters = new object?[] { "2023-02-29", null, null };

        // Act
        var result = (bool)method!.Invoke(inputDateOnly, parameters)!;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void InputDateOnly_InheritsFromInputBase()
    {
        // Assert
        Assert.True(typeof(InputDateOnly).BaseType?.Name.StartsWith("InputBase"));
    }

    [Fact]
    public void TryParseValueFromString_NullValue_ReturnsFalse()
    {
        // Arrange
        var inputDateOnly = new InputDateOnly();
        var method = typeof(InputDateOnly).GetMethod("TryParseValueFromString",
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        var parameters = new object?[] { null, null, null };

        // Act
        var result = (bool)method!.Invoke(inputDateOnly, parameters)!;

        // Assert
        Assert.False(result);
    }
}
