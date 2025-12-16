namespace CharacterManager.Server.Models;

public class Profile
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public bool AdultMode { get; set; } = false;
    public string Language { get; set; } = "fr";
    public string Role { get; set; } = "utilisateur"; // or "admin"
    public string PasswordHash { get; set; } = string.Empty;
    public string PasswordSalt { get; set; } = string.Empty;
    public string HashAlgorithm { get; set; } = "PBKDF2";
    public int FailedLoginCount { get; set; } = 0;
    public DateTimeOffset? LockoutUntil { get; set; }
}
