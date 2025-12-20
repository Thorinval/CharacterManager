using CharacterManager.Server.Data;
using CharacterManager.Server.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CharacterManager.Server.Services;

public class ProfileService
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _config;

    public ProfileService(ApplicationDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<Profile> GetOrCreateAsync(string username)
    {
        var existing = await _db.Profiles.FirstOrDefaultAsync(p => p.Username == username);
        if (existing != null) return existing;
        var prof = new Profile { Username = username, AdultMode = false, Language = "fr", Role = "utilisateur" };
        _db.Profiles.Add(prof);
        await _db.SaveChangesAsync();
        return prof;
    }

    public async Task UpdateAsync(Profile profile)
    {
        _db.Profiles.Update(profile);
        await _db.SaveChangesAsync();
    }

    public async Task<Profile?> GetByUsernameAsync(string username)
        => await _db.Profiles.FirstOrDefaultAsync(p => p.Username == username);

    public Profile? GetByUsername(string username)
        => _db.Profiles.FirstOrDefault(p => p.Username == username);

    public async Task<bool> CreateUserAsync(string username, string password, string role)
    {
        if (await _db.Profiles.AnyAsync(p => p.Username == username)) return false;
        var (hash, salt) = HashPassword(password);
        var prof = new Profile
        {
            Username = username,
            Role = string.IsNullOrWhiteSpace(role) ? "utilisateur" : role.ToLower(),
            PasswordHash = hash,
            PasswordSalt = salt,
            HashAlgorithm = "PBKDF2",
            AdultMode = false,
            Language = "fr"
        };
        _db.Profiles.Add(prof);
        await _db.SaveChangesAsync();
        return true;
    }

    public bool VerifyPassword(Profile profile, string password)
        => VerifyPbkdf2(password, profile.PasswordHash, profile.PasswordSalt);

    public (bool ok, string? error) ValidatePasswordStrength(string password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < 8) return (false, "Au moins 8 caractères");
        if (!password.Any(char.IsUpper)) return (false, "Inclure une majuscule");
        if (!password.Any(char.IsLower)) return (false, "Inclure une minuscule");
        if (!password.Any(char.IsDigit)) return (false, "Inclure un chiffre");
        if (!password.Any(ch => "!@#$%^&*()-_=+[]{};:'\"|,.<>/?".Contains(ch))) return (false, "Inclure un symbole");
        return (true, null);
    }

    public async Task<bool> DeleteAsync(string username)
    {
        var p = await GetByUsernameAsync(username);
        if (p == null) return false;
        _db.Profiles.Remove(p);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<Profile>> GetAllAsync()
        => await _db.Profiles.OrderBy(p => p.Username).ToListAsync();

    public async Task<bool> UpdateRoleAsync(string username, string role)
    {
        var p = await GetByUsernameAsync(username);
        if (p == null) return false;
        p.Role = string.IsNullOrWhiteSpace(role) ? p.Role : role.ToLower();
        _db.Profiles.Update(p);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ResetPasswordAsync(string username, string newPassword)
    {
        var (ok, _) = ValidatePasswordStrength(newPassword);
        if (!ok) return false;
        return await ChangePasswordAsync(username, newPassword);
    }

    public async Task<(bool ok, string? error)> RegisterLoginAttemptAsync(string username, bool success)
    {
        var p = await GetByUsernameAsync(username);
        if (p == null) return (false, "Utilisateur introuvable");
        
        var maxAttempts = _config.GetValue<int>("Security:Lockout:MaxAttempts", 5);
        var lockoutMinutes = _config.GetValue<int>("Security:Lockout:LockoutMinutes", 15);
        
        if (!success)
        {
            p.FailedLoginCount += 1;
            if (p.FailedLoginCount >= maxAttempts)
            {
                p.LockoutUntil = DateTimeOffset.UtcNow.AddMinutes(lockoutMinutes);
                p.FailedLoginCount = 0;
            }
        }
        else
        {
            p.FailedLoginCount = 0;
            p.LockoutUntil = null;
        }
        _db.Profiles.Update(p);
        await _db.SaveChangesAsync();
        return (true, null);
    }

    private static (string hash, string salt) HashPassword(string password)
    {
        // PBKDF2 with random salt
        var saltBytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(16);
        using var pbkdf2 = new System.Security.Cryptography.Rfc2898DeriveBytes(password, saltBytes, 100_000, System.Security.Cryptography.HashAlgorithmName.SHA256);
        var hash = pbkdf2.GetBytes(32);
        return (Convert.ToHexString(hash), Convert.ToHexString(saltBytes));
    }

    private static bool VerifyPbkdf2(string password, string hexHash, string hexSalt)
    {
        var saltBytes = Convert.FromHexString(hexSalt);
        using var pbkdf2 = new System.Security.Cryptography.Rfc2898DeriveBytes(password, saltBytes, 100_000, System.Security.Cryptography.HashAlgorithmName.SHA256);
        var hash = pbkdf2.GetBytes(32);
        var provided = Convert.ToHexString(hash);
        return string.Equals(provided, hexHash, StringComparison.Ordinal);
    }

    public async Task<bool> ChangePasswordAsync(string username, string newPassword)
    {
        var profile = await GetByUsernameAsync(username);
        if (profile == null) return false;
        var (hash, salt) = HashPassword(newPassword);
        profile.PasswordHash = hash;
        profile.PasswordSalt = salt;
        profile.HashAlgorithm = "PBKDF2";
        _db.Profiles.Update(profile);
        await _db.SaveChangesAsync();
        return true;
    }
}
