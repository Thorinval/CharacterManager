using CharacterManager.Server.Data;
using CharacterManager.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace CharacterManager.Server.Services;

public class CapaciteService
{
    private readonly ApplicationDbContext _context;

    public CapaciteService(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Récupère toutes les capacités
    /// </summary>
    public async Task<List<Capacite>> GetAllAsync()
    {
        return await _context.Capacites.OrderBy(c => c.Nom).ToListAsync();
    }

    /// <summary>
    /// Récupère une capacité par son ID
    /// </summary>
    public async Task<Capacite?> GetByIdAsync(int id)
    {
        return await _context.Capacites.FirstOrDefaultAsync(c => c.Id == id);
    }

    /// <summary>
    /// Crée une nouvelle capacité
    /// </summary>
    public async Task<Capacite> CreateAsync(Capacite capacite)
    {
        if (string.IsNullOrWhiteSpace(capacite.Nom))
        {
            throw new ArgumentException("Le nom de la capacité est requis.");
        }

        _context.Capacites.Add(capacite);
        await _context.SaveChangesAsync();
        return capacite;
    }

    /// <summary>
    /// Met à jour une capacité existante
    /// </summary>
    public async Task<Capacite> UpdateAsync(int id, Capacite capacite)
    {
        var existing = await _context.Capacites.FirstOrDefaultAsync(c => c.Id == id);
        if (existing == null)
        {
            throw new InvalidOperationException($"Aucune capacité avec l'ID {id} n'a été trouvée.");
        }

        if (string.IsNullOrWhiteSpace(capacite.Nom))
        {
            throw new ArgumentException("Le nom de la capacité est requis.");
        }

        existing.Nom = capacite.Nom;
        existing.Description = capacite.Description ?? string.Empty;
        existing.Icon = capacite.Icon ?? string.Empty;

        await _context.SaveChangesAsync();
        return existing;
    }

    /// <summary>
    /// Supprime une capacité par son ID
    /// </summary>
    public async Task DeleteAsync(int id)
    {
        var capacite = await _context.Capacites.FirstOrDefaultAsync(c => c.Id == id);
        if (capacite != null)
        {
            _context.Capacites.Remove(capacite);
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Obtient le nombre total de capacités
    /// </summary>
    public int GetCount()
    {
        return _context.Capacites.Count();
    }
}
