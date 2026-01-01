using CharacterManager.Server.Data;
using CharacterManager.Server.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace CharacterManager.Server.Services;

public class HistoriqueLigueService
{
    private readonly ApplicationDbContext _context;

    public HistoriqueLigueService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<HistoriqueLigue>> GetAllAsync()
    {
        return await _context.HistoriquesLigue
            .OrderByDescending(h => h.DatePassage)
            .ThenByDescending(h => h.Ligue)
            .ToListAsync();
    }

    public async Task<HistoriqueLigue?> GetByIdAsync(int id)
    {
        return await _context.HistoriquesLigue.FindAsync(id);
    }

    public async Task<HistoriqueLigue> AddAsync(HistoriqueLigue historique)
    {
        _context.HistoriquesLigue.Add(historique);
        await _context.SaveChangesAsync();
        return historique;
    }

    public async Task<int?> GetHighestLeagueAsync()
    {
        // Elite Top 50 is the highest, then Ligue 1, 2, etc.
        return await _context.HistoriquesLigue
            .OrderByDescending(h => h.Ligue == 50)
            .ThenBy(h => h.Ligue)
            .Select(h => (int?)h.Ligue)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> UpdateAsync(HistoriqueLigue historique)
    {
        var existing = await _context.HistoriquesLigue.FindAsync(historique.Id);
        if (existing == null)
            return false;

        existing.DatePassage = historique.DatePassage;
        existing.Ligue = historique.Ligue;
        existing.Notes = historique.Notes;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var historique = await _context.HistoriquesLigue.FindAsync(id);
        if (historique == null)
            return false;

        _context.HistoriquesLigue.Remove(historique);
        await _context.SaveChangesAsync();
        return true;
    }
}
