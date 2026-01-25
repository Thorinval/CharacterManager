using CharacterManager.Server.Models;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace CharacterManager.Tests.Components;

/// <summary>
/// Tests for ClassementFormModel validation logic
/// </summary>
public class ClassementFormModelTests
{
    #region Ligue Validation Tests

    [Theory]
    [InlineData(1)]   // Valid: lowest league
    [InlineData(10)]  // Valid: middle league
    [InlineData(25)]  // Valid: highest regular league
    [InlineData(50)]  // Valid: Elite TOP 50
    public void Validate_ShouldPass_WhenLigueIsValid(int ligue)
    {
        // Arrange
        var model = CreateValidModel();
        model.Ligue = ligue;

        // Act
        var results = ValidateModel(model);

        // Assert - no validation errors for Ligue
        Assert.DoesNotContain(results, r => r.MemberNames.Contains(nameof(ClassementFormModel.Ligue)));
    }

    [Theory]
    [InlineData(0)]   // Invalid: below range
    [InlineData(-1)]  // Invalid: negative
    [InlineData(26)]  // Invalid: above 25 but not 50
    [InlineData(49)]  // Invalid: close to Elite but not 50
    [InlineData(51)]  // Invalid: above Elite
    [InlineData(100)] // Invalid: way above
    public void Validate_ShouldFail_WhenLigueIsInvalid(int ligue)
    {
        // Arrange
        var model = CreateValidModel();
        model.Ligue = ligue;

        // Act
        var results = ValidateModel(model);

        // Assert
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(ClassementFormModel.Ligue)));
    }

    [Fact]
    public void Validate_ShouldNotValidateLigue_WhenNull()
    {
        // Arrange - Ligue is null, which should trigger Required validation, not custom validation
        var model = CreateValidModel();
        model.Ligue = null;

        // Act
        var results = ValidateModel(model);

        // Assert - custom validation doesn't trigger for null (Required handles it)
        var customLigueError = results.FirstOrDefault(r => 
            r.MemberNames.Contains(nameof(ClassementFormModel.Ligue)) &&
            r.ErrorMessage!.Contains("comprise entre"));
        Assert.Null(customLigueError);
    }

    #endregion

    #region Required Field Validation Tests

    [Fact]
    public void Validate_ShouldFail_WhenNutakuIsNull()
    {
        // Arrange
        var model = CreateValidModel();
        model.Nutaku = null;

        // Act
        var results = ValidateModel(model);

        // Assert
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(ClassementFormModel.Nutaku)));
    }

    [Fact]
    public void Validate_ShouldFail_WhenTop150IsNull()
    {
        // Arrange
        var model = CreateValidModel();
        model.Top150 = null;

        // Act
        var results = ValidateModel(model);

        // Assert
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(ClassementFormModel.Top150)));
    }

    [Fact]
    public void Validate_ShouldFail_WhenFranceIsNull()
    {
        // Arrange
        var model = CreateValidModel();
        model.France = null;

        // Act
        var results = ValidateModel(model);

        // Assert
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(ClassementFormModel.France)));
    }

    [Fact]
    public void Validate_ShouldFail_WhenScoreIsNull()
    {
        // Arrange
        var model = CreateValidModel();
        model.Score = null;

        // Act
        var results = ValidateModel(model);

        // Assert
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(ClassementFormModel.Score)));
    }

    #endregion

    #region Range Validation Tests

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Validate_ShouldFail_WhenNutakuIsNotPositive(int value)
    {
        // Arrange
        var model = CreateValidModel();
        model.Nutaku = value;

        // Act
        var results = ValidateModel(model);

        // Assert
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(ClassementFormModel.Nutaku)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_ShouldFail_WhenScoreIsNotPositive(int value)
    {
        // Arrange
        var model = CreateValidModel();
        model.Score = value;

        // Act
        var results = ValidateModel(model);

        // Assert
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(ClassementFormModel.Score)));
    }

    #endregion

    #region Valid Model Tests

    [Fact]
    public void Validate_ShouldPass_WhenAllFieldsAreValid()
    {
        // Arrange
        var model = CreateValidModel();

        // Act
        var results = ValidateModel(model);

        // Assert
        Assert.Empty(results);
    }

    #endregion

    #region Helper Methods

    private static ClassementFormModel CreateValidModel()
    {
        return new ClassementFormModel
        {
            DateEnregistrement = DateOnly.FromDateTime(DateTime.Now),
            Nutaku = 100,
            Top150 = 50,
            France = 25,
            Ligue = 10,
            Score = 50000
        };
    }

    private static List<ValidationResult> ValidateModel(ClassementFormModel model)
    {
        var context = new ValidationContext(model);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, context, results, validateAllProperties: true);
        return results;
    }

    #endregion
}
