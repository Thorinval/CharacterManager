using CharacterManager.Server.Data;
using CharacterManager.Server.Models;
using CharacterManager.Server.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;

namespace CharacterManager.Tests;

/// <summary>
/// Tests for the AuthenticationHelper service
/// </summary>
public class AuthenticationHelperTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _context;
    private readonly ProfileService _profileService;
    private readonly AuthenticationHelper _authHelper;
    private readonly Mock<ILogger<ProfileService>> _loggerMock;

    public AuthenticationHelperTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
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

        _loggerMock = new Mock<ILogger<ProfileService>>();
        _profileService = new ProfileService(_context, config, _loggerMock.Object);
        _authHelper = new AuthenticationHelper(_profileService);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    #region GenerateSecurePassword Tests

    [Fact]
    public void GenerateSecurePassword_ReturnsNonEmptyString()
    {
        // Act
        var password = _authHelper.GenerateSecurePassword();

        // Assert
        Assert.False(string.IsNullOrEmpty(password));
    }

    [Fact]
    public void GenerateSecurePassword_ReturnsDifferentPasswordsOnEachCall()
    {
        // Act
        var password1 = _authHelper.GenerateSecurePassword();
        var password2 = _authHelper.GenerateSecurePassword();

        // Assert
        Assert.NotEqual(password1, password2);
    }

    [Fact]
    public void GenerateSecurePassword_ReturnsPasswordWithExpectedFormat()
    {
        // Act
        var password = _authHelper.GenerateSecurePassword();

        // Assert - BitConverter.ToString returns hex with dashes (e.g., "A1-B2-C3-...")
        Assert.Contains("-", password);
        Assert.True(password.Length > 30); // 16 bytes = 47 chars (XX-XX-XX-...)
    }

    [Fact]
    public void GenerateSecurePassword_GeneratesMultipleUniquePasswords()
    {
        // Act
        var passwords = new HashSet<string>();
        for (int i = 0; i < 100; i++)
        {
            passwords.Add(_authHelper.GenerateSecurePassword());
        }

        // Assert - All 100 should be unique
        Assert.Equal(100, passwords.Count);
    }

    #endregion

    #region ValidateLoginInput Tests

    [Fact]
    public void ValidateLoginInput_WithValidCredentials_ReturnsTrue()
    {
        // Act
        var (isValid, errorCode) = _authHelper.ValidateLoginInput("user", "password");

        // Assert
        Assert.True(isValid);
        Assert.Null(errorCode);
    }

    [Fact]
    public void ValidateLoginInput_WithNullUsername_ReturnsFalse()
    {
        // Act
        var (isValid, errorCode) = _authHelper.ValidateLoginInput(null, "password");

        // Assert
        Assert.False(isValid);
        Assert.Equal("required", errorCode);
    }

    [Fact]
    public void ValidateLoginInput_WithEmptyUsername_ReturnsFalse()
    {
        // Act
        var (isValid, errorCode) = _authHelper.ValidateLoginInput("", "password");

        // Assert
        Assert.False(isValid);
        Assert.Equal("required", errorCode);
    }

    [Fact]
    public void ValidateLoginInput_WithWhitespaceUsername_ReturnsFalse()
    {
        // Act
        var (isValid, errorCode) = _authHelper.ValidateLoginInput("   ", "password");

        // Assert
        Assert.False(isValid);
        Assert.Equal("required", errorCode);
    }

    [Fact]
    public void ValidateLoginInput_WithNullPassword_ReturnsFalse()
    {
        // Act
        var (isValid, errorCode) = _authHelper.ValidateLoginInput("user", null);

        // Assert
        Assert.False(isValid);
        Assert.Equal("required", errorCode);
    }

    [Fact]
    public void ValidateLoginInput_WithEmptyPassword_ReturnsFalse()
    {
        // Act
        var (isValid, errorCode) = _authHelper.ValidateLoginInput("user", "");

        // Assert
        Assert.False(isValid);
        Assert.Equal("required", errorCode);
    }

    [Fact]
    public void ValidateLoginInput_WithWhitespacePassword_ReturnsFalse()
    {
        // Act
        var (isValid, errorCode) = _authHelper.ValidateLoginInput("user", "   ");

        // Assert
        Assert.False(isValid);
        Assert.Equal("required", errorCode);
    }

    [Fact]
    public void ValidateLoginInput_WithBothEmpty_ReturnsFalse()
    {
        // Act
        var (isValid, errorCode) = _authHelper.ValidateLoginInput("", "");

        // Assert
        Assert.False(isValid);
        Assert.Equal("required", errorCode);
    }

    #endregion

    #region EnsureDefaultAdminExistsAsync Tests

    [Fact]
    public async Task EnsureDefaultAdminExistsAsync_WhenNoProfilesExist_CreatesAdmin()
    {
        // Arrange - No profiles in database

        // Act
        var generatedPassword = await _authHelper.EnsureDefaultAdminExistsAsync();

        // Assert
        Assert.NotNull(generatedPassword);
        var admin = await _profileService.GetByUsernameAsync("admin");
        Assert.NotNull(admin);
        Assert.Equal("admin", admin.Role);
    }

    [Fact]
    public async Task EnsureDefaultAdminExistsAsync_WhenProfilesExist_ReturnsNull()
    {
        // Arrange - Create an existing profile
        await _profileService.CreateUserAsync("existingUser", "password", "user");

        // Act
        var result = await _authHelper.EnsureDefaultAdminExistsAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task EnsureDefaultAdminExistsAsync_ReturnsValidPassword()
    {
        // Act
        var generatedPassword = await _authHelper.EnsureDefaultAdminExistsAsync();

        // Assert
        Assert.NotNull(generatedPassword);
        Assert.True(generatedPassword.Length > 30);
    }

    #endregion

    #region AuthenticateProfileAsync Tests

    [Fact]
    public async Task AuthenticateProfileAsync_WithValidCredentials_ReturnsProfile()
    {
        // Arrange
        await _profileService.CreateUserAsync("testuser", "testpassword", "user");

        // Act
        var (profile, errorCode, lockoutMinutes) = await _authHelper.AuthenticateProfileAsync("testuser", "testpassword");

        // Assert
        Assert.NotNull(profile);
        Assert.Null(errorCode);
        Assert.Null(lockoutMinutes);
        Assert.Equal("testuser", profile.Username);
    }

    [Fact]
    public async Task AuthenticateProfileAsync_WithInvalidUsername_ReturnsInvalidError()
    {
        // Arrange
        await _profileService.CreateUserAsync("testuser", "testpassword", "user");

        // Act
        var (profile, errorCode, lockoutMinutes) = await _authHelper.AuthenticateProfileAsync("wronguser", "testpassword");

        // Assert
        Assert.Null(profile);
        Assert.Equal("invalid", errorCode);
        Assert.Null(lockoutMinutes);
    }

    [Fact]
    public async Task AuthenticateProfileAsync_WithInvalidPassword_ReturnsInvalidError()
    {
        // Arrange
        await _profileService.CreateUserAsync("testuser", "testpassword", "user");

        // Act
        var (profile, errorCode, lockoutMinutes) = await _authHelper.AuthenticateProfileAsync("testuser", "wrongpassword");

        // Assert
        Assert.Null(profile);
        Assert.Equal("invalid", errorCode);
        Assert.Null(lockoutMinutes);
    }

    [Fact]
    public async Task AuthenticateProfileAsync_WithLockedAccount_ReturnsLockedError()
    {
        // Arrange
        await _profileService.CreateUserAsync("testuser", "testpassword", "user");
        var profile = await _profileService.GetByUsernameAsync("testuser");
        profile!.LockoutUntil = DateTimeOffset.UtcNow.AddMinutes(10);
        await _context.SaveChangesAsync();

        // Act
        var (resultProfile, errorCode, lockoutMinutes) = await _authHelper.AuthenticateProfileAsync("testuser", "testpassword");

        // Assert
        Assert.Null(resultProfile);
        Assert.Equal("locked", errorCode);
        Assert.NotNull(lockoutMinutes);
        Assert.True(lockoutMinutes > 0);
    }

    [Fact]
    public async Task AuthenticateProfileAsync_WithExpiredLockout_AuthenticatesSuccessfully()
    {
        // Arrange
        await _profileService.CreateUserAsync("testuser", "testpassword", "user");
        var profile = await _profileService.GetByUsernameAsync("testuser");
        profile!.LockoutUntil = DateTimeOffset.UtcNow.AddMinutes(-10); // Expired lockout
        await _context.SaveChangesAsync();

        // Act
        var (resultProfile, errorCode, lockoutMinutes) = await _authHelper.AuthenticateProfileAsync("testuser", "testpassword");

        // Assert
        Assert.NotNull(resultProfile);
        Assert.Null(errorCode);
        Assert.Null(lockoutMinutes);
    }

    #endregion

    #region CreateClaimsPrincipal Tests

    [Fact]
    public async Task CreateClaimsPrincipal_ReturnsValidPrincipal()
    {
        // Arrange
        await _profileService.CreateUserAsync("testuser", "testpassword", "admin");
        var profile = await _profileService.GetByUsernameAsync("testuser");

        // Act
        var principal = _authHelper.CreateClaimsPrincipal("testuser", profile!);

        // Assert
        Assert.NotNull(principal);
        Assert.True(principal.Identity!.IsAuthenticated);
    }

    [Fact]
    public async Task CreateClaimsPrincipal_ContainsNameClaim()
    {
        // Arrange
        await _profileService.CreateUserAsync("testuser", "testpassword", "user");
        var profile = await _profileService.GetByUsernameAsync("testuser");

        // Act
        var principal = _authHelper.CreateClaimsPrincipal("testuser", profile!);

        // Assert
        var nameClaim = principal.FindFirst(ClaimTypes.Name);
        Assert.NotNull(nameClaim);
        Assert.Equal("testuser", nameClaim.Value);
    }

    [Fact]
    public async Task CreateClaimsPrincipal_ContainsRoleClaim()
    {
        // Arrange
        await _profileService.CreateUserAsync("testuser", "testpassword", "admin");
        var profile = await _profileService.GetByUsernameAsync("testuser");

        // Act
        var principal = _authHelper.CreateClaimsPrincipal("testuser", profile!);

        // Assert
        var roleClaim = principal.FindFirst(ClaimTypes.Role);
        Assert.NotNull(roleClaim);
        Assert.Equal("admin", roleClaim.Value);
    }

    [Fact]
    public async Task CreateClaimsPrincipal_HasCookieAuthenticationScheme()
    {
        // Arrange
        await _profileService.CreateUserAsync("testuser", "testpassword", "user");
        var profile = await _profileService.GetByUsernameAsync("testuser");

        // Act
        var principal = _authHelper.CreateClaimsPrincipal("testuser", profile!);

        // Assert
        Assert.Equal("Cookies", principal.Identity!.AuthenticationType);
    }

    #endregion
}
