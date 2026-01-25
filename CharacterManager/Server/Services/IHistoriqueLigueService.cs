namespace CharacterManager.Server.Services;

public interface IHistoriqueLigueService
{
    Task<int?> GetHighestLeagueAsync();
}
