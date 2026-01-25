using CharacterManager.Server.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using System.Security.Cryptography;

namespace CharacterManager.Server.Services;

/// <summary>
/// Helper service for authentication-related operations
/// Extracted from Program.cs for testability
/// </summary>
public class AuthenticationHelper : IAuthenticationHelper
{
    private readonly ProfileService _profileService;

    public AuthenticationHelper(ProfileService profileService)
    {
        _profileService = profileService;
    }

    /// <summary>
    /// Generates a cryptographically secure random password
    /// </summary>
    public string GenerateSecurePassword()
    {
        using var randomGenerator = RandomNumberGenerator.Create();
        byte[] data = new byte[16];
        randomGenerator.GetBytes(data);
        return BitConverter.ToString(data);
    }

    /// <summary>
    /// Validates login input parameters
    /// </summary>
    public (bool isValid, string? errorCode) ValidateLoginInput(string? username, string? password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return (false, "required");
        }
        return (true, null);
    }

    /// <summary>
    /// Ensures a default admin account exists, creating one if needed
    /// Returns the generated password if a new admin was created, null otherwise
    /// </summary>
    public async Task<string?> EnsureDefaultAdminExistsAsync()
    {
        var allProfiles = await _profileService.GetAllAsync();
        if (allProfiles != null && allProfiles.Count > 0)
            return null;

        var randomPassword = GenerateSecurePassword();
        await _profileService.CreateUserAsync("admin", randomPassword, "admin");
        return randomPassword;
    }

    /// <summary>
    /// Authenticates a profile by username and password
    /// Returns (profile, null) on success, (null, errorCode) on failure
    /// </summary>
    public async Task<(Profile? profile, string? errorCode, int? lockoutMinutes)> AuthenticateProfileAsync(
        string username, 
        string password)
    {
        var profile = await _profileService.GetByUsernameAsync(username);
        if (profile == null)
        {
            await _profileService.RegisterLoginAttemptAsync(username, false);
            return (null, "invalid", null);
        }

        // Check if account is locked
        if (profile.LockoutUntil.HasValue && profile.LockoutUntil.Value > DateTimeOffset.UtcNow)
        {
            var remaining = (int)(profile.LockoutUntil.Value - DateTimeOffset.UtcNow).TotalMinutes;
            return (null, "locked", remaining);
        }

        // Verify password
        if (!ProfileService.VerifyPassword(profile, password))
        {
            await _profileService.RegisterLoginAttemptAsync(username, false);
            return (null, "invalid", null);
        }

        // Register successful login
        await _profileService.RegisterLoginAttemptAsync(username, true);
        return (profile, null, null);
    }

    /// <summary>
    /// Creates claims for a profile to be used for authentication
    /// </summary>
    public ClaimsPrincipal CreateClaimsPrincipal(string username, Profile profile)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, profile.Role)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }

    /// <summary>
    /// Signs in a profile by creating authentication claims and session
    /// </summary>
    public async Task SignInProfileAsync(string username, Profile profile, HttpContext context)
    {
        var principal = CreateClaimsPrincipal(username, profile);
        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
    }
}
