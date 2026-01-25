namespace CharacterManager.Server.Models;

public enum ImportLogLevel
{
    Ok,
    Warning,
    Error,
    Duplicate
}

public enum ImportLogCategory
{
    General,
    Classement,
    Commandant,
    Mercenaires,
    Androides,
    Lucie,
    Capacites,
    Historique
}

public class ImportLogEntry
{
    public ImportLogLevel Level { get; set; }
    public ImportLogCategory Category { get; set; }
    public string DataType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
