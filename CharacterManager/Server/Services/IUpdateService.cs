namespace CharacterManager.Server.Services;

/// <summary>
/// Interface du service de vérification des mises à jour
/// </summary>
public interface IUpdateService
{
    Task<UpdateInfo?> CheckForUpdatesAsync();
}
