namespace CharacterManager.Server.Services;

/// <summary>
/// Interface du service de gestion de l'initialisation de la base de données
/// </summary>
public interface IDatabaseInitializationService
{
    /// <summary>
    /// Initializes the database with migrations and ensures all tables and columns exist
    /// </summary>
    Task InitializeDatabaseAsync();

    /// <summary>
    /// Initializes default AppSettings and checks database state
    /// </summary>
    Task InitializeAppSettingsAndCheckStateAsync();

        /// <summary>
        /// Génère rétroactivement l'historique de puissance Lucie à partir des classements et modifications existants
        /// </summary>
        Task<(int ClassementsTraites, int JoursTraites)> GenerateLuciePowerHistoryAsync(IHistoriqueModificationService historiqueService, IPersonnageService personnageService);
}
