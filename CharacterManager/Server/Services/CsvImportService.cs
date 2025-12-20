using System.Text;
using CharacterManager.Server.Data;
using CharacterManager.Server.Models;

namespace CharacterManager.Server.Services;

/// <summary>
/// Service d'import CSV pour les personnages (inventaire simple).
/// </summary>
public class CsvImportService(PersonnageService personnageService, ApplicationDbContext context)
{
    private readonly PersonnageService _personnageService = personnageService;
    private readonly ApplicationDbContext _context = context;

    /// <summary>
    /// Importe un flux CSV et met à jour ou ajoute les personnages.
    /// </summary>
    public async Task<ImportResult> ImportCsvAsync(Stream csvStream)
    {
        var result = new ImportResult();
        var errors = new List<string>();

        using var reader = new StreamReader(csvStream, Encoding.UTF8, leaveOpen: false);
        string? header = await reader.ReadLineAsync();

        if (header == null)
        {
            result.Error = "Fichier CSV vide";
            return result;
        }

        // Aucune donnée après l'en-tête
        if (reader.EndOfStream)
        {
            result.Error = "Aucune donnée à importer";
            return result;
        }

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var parts = line.Split(';');
            if (parts.Length < 12)
            {
                errors.Add("Ligne invalide: colonnes insuffisantes");
                continue;
            }

            try
            {
                var personnage = BuildPersonnage(parts);
                if (personnage == null)
                    continue;

                ImportOrUpdate(personnage);
                result.SuccessCount++;
            }
            catch (Exception ex)
            {
                errors.Add($"Erreur lors de l'import d'une ligne: {ex.Message}");
            }
        }

        result.Errors = errors;
        result.IsSuccess = result.SuccessCount > 0 && string.IsNullOrEmpty(result.Error);
        return result;
    }

    private Personnage? BuildPersonnage(string[] parts)
    {
        // parts: 0 Personnage,1 Rareté,2 Type,3 Puissance,4 PA,5 PV,6 Action,7 Role,8 Niveau,9 Rang,10 Selection,11 Faction
        var nom = parts[0]?.Trim();
        if (string.IsNullOrWhiteSpace(nom))
            return null;

        var personnage = _context.Personnages
            .FirstOrDefault(p => p.Nom.Equals(nom, StringComparison.OrdinalIgnoreCase))
            ?? new Personnage { Nom = nom };

        personnage.Rarete = ParseRarete(parts[1]);
        personnage.Type = ParseType(parts[2]);
        personnage.Puissance = ParseInt(parts[3]);
        personnage.PA = ParseInt(parts[4]);
        personnage.PV = ParseInt(parts[5]);
        personnage.TypeAttaque = ParseTypeAttaque(parts[6]);
        personnage.Role = ParseRole(parts[7]);
        personnage.Niveau = ParseInt(parts[8], defaultValue: 1);
        personnage.Rang = ParseInt(parts[9], defaultValue: 1);
        personnage.Selectionne = ParseBool(parts[10]);
        personnage.Faction = ParseFaction(parts[11]);

        // Description par défaut si absente
        personnage.Description = personnage.Description ?? $"Personnage {personnage.Nom}";

        return personnage;
    }

    private void ImportOrUpdate(Personnage personnage)
    {
        var existing = _context.Personnages
            .FirstOrDefault(p => p.Nom.Equals(personnage.Nom, StringComparison.OrdinalIgnoreCase));

        if (existing != null)
        {
            existing.Rarete = personnage.Rarete;
            existing.Type = personnage.Type;
            existing.Puissance = personnage.Puissance;
            existing.PA = personnage.PA;
            existing.PV = personnage.PV;
            existing.Niveau = personnage.Niveau;
            existing.Rang = personnage.Rang;
            existing.Role = personnage.Role;
            existing.Faction = personnage.Faction;
            existing.Selectionne = personnage.Selectionne;
            existing.TypeAttaque = personnage.TypeAttaque;
            existing.Description = personnage.Description;
            _context.Personnages.Update(existing);
        }
        else
        {
            _context.Personnages.Add(personnage);
        }

        _context.SaveChanges();
    }

    private static int ParseInt(string? value, int defaultValue = 0)
    {
        return int.TryParse(value, out var parsed) ? parsed : defaultValue;
    }

    private static bool ParseBool(string? value)
    {
        return string.Equals(value?.Trim(), "Oui", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value?.Trim(), "Yes", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value?.Trim(), "True", StringComparison.OrdinalIgnoreCase);
    }

    private static Rarete ParseRarete(string? value)
    {
        return value?.Trim().ToUpperInvariant() switch
        {
            "SSR" => Rarete.SSR,
            "SR" => Rarete.SR,
            "R" => Rarete.R,
            _ => Rarete.R
        };
    }

    private static TypePersonnage ParseType(string? value)
    {
        var v = value?.Trim().ToLowerInvariant() ?? string.Empty;
        return v switch
        {
            "mercenaire" => TypePersonnage.Mercenaire,
            "androïde" or "androide" => TypePersonnage.Androïde,
            "commandant" => TypePersonnage.Commandant,
            _ => TypePersonnage.Mercenaire
        };
    }

    private static Role ParseRole(string? value)
    {
        var v = value?.Trim().ToLowerInvariant() ?? string.Empty;
        return v switch
        {
            "sentinelle" => Role.Sentinelle,
            "combattante" => Role.Combattante,
            "androïde" or "androide" => Role.Androide,
            "commandant" => Role.Commandant,
            _ => Role.Combattante
        };
    }

    private static TypeAttaque ParseTypeAttaque(string? value)
    {
        var v = value?.Trim().ToLowerInvariant() ?? string.Empty;
        return v switch
        {
            "mêlée" or "melee" => TypeAttaque.Mêlée,
            "distance" => TypeAttaque.Distance,
            "androïde" or "androide" => TypeAttaque.Androïde,
            _ => TypeAttaque.Inconnu
        };
    }

    private static Faction ParseFaction(string? value)
    {
        var v = value?.Trim().ToLowerInvariant() ?? string.Empty;
        return v switch
        {
            "syndicat" => Faction.Syndicat,
            "pacificateurs" => Faction.Pacificateurs,
            "hommes libres" or "hommeslibres" => Faction.HommesLibres,
            _ => Faction.Syndicat
        };
    }
}
