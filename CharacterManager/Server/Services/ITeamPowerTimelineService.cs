using System.Threading.Tasks;

namespace CharacterManager.Server.Services;

public interface ITeamPowerTimelineService
{
    Task RecomputeAllAsync();
    Task RecomputeFromDateAsync(DateOnly startDate);
    /// <summary>
    /// Initializes timeline with seed data if the table is empty (called on startup)
    /// </summary>
    Task SeedIfEmptyAsync();
}
