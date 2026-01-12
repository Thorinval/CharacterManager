using System.Text.Json;
using CharacterManager.Server.Data;
using CharacterManager.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace CharacterManager.Server.Services;

/// <summary>
/// Service de gestion de l'historique des modifications
/// </summary>
public class HistoriqueModificationService
{
    private readonly ApplicationDbContext _context;

    public HistoriqueModificationService(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Enregistre une création d'entité
    /// </summary>
    public async Task EnregistrerCreationAsync(
        TypeEntite typeEntite,
        int entiteId,
        string nomEntite,
        object? donnees,
        string? description = null)
    {
        var historique = new HistoriqueModification
        {
            TypeEntite = typeEntite,
            EntiteId = entiteId,
            NomEntite = nomEntite,
            TypeModification = TypeModification.Creation,
            DateModification = DateTime.UtcNow,
            NouvelleValeur = donnees != null ? JsonSerializer.Serialize(donnees) : null,
            Description = description ?? $"Création de {nomEntite}"
        };

        _context.HistoriquesModifications.Add(historique);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Enregistre une modification d'entité.
    /// Si une modification du même type (même TypeEntite, EntiteId, TypeModification et ChampModifie) 
    /// a eu lieu il y a moins de 5 secondes, la ligne existante est mise à jour au lieu d'en ajouter une nouvelle.
    /// </summary>
    public async Task EnregistrerModificationAsync(
        TypeEntite typeEntite,
        int entiteId,
        string nomEntite,
        string champModifie,
        object? ancienneValeur,
        object? nouvelleValeur,
        string? description = null)
    {
        var dateDebut = DateTime.UtcNow.AddSeconds(-5);
        
        // Cherche une modification récente du même type
        var derniereModification = await _context.HistoriquesModifications
            .Where(h => h.TypeEntite == typeEntite 
                && h.EntiteId == entiteId 
                && h.TypeModification == TypeModification.Modification 
                && h.ChampModifie == champModifie
                && h.DateModification >= dateDebut)
            .OrderByDescending(h => h.DateModification)
            .FirstOrDefaultAsync();
        
        if (derniereModification != null)
        {
            // Mise à jour de la ligne existante
            derniereModification.DateModification = DateTime.UtcNow;
            derniereModification.NouvelleValeur = nouvelleValeur != null ? JsonSerializer.Serialize(nouvelleValeur) : null;
            derniereModification.Description = description ?? $"Modification de {champModifie} pour {nomEntite}";
            
            _context.HistoriquesModifications.Update(derniereModification);
        }
        else
        {
            // Création d'une nouvelle ligne
            var historique = new HistoriqueModification
            {
                TypeEntite = typeEntite,
                EntiteId = entiteId,
                NomEntite = nomEntite,
                TypeModification = TypeModification.Modification,
                DateModification = DateTime.UtcNow,
                ChampModifie = champModifie,
                AncienneValeur = ancienneValeur != null ? JsonSerializer.Serialize(ancienneValeur) : null,
                NouvelleValeur = nouvelleValeur != null ? JsonSerializer.Serialize(nouvelleValeur) : null,
                Description = description ?? $"Modification de {champModifie} pour {nomEntite}"
            };

            _context.HistoriquesModifications.Add(historique);
        }
        
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Enregistre une suppression d'entité
    /// </summary>
    public async Task EnregistrerSuppressionAsync(
        TypeEntite typeEntite,
        int entiteId,
        string nomEntite,
        object? donnees,
        string? description = null)
    {
        var historique = new HistoriqueModification
        {
            TypeEntite = typeEntite,
            EntiteId = entiteId,
            NomEntite = nomEntite,
            TypeModification = TypeModification.Suppression,
            DateModification = DateTime.UtcNow,
            AncienneValeur = donnees != null ? JsonSerializer.Serialize(donnees) : null,
            Description = description ?? $"Suppression de {nomEntite}"
        };

        _context.HistoriquesModifications.Add(historique);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Récupère l'historique avec filtres optionnels
    /// </summary>
    public async Task<List<HistoriqueModification>> GetHistoriqueAsync(
        TypeEntite? typeEntite = null,
        int? entiteId = null,
        DateTime? dateDebut = null,
        DateTime? dateFin = null,
        int? limit = null)
    {
        var query = _context.HistoriquesModifications.AsQueryable();

        if (typeEntite.HasValue)
            query = query.Where(h => h.TypeEntite == typeEntite.Value);

        if (entiteId.HasValue)
            query = query.Where(h => h.EntiteId == entiteId.Value);

        if (dateDebut.HasValue)
            query = query.Where(h => h.DateModification >= dateDebut.Value);

        if (dateFin.HasValue)
            query = query.Where(h => h.DateModification <= dateFin.Value);

        query = query.OrderByDescending(h => h.DateModification);

        if (limit.HasValue)
            query = query.Take(limit.Value);

        return await query.ToListAsync();
    }

    /// <summary>
    /// Récupère l'historique pour une entité spécifique
    /// </summary>
    public async Task<List<HistoriqueModification>> GetHistoriqueEntiteAsync(TypeEntite typeEntite, int entiteId)
    {
        return await _context.HistoriquesModifications
            .Where(h => h.TypeEntite == typeEntite && h.EntiteId == entiteId)
            .OrderByDescending(h => h.DateModification)
            .ToListAsync();
    }

    /// <summary>
    /// Supprime l'historique plus ancien qu'une date donnée
    /// </summary>
    public async Task<int> SupprimerHistoriqueAvantAsync(DateTime date)
    {
        var anciens = await _context.HistoriquesModifications
            .Where(h => h.DateModification < date)
            .ToListAsync();

        _context.HistoriquesModifications.RemoveRange(anciens);
        await _context.SaveChangesAsync();

        return anciens.Count;
    }

    /// <summary>
    /// Exporte l'historique en JSON
    /// </summary>
    public async Task<string> ExporterAsync(DateTime? dateDebut = null, DateTime? dateFin = null)
    {
        var historique = await GetHistoriqueAsync(
            dateDebut: dateDebut,
            dateFin: dateFin);

        return JsonSerializer.Serialize(historique, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    /// <summary>
    /// Compte le nombre d'entrées dans l'historique
    /// </summary>
    public async Task<int> GetCountAsync(TypeEntite? typeEntite = null)
    {
        var query = _context.HistoriquesModifications.AsQueryable();

        if (typeEntite.HasValue)
            query = query.Where(h => h.TypeEntite == typeEntite.Value);

        return await query.CountAsync();
    }
}
