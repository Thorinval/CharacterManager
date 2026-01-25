using CharacterManager.Server.Data;
using CharacterManager.Server.Models;
using CharacterManager.Server.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CharacterManager.Tests;

public class ProfileServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ProfileService _service;
    private readonly Mock<ILogger<ProfileService>> _loggerMock = new();

    public ProfileServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Security:Lockout:MaxAttempts"] = "3",
                ["Security:Lockout:LockoutMinutes"] = "5"
            })
            .Build();

        _service = new ProfileService(_context, config, _loggerMock.Object);
    }

    #region GetOrCreateAsync Tests

    [Fact]
    public async Task GetOrCreateAsync_ShouldCreateProfile_WhenNotExists()
    {
        // Act
        var profile = await _service.GetOrCreateAsync("newuser");

        // Assert
        Assert.NotNull(profile);
        Assert.Equal("newuser", profile.Username);
        Assert.Equal("fr", profile.Language);
        Assert.False(profile.AdultMode);
        Assert.Equal("utilisateur", profile.Role);
    }

    [Fact]
    public async Task GetOrCreateAsync_ShouldReturnExisting_WhenExists()
    {
        // Arrange
        var existing = new Profile { Username = "existing", Language = "en", AdultMode = true };
        _context.Profiles.Add(existing);
        await _context.SaveChangesAsync();

        // Act
        var profile = await _service.GetOrCreateAsync("existing");

        // Assert
        Assert.Equal("en", profile.Language);
        Assert.True(profile.AdultMode);
    }

    #endregion

    #region GetByUsernameAsync Tests

    [Fact]
    public async Task GetByUsernameAsync_ShouldReturnNull_WhenNotFound()
    {
        // Act
        var profile = await _service.GetByUsernameAsync("nonexistent");

        // Assert
        Assert.Null(profile);
    }

    [Fact]
    public async Task GetByUsernameAsync_ShouldReturnProfile_WhenFound()
    {
        // Arrange
        _context.Profiles.Add(new Profile { Username = "alice", Language = "de" });
        await _context.SaveChangesAsync();

        // Act
        var profile = await _service.GetByUsernameAsync("alice");

        // Assert
        Assert.NotNull(profile);
        Assert.Equal("alice", profile!.Username);
    }

    #endregion

    #region GetByUsername Tests

    [Fact]
    public void GetByUsername_ShouldReturnNull_WhenNotFound()
    {
        // Act
        var profile = _service.GetByUsername("nonexistent");

        // Assert
        Assert.Null(profile);
    }

    [Fact]
    public async Task GetByUsername_ShouldReturnProfile_WhenFound()
    {
        // Arrange
        _context.Profiles.Add(new Profile { Username = "bob" });
        await _context.SaveChangesAsync();

        // Act
        var profile = await _service.GetByUsernameAsync("bob");

        // Assert
        Assert.NotNull(profile);
    }

    #endregion

    #region CreateUserAsync Tests

    [Fact]
    public async Task CreateUserAsync_ShouldReturnFalse_WhenUserExists()
    {
        // Arrange
        _context.Profiles.Add(new Profile { Username = "existing" });
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CreateUserAsync("existing", "Password1!", "admin");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CreateUserAsync_ShouldCreateUser_WhenNotExists()
    {
        // Act
        var result = await _service.CreateUserAsync("newuser", "Password1!", "admin");

        // Assert
        Assert.True(result);
        var profile = await _context.Profiles.FirstOrDefaultAsync(p => p.Username == "newuser");
        Assert.NotNull(profile);
        Assert.Equal("admin", profile!.Role);
        Assert.Equal("PBKDF2", profile.HashAlgorithm);
    }

    [Fact]
    public async Task CreateUserAsync_ShouldUseDefaultRole_WhenRoleIsEmpty()
    {
        // Act
        await _service.CreateUserAsync("user1", "Password1!", "");

        // Assert
        var profile = await _context.Profiles.FirstAsync(p => p.Username == "user1");
        Assert.Equal("utilisateur", profile.Role);
    }

    #endregion

    #region ValidatePasswordStrength Tests

    [Theory]
    [InlineData("short", false, "Au moins 8 caractères")]
    [InlineData("alllowercase1!", false, "Inclure une majuscule")]
    [InlineData("ALLUPPERCASE1!", false, "Inclure une minuscule")]
    [InlineData("NoDigitsHere!", false, "Inclure un chiffre")]
    [InlineData("NoSymbols123A", false, "Inclure un symbole")]
    [InlineData("ValidPass1!", true, null)]
    public void ValidatePasswordStrength_ShouldValidateCorrectly(string password, bool expectedOk, string? expectedError)
    {
        // Act
        var (ok, error) = _service.ValidatePasswordStrength(password);

        // Assert
        Assert.Equal(expectedOk, ok);
        Assert.Equal(expectedError, error);
    }

    [Fact]
    public void ValidatePasswordStrength_ShouldRejectEmpty()
    {
        // Act
        var (ok, error) = _service.ValidatePasswordStrength("");

        // Assert
        Assert.False(ok);
        Assert.Contains("8 caractères", error!);
    }

    #endregion

    #region VerifyPassword Tests

    [Fact]
    public async Task VerifyPassword_ShouldReturnTrue_WhenPasswordMatches()
    {
        // Arrange
        await _service.CreateUserAsync("testuser", "MyPassword1!", "utilisateur");
        var profile = await _context.Profiles.FirstAsync(p => p.Username == "testuser");

        // Act
        var result = _service.VerifyPassword(profile, "MyPassword1!");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task VerifyPassword_ShouldReturnFalse_WhenPasswordDoesNotMatch()
    {
        // Arrange
        await _service.CreateUserAsync("testuser", "MyPassword1!", "utilisateur");
        var profile = await _context.Profiles.FirstAsync(p => p.Username == "testuser");

        // Act
        var result = _service.VerifyPassword(profile, "WrongPassword1!");

        // Assert
        Assert.False(result);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ShouldReturnFalse_WhenNotFound()
    {
        // Act
        var result = await _service.DeleteAsync("nonexistent");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveProfile()
    {
        // Arrange
        _context.Profiles.Add(new Profile { Username = "todelete" });
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.DeleteAsync("todelete");

        // Assert
        Assert.True(result);
        Assert.Null(await _context.Profiles.FirstOrDefaultAsync(p => p.Username == "todelete"));
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_ShouldReturnOrderedByUsername()
    {
        // Arrange
        _context.Profiles.AddRange(
            new Profile { Username = "charlie" },
            new Profile { Username = "alice" },
            new Profile { Username = "bob" });
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("alice", result[0].Username);
        Assert.Equal("bob", result[1].Username);
        Assert.Equal("charlie", result[2].Username);
    }

    #endregion

    #region UpdateRoleAsync Tests

    [Fact]
    public async Task UpdateRoleAsync_ShouldReturnFalse_WhenNotFound()
    {
        // Act
        var result = await _service.UpdateRoleAsync("nonexistent", "admin");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task UpdateRoleAsync_ShouldUpdateRole()
    {
        // Arrange
        _context.Profiles.Add(new Profile { Username = "user", Role = "utilisateur" });
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.UpdateRoleAsync("user", "ADMIN");

        // Assert
        Assert.True(result);
        var profile = await _context.Profiles.FirstAsync(p => p.Username == "user");
        Assert.Equal("admin", profile.Role); // lowercase
    }

    [Fact]
    public async Task UpdateRoleAsync_ShouldKeepExistingRole_WhenNewRoleIsEmpty()
    {
        // Arrange
        _context.Profiles.Add(new Profile { Username = "user", Role = "admin" });
        await _context.SaveChangesAsync();

        // Act
        await _service.UpdateRoleAsync("user", "");

        // Assert
        var profile = await _context.Profiles.FirstAsync(p => p.Username == "user");
        Assert.Equal("admin", profile.Role);
    }

    #endregion

    #region ResetPasswordAsync Tests

    [Fact]
    public async Task ResetPasswordAsync_ShouldReturnFalse_WhenPasswordWeak()
    {
        // Arrange
        _context.Profiles.Add(new Profile { Username = "user" });
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ResetPasswordAsync("user", "weak");

        // Assert
        Assert.False(result);
    }

    #endregion

    #region RegisterLoginAttemptAsync Tests

    [Fact]
    public async Task RegisterLoginAttemptAsync_ShouldReturnError_WhenUserNotFound()
    {
        // Act
        var (ok, error) = await _service.RegisterLoginAttemptAsync("nonexistent", false);

        // Assert
        Assert.False(ok);
        Assert.Equal("Utilisateur introuvable", error);
    }

    [Fact]
    public async Task RegisterLoginAttemptAsync_ShouldResetFailedCount_OnSuccess()
    {
        // Arrange
        _context.Profiles.Add(new Profile { Username = "user", FailedLoginCount = 2 });
        await _context.SaveChangesAsync();

        // Act
        await _service.RegisterLoginAttemptAsync("user", success: true);

        // Assert
        var profile = await _context.Profiles.FirstAsync(p => p.Username == "user");
        Assert.Equal(0, profile.FailedLoginCount);
        Assert.Null(profile.LockoutUntil);
    }

    [Fact]
    public async Task RegisterLoginAttemptAsync_ShouldIncrementFailedCount_OnFailure()
    {
        // Arrange
        _context.Profiles.Add(new Profile { Username = "user", FailedLoginCount = 0 });
        await _context.SaveChangesAsync();

        // Act
        await _service.RegisterLoginAttemptAsync("user", success: false);

        // Assert
        var profile = await _context.Profiles.FirstAsync(p => p.Username == "user");
        Assert.Equal(1, profile.FailedLoginCount);
    }

    [Fact]
    public async Task RegisterLoginAttemptAsync_ShouldLockout_AfterMaxAttempts()
    {
        // Arrange - MaxAttempts is 3 in config
        _context.Profiles.Add(new Profile { Username = "user", FailedLoginCount = 2 });
        await _context.SaveChangesAsync();

        // Act
        await _service.RegisterLoginAttemptAsync("user", success: false);

        // Assert
        var profile = await _context.Profiles.FirstAsync(p => p.Username == "user");
        Assert.Equal(0, profile.FailedLoginCount); // Reset after lockout
        Assert.NotNull(profile.LockoutUntil);
        Assert.True(profile.LockoutUntil > DateTimeOffset.UtcNow);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ShouldPersistChanges()
    {
        // Arrange
        var profile = new Profile { Username = "user", Language = "fr" };
        _context.Profiles.Add(profile);
        await _context.SaveChangesAsync();

        // Act
        profile.Language = "en";
        profile.AdultMode = true;
        await _service.UpdateAsync(profile);

        // Assert
        var updated = await _context.Profiles.FirstAsync(p => p.Username == "user");
        Assert.Equal("en", updated.Language);
        Assert.True(updated.AdultMode);
    }

    #endregion

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _context?.Dispose();
        }
    }
}
