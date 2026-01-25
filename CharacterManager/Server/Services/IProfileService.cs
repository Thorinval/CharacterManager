using CharacterManager.Server.Models;

namespace CharacterManager.Server.Services;

/// <summary>
/// Interface du service de gestion des profils utilisateurs
/// </summary>
public interface IProfileService
{
    Task<Profile> GetOrCreateAsync(string username);
    Task UpdateAsync(Profile profile);
    Task<Profile?> GetByUsernameAsync(string username);
    Profile? GetByUsername(string username);
    Task<bool> CreateUserAsync(string username, string password, string role);
    Task<bool> DeleteAsync(string username);
    Task<List<Profile>> GetAllAsync();
    Task<bool> UpdateRoleAsync(string username, string role);
    Task<bool> ResetPasswordAsync(string username, string newPassword);
    Task<(bool ok, string? error)> RegisterLoginAttemptAsync(string username, bool success);
}
