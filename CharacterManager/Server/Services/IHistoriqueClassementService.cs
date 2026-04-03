using CharacterManager.Server.Models;

namespace CharacterManager.Server.Services;

/// <summary>
/// Interface du service de gestion de l'historique des classements
/// </summary>
public interface IHistoriqueClassementService
{
    Task<List<HistoriqueClassement>> GetHistoriqueAsync();
    Task<List<HistoriqueClassement>> GetHistoriqueAsync(DateTime dateDebut, DateTime dateFin);
    Task<List<HistoriqueClassement>> GetHistoriqueRecentAsync(int nombre = 50);
    Task<int> GetMaxScoreAsync();
    Task SupprimerEnregistrementAsync(int id);
    Task ViderHistoriqueAsync();
    Task UpdateClassementEditableAsync(int id, DateOnly date, int ligue, int score, int nutaku, int top150, int france);
}
