namespace CharacterManager.Server.Services;

using CharacterManager.Server.Models;

public interface IHistoriqueLigueService
{
    Task<int?> GetHighestLeagueAsync();
    Task<List<HistoriqueLigue>> GetAllAsync();
    Task<HistoriqueLigue?> GetByIdAsync(int id);
    Task<HistoriqueLigue> AddAsync(HistoriqueLigue historique);
    Task<bool> UpdateAsync(HistoriqueLigue historique);
    Task<bool> DeleteAsync(int id);
}
