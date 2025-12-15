using CharacterManager.Server.Data;
using CharacterManager.Server.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CharacterManager.Server.Services;

public class HistoriqueEscouadeService(ApplicationDbContext dbContext)
{
    /// <summary>
    /// Enregistre l'état actuel de l'escouade dans l'historique
    /// </summary>
    public async Task EnregistrerEscouadeAsync(
        List<Personnage> mercenaires,
        Personnage? commandant,
        List<Personnage> androides,
        int? classement = null)
    {
        try
        {
            var mercenairesData = mercenaires.Select(m => new PersonnelHistorique
            {
                Id = m.Id,
                Nom = m.Nom,
                Niveau = m.Niveau,
                Rarete = m.Rarete.ToString(),
                Puissance = m.Puissance,
                ImageUrl = m.ImageUrlSelected
            }).ToList();

            var commandantData = commandant != null ? new PersonnelHistorique
            {
                Id = commandant.Id,
                Nom = commandant.Nom,
                Niveau = commandant.Niveau,
                Rarete = commandant.Rarete.ToString(),
                Puissance = commandant.Puissance,
                ImageUrl = commandant.ImageUrlPreview
            } : null;

            var androidsData = androides.Select(a => new PersonnelHistorique
            {
                Id = a.Id,
                Nom = a.Nom,
                Niveau = a.Niveau,
                Rarete = a.Rarete.ToString(),
                Puissance = a.Puissance,
                ImageUrl = a.ImageUrlSelected
            }).ToList();

            var donneesEscouade = new DonneesEscouadeSerialisees
            {
                Mercenaires = mercenairesData,
                Commandant = commandantData,
                Androides = androidsData
            };

            var historique = new HistoriqueEscouade
            {
                DateEnregistrement = DateTime.UtcNow,
                PuissanceTotal = mercenairesData.Sum(m => m.Puissance) 
                    + (commandantData?.Puissance ?? 0) 
                    + androidsData.Sum(a => a.Puissance),
                Classement = classement,
                DonneesEscouadeJson = JsonSerializer.Serialize(donneesEscouade)
            };

            dbContext.HistoriquesEscouade.Add(historique);
            await dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Erreur lors de l'enregistrement de l'escouade: {ex.Message}");
        }
    }

    /// <summary>
    /// Récupère l'historique complet des escouades
    /// </summary>
    public async Task<List<HistoriqueEscouade>> GetHistoriqueAsync()
    {
        return await dbContext.HistoriquesEscouade
            .OrderByDescending(h => h.DateEnregistrement)
            .ToListAsync();
    }

    /// <summary>
    /// Récupère l'historique filtré par plage de dates
    /// </summary>
    public async Task<List<HistoriqueEscouade>> GetHistoriqueAsync(DateTime dateDebut, DateTime dateFin)
    {
        return await dbContext.HistoriquesEscouade
            .Where(h => h.DateEnregistrement >= dateDebut && h.DateEnregistrement <= dateFin)
            .OrderByDescending(h => h.DateEnregistrement)
            .ToListAsync();
    }

    /// <summary>
    /// Récupère l'historique limité aux N derniers enregistrements
    /// </summary>
    public async Task<List<HistoriqueEscouade>> GetHistoriqueRecentAsync(int nombre = 50)
    {
        return await dbContext.HistoriquesEscouade
            .OrderByDescending(h => h.DateEnregistrement)
            .Take(nombre)
            .ToListAsync();
    }

    /// <summary>
    /// Déserialise les données de l'escouade depuis JSON
    /// </summary>
    public DonneesEscouadeSerialisees? DeserializerEscouade(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<DonneesEscouadeSerialisees>(json);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Supprime un enregistrement d'historique
    /// </summary>
    public async Task SupprimerEnregistrementAsync(int id)
    {
        var historique = await dbContext.HistoriquesEscouade.FindAsync(id);
        if (historique != null)
        {
            dbContext.HistoriquesEscouade.Remove(historique);
            await dbContext.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Vide l'historique complet
    /// </summary>
    public async Task ViderHistoriqueAsync()
    {
        dbContext.HistoriquesEscouade.RemoveRange(dbContext.HistoriquesEscouade);
        await dbContext.SaveChangesAsync();
    }
}
