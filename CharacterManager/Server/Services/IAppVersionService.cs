namespace CharacterManager.Server.Services;

/// <summary>
/// Interface du service de gestion des informations de version de l'application
/// </summary>
public interface IAppVersionService
{
    string GetAppName();
    string GetAppVersion();
    string GetAuthor();
    string GetDescription();
    string GetBuildVersion();
    string GetFileVersion();
    string GetGitCommitHash();
}
