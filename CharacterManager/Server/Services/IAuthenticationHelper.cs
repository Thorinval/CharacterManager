using CharacterManager.Server.Models;
using System.Security.Claims;

namespace CharacterManager.Server.Services;

/// <summary>
/// Interface for authentication helper operations
/// </summary>
public interface IAuthenticationHelper
{
    /// <summary>
    /// Generates a cryptographically secure random password
    /// </summary>
    string GenerateSecurePassword();

    /// <summary>
    /// Validates login input parameters
    /// </summary>
    (bool isValid, string? errorCode) ValidateLoginInput(string? username, string? password);

    /// <summary>
    /// Ensures a default admin account exists, creating one if needed
    /// Returns the generated password if a new admin was created, null otherwise
    /// </summary>
    Task<string?> EnsureDefaultAdminExistsAsync();

    /// <summary>
    /// Authenticates a profile by username and password
    /// Returns (profile, null, null) on success, (null, errorCode, lockoutMinutes) on failure
    /// </summary>
    Task<(Profile? profile, string? errorCode, int? lockoutMinutes)> AuthenticateProfileAsync(string username, string password);

    /// <summary>
    /// Creates claims for a profile to be used for authentication
    /// </summary>
    ClaimsPrincipal CreateClaimsPrincipal(string username, Profile profile);

    /// <summary>
    /// Signs in a profile by creating authentication claims and session
    /// </summary>
    Task SignInProfileAsync(string username, Profile profile, HttpContext context);
}
