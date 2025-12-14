namespace CharacterManager.Server.Models;

public class AppSettings
{
    public int Id { get; set; }
    public string LastImportedFileName { get; set; } = string.Empty;
    public DateTime? LastImportedDate { get; set; }
}
