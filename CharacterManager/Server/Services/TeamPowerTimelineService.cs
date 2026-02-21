using CharacterManager.Server.Data;
using CharacterManager.Server.Models;
using CharacterManager.Server.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace CharacterManager.Server.Services;

public class TeamPowerTimelineService(ApplicationDbContext db, IStatistiquesService statistiquesService) : ITeamPowerTimelineService
{
    private readonly ApplicationDbContext _db = db;
    private readonly IStatistiquesService _stats = statistiquesService;

    public async Task RecomputeAllAsync()
    {
        await EnsureTableAsync();

        var selected = _stats.GetSelectedTeamPowerEvolutionData();
        var best = _stats.GetBestTeamPowerEvolutionData();

        // Clear and refill
        var existing = _db.TeamPowerTimelineRecords.ToList();
        if (existing.Any())
        {
            _db.TeamPowerTimelineRecords.RemoveRange(existing);
            await _db.SaveChangesAsync();
        }

        var toInsert = new List<TeamPowerTimelineRecord>();
        toInsert.AddRange(selected.Select(s => new TeamPowerTimelineRecord
        {
            Date = s.Date,
            Type = TeamPowerTimelineType.Selected,
            TotalPower = s.TotalPower,
            DateInsertion = DateTime.Now
        }));
        toInsert.AddRange(best.Select(b => new TeamPowerTimelineRecord
        {
            Date = b.Date,
            Type = TeamPowerTimelineType.Best,
            TotalPower = b.TotalPower,
            DateInsertion = DateTime.Now
        }));

        _db.TeamPowerTimelineRecords.AddRange(toInsert);
        await _db.SaveChangesAsync();
    }

    public async Task RecomputeFromDateAsync(DateOnly startDate)
    {
        await EnsureTableAsync();

        var selected = _stats.GetSelectedTeamPowerEvolutionData();
        var best = _stats.GetBestTeamPowerEvolutionData();

        var start = startDate;

        // Build maps for quick lookup
        var selectedMap = selected.ToDictionary(x => x.Date, x => x.TotalPower);
        var bestMap = best.ToDictionary(x => x.Date, x => x.TotalPower);

        var existing = _db.TeamPowerTimelineRecords
            .Where(r => r.Date >= start)
            .ToList();

        // Remove any records on/after start that no longer exist
        var toRemove = existing.Where(r =>
            (r.Type == TeamPowerTimelineType.Selected && !selectedMap.ContainsKey(r.Date)) ||
            (r.Type == TeamPowerTimelineType.Best && !bestMap.ContainsKey(r.Date)))
            .ToList();
        if (toRemove.Any())
        {
            _db.TeamPowerTimelineRecords.RemoveRange(toRemove);
            await _db.SaveChangesAsync();
        }

        // Upsert selected
        foreach (var kv in selectedMap.Where(kv => kv.Key >= start))
        {
            var record = _db.TeamPowerTimelineRecords
                .FirstOrDefault(r => r.Date == kv.Key && r.Type == TeamPowerTimelineType.Selected);
            if (record == null)
            {
                _db.TeamPowerTimelineRecords.Add(new TeamPowerTimelineRecord
                {
                    Date = kv.Key,
                    Type = TeamPowerTimelineType.Selected,
                    TotalPower = kv.Value,
                    DateInsertion = DateTime.Now
                });
            }
            else if (record.TotalPower != kv.Value)
            {
                record.TotalPower = kv.Value;
            }
        }

        // Upsert best
        foreach (var kv in bestMap.Where(kv => kv.Key >= start))
        {
            var record = _db.TeamPowerTimelineRecords
                .FirstOrDefault(r => r.Date == kv.Key && r.Type == TeamPowerTimelineType.Best);
            if (record == null)
            {
                _db.TeamPowerTimelineRecords.Add(new TeamPowerTimelineRecord
                {
                    Date = kv.Key,
                    Type = TeamPowerTimelineType.Best,
                    TotalPower = kv.Value,
                    DateInsertion = DateTime.Now
                });
            }
            else if (record.TotalPower != kv.Value)
            {
                record.TotalPower = kv.Value;
            }
        }

        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Initializes the timeline table if empty (seed on startup)
    /// </summary>
    public async Task SeedIfEmptyAsync()
    {
        try
        {
            await EnsureTableAsync();

            // Check if timeline is already populated
            var hasData = _db.TeamPowerTimelineRecords.Any();
            if (!hasData)
            {
                // Seed with computed data
                await RecomputeAllAsync();
            }
        }
        catch
        {
            // Non-blocking: failure to seed should not prevent app startup
        }
    }

    private async Task EnsureTableAsync()
    {
        try
        {
            await _db.Database.ExecuteSqlRawAsync(
                "CREATE TABLE IF NOT EXISTS TeamPowerTimelineRecords (\n" +
                "Id INTEGER PRIMARY KEY AUTOINCREMENT,\n" +
                "Date TEXT NOT NULL,\n" +
                "Type INTEGER NOT NULL,\n" +
                "TotalPower INTEGER NOT NULL,\n" +
                "DateInsertion TEXT NOT NULL\n" +
                ");");

            await _db.Database.ExecuteSqlRawAsync(
                "CREATE INDEX IF NOT EXISTS IX_TeamPowerTimelineRecords_Date_Type ON TeamPowerTimelineRecords(Date, Type);");

            // Try to add unique constraint (ignore if it already exists)
            try
            {
                await _db.Database.ExecuteSqlRawAsync(
                    "CREATE UNIQUE INDEX IF NOT EXISTS UX_TeamPowerTimelineRecords_Date_Type_Unique ON TeamPowerTimelineRecords(Date, Type);");
            }
            catch
            {
                // Unique constraint may already exist from migration
            }
        }
        catch
        {
            // Ignore if EF migrations already created the table/index
        }
    }
}
