using CharacterManager.Server.Models;
using CharacterManager.Server.Data;
using CharacterManager.Server.Constants;
using Microsoft.EntityFrameworkCore;
using System.Xml.Linq;
using System.Globalization;

namespace CharacterManager.Server.Services;

/// <summary>
/// Classe de base pour les services PML (import/export)
/// Contient les méthodes communes de parsing et d'accès aux données
/// </summary>
public abstract class PmlServiceBase(ApplicationDbContext context)
{
    protected readonly ApplicationDbContext _context = context;

    #region Helpers de parsing

    protected static string NormalizeUpper(string? value) => (value ?? string.Empty).Trim().ToUpper();

    protected static Rarete ParseRarete(string? value)
    {
        return value switch
        {
            PersonnageConstants.Rarities.SSR => Rarete.SSR,
            PersonnageConstants.Rarities.SR => Rarete.SR,
            PersonnageConstants.Rarities.R => Rarete.R,
            _ => Rarete.R
        };
    }

    protected static bool ParseBool(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.Trim();

        return trimmed.Equals("true", StringComparison.OrdinalIgnoreCase)
            || trimmed.Equals("1", StringComparison.OrdinalIgnoreCase)
            || trimmed.Equals("yes", StringComparison.OrdinalIgnoreCase)
            || trimmed.Equals("oui", StringComparison.OrdinalIgnoreCase);
    }

    protected static TypePersonnage ParseType(string? value)
    {
        return value switch
        {
            PersonnageConstants.Types.Mercenaire => TypePersonnage.Mercenaire,
            ImportExportConstants.XmlElements.Androïde or PersonnageConstants.Types.Androide => TypePersonnage.Androide,
            PersonnageConstants.Types.Commandant => TypePersonnage.Commandant,
            _ => TypePersonnage.Mercenaire
        };
    }

    protected static Role ParseRole(string? value)
    {
        return value switch
        {
            PersonnageConstants.Roles.Sentinelle => Role.Sentinelle,
            PersonnageConstants.Roles.Combattante => Role.Combattante,
            PersonnageConstants.Types.Androide => Role.Androide,
            PersonnageConstants.Types.Commandant => Role.Commandant,
            _ => Role.Combattante
        };
    }

    protected static Faction ParseFaction(string? value)
    {
        return value switch
        {
            PersonnageConstants.Factions.Syndicat => Faction.Syndicat,
            PersonnageConstants.XmlElements.Faction => Faction.Pacificateurs,
            PersonnageConstants.Factions.HommesLibres => Faction.HommesLibres,
            _ => Faction.Syndicat
        };
    }

    protected static TypeAttaque ParseTypeAttaque(string? value)
    {
        return value switch
        {
            PersonnageConstants.AttackTypes.MeleeAccent or PersonnageConstants.AttackTypes.Melee => TypeAttaque.Melee,
            "Distance" => TypeAttaque.Distance,
            ImportExportConstants.XmlElements.Androïde or PersonnageConstants.Types.Androide => TypeAttaque.Androide,
            _ => TypeAttaque.Inconnu
        };
    }

    protected static List<Capacite> ParseCapacites(XElement element)
    {
        var capacitesElement = element.Element(PersonnageConstants.XmlElements.Capacites);
        if (capacitesElement == null)
        {
            return [];
        }

        var capacites = new List<Capacite>();

        foreach (var capaciteElement in capacitesElement.Elements(PersonnageConstants.XmlElements.Capacite))
        {
            var nom = capaciteElement.Element(ImportExportConstants.XmlElements.Nom)?.Value ?? capaciteElement.Value;
            if (string.IsNullOrWhiteSpace(nom))
            {
                continue;
            }

            var description = capaciteElement.Element(ImportExportConstants.XmlElements.Description)?.Value ?? string.Empty;
            var icon = capaciteElement.Element(PersonnageConstants.XmlElements.Icon)?.Value ?? string.Empty;

            capacites.Add(new Capacite
            {
                Nom = nom.Trim(),
                Description = description,
                Icon = icon
            });
        }

        return capacites;
    }

    protected Personnage? ParsePersonnageFromXml(XElement element)
    {
        var nom = element.Element(ImportExportConstants.XmlElements.Nom)?.Value;
        if (string.IsNullOrWhiteSpace(nom))
            return null;

        var id = int.TryParse(element.Attribute(ImportExportConstants.XmlElements.Id)?.Value, out var parsedId) ? parsedId : 0;

        var personnage = new Personnage
        {
            Id = id,
            Nom = nom.Trim().ToUpperInvariant(), // Normaliser en majuscules
            Rarete = ParseRarete(element.Element(PersonnageConstants.XmlElements.Rarete)?.Value),
            Type = ParseType(element.Element(PersonnageConstants.XmlElements.Type)?.Value),
            Puissance = int.TryParse(element.Element(PersonnageConstants.XmlElements.Puissance)?.Value, out var p) ? p : 0,
            PA = int.TryParse(element.Element(PersonnageConstants.XmlElements.PA)?.Value, out var pa) ? pa : 0,
            PV = int.TryParse(element.Element(PersonnageConstants.XmlElements.PV)?.Value, out var pv) ? pv : 0,
            Niveau = int.TryParse(element.Element(PersonnageConstants.XmlElements.Niveau)?.Value, out var n) ? n : 1,
            Rang = int.TryParse(element.Element(PersonnageConstants.XmlElements.Rang)?.Value, out var r) ? r : 1,
            Role = ParseRole(element.Element(PersonnageConstants.XmlElements.Role)?.Value),
            Faction = ParseFaction(element.Element(PersonnageConstants.XmlElements.Faction)?.Value),
            TypeAttaque = ParseTypeAttaque(element.Element(PersonnageConstants.XmlElements.TypeAttaque)?.Value),
            Selectionne = ParseBool(element.Element(PersonnageConstants.XmlElements.Selectionne)?.Value),
            HasRelation = ParseBool(element.Element(PersonnageConstants.XmlElements.HasRelation)?.Value),
            NivRelation = int.TryParse(element.Element(PersonnageConstants.XmlElements.NivRelation)?.Value, out var nivRel) ? nivRel : 0,
            Capacites = ParseCapacites(element)
        };

        return personnage;
    }

    protected static PersonnageHistorique ConvertToPersonnageHistorique(Personnage personnage)
    {
        return new PersonnageHistorique
        {
            Nom = personnage.Nom,
            Rarete = personnage.Rarete,
            Type = personnage.Type,
            Puissance = personnage.Puissance,
            PA = personnage.PA,
            PV = personnage.PV,
            Niveau = personnage.Niveau,
            Rang = personnage.Rang,
            Role = personnage.Role,
            Faction = personnage.Faction,
            TypeAttaque = personnage.TypeAttaque,
            Selectionne = personnage.Selectionne,
            HasRelation = personnage.HasRelation,
            NivRelation = personnage.NivRelation,
            Capacites = personnage.Capacites
                .Select(c => new Capacite
                {
                    Nom = c.Nom,
                    Description = c.Description,
                    Icon = c.Icon
                })
                .ToList(),
            IdOrigine = personnage.Id
        };
    }

    protected static PieceHistorique ParsePieceHistorique(XElement pieceElement, string nom)
    {
        var pieceHistorique = new PieceHistorique
        {
            Nom = nom,
            Niveau = int.TryParse(pieceElement.Element(PersonnageConstants.XmlElements.Niveau)?.Value, out var niveau) ? niveau : 1,
            Selectionnee = ParseBool(pieceElement.Element(PersonnageConstants.XmlElements.Selectionne)?.Value),
            IdOrigine = int.TryParse(pieceElement.Attribute(ImportExportConstants.XmlElements.Id)?.Value, out var parsedId) ? parsedId : 0
        };

        if (int.TryParse(pieceElement.Element(LucieHouseConstants.XmlElements.PuissanceTactique)?.Value, out var pTact))
        {
            pieceHistorique.AspectsTactiques.Puissance = pTact;
        }

        if (int.TryParse(pieceElement.Element(PersonnageConstants.XmlElements.Puissance)?.Value, out var pStrat))
        {
            pieceHistorique.AspectsStrategiques.Puissance = pStrat;
        }

        return pieceHistorique;
    }

    protected Piece? ParseLuciePiece(XElement pieceElement)
    {
        var nom = pieceElement.Element(ImportExportConstants.XmlElements.Nom)?.Value;
        if (string.IsNullOrWhiteSpace(nom))
            return null;

        var piece = new Piece
        {
            Nom = nom,
            Niveau = int.TryParse(pieceElement.Element(PersonnageConstants.XmlElements.Niveau)?.Value, out var niveau) ? niveau : 1,
            Selectionnee = ParseBool(pieceElement.Element(PersonnageConstants.XmlElements.Selectionne)?.Value)
        };

        ParseLuciePieceBonus(pieceElement, piece);
        ParseLuciePiecePuissance(pieceElement, piece);

        return piece;
    }

    protected static void ParseLuciePieceBonus(XElement pieceElement, Piece piece)
    {
        // Parser les bonus tactiques
        var bonusTactiquesElement = pieceElement.Element(LucieHouseConstants.XmlElements.BonusTactiques);
        if (bonusTactiquesElement != null)
        {
            piece.AspectsTactiques.Bonus = [.. bonusTactiquesElement.Elements(LucieHouseConstants.XmlElements.Bonus)
                .Select(b => b.Value)
                .Where(b => !string.IsNullOrWhiteSpace(b))];
            piece.AspectsTactiques.Puissance = piece.AspectsTactiques.Bonus.Count;
        }

        // Parser les bonus stratégiques
        var bonusStrategiquesElement = pieceElement.Element(LucieHouseConstants.XmlElements.BonusStrategiques);
        if (bonusStrategiquesElement != null)
        {
            piece.AspectsStrategiques.Bonus = bonusStrategiquesElement.Elements(LucieHouseConstants.XmlElements.Bonus)
                .Select(b => b.Value)
                .Where(b => !string.IsNullOrWhiteSpace(b))
                .ToList();
            piece.AspectsStrategiques.Puissance = piece.AspectsStrategiques.Bonus.Count;
        }
    }

    protected static void ParseLuciePiecePuissance(XElement pieceElement, Piece piece)
    {
        // Puissance tactiques et stratégiques (nouveau format). Fallback: ancienne balise "Puissance" alimente les tactiques.
        if (int.TryParse(pieceElement.Element(LucieHouseConstants.XmlElements.PuissanceTactique)?.Value, out var pTact))
        {
            piece.AspectsTactiques.Puissance = pTact;
        }
        else if (int.TryParse(pieceElement.Element(PersonnageConstants.XmlElements.Puissance)?.Value, out var pLegacy))
        {
            piece.AspectsTactiques.Puissance = pLegacy;
        }

        if (int.TryParse(pieceElement.Element(PersonnageConstants.XmlElements.Puissance)?.Value, out var pStrat))
        {
            piece.AspectsStrategiques.Puissance = pStrat;
        }
    }

    #endregion

    #region Méthodes communes d'accès aux données

    protected List<Capacite> ResolveCapacites(IEnumerable<Capacite> importedCapacites)
    {
        var resolved = new List<Capacite>();

        foreach (var capacite in importedCapacites)
        {
            if (string.IsNullOrWhiteSpace(capacite.Nom))
            {
                continue;
            }

            var existing = FindExistingCapacite(capacite);
            if (existing != null)
            {
                UpdateExistingCapacite(existing, capacite);
                resolved.Add(existing);
            }
            else
            {
                var newCapacite = CreateNewCapacite(capacite);
                _context.Capacites.Add(newCapacite);
                resolved.Add(newCapacite);
            }
        }

        return resolved;
    }

    protected Capacite? FindExistingCapacite(Capacite capacite)
    {
        if (capacite.Id > 0)
        {
            return _context.Capacites.FirstOrDefault(c => c.Id == capacite.Id);
        }

        // EF Core ne peut pas traduire StringComparison.OrdinalIgnoreCase en SQL
        // Utiliser ToUpper() qui est supporté par SQLite
        var normalizedName = capacite.Nom.ToUpper();
        return _context.Capacites.FirstOrDefault(c => c.Nom.ToUpper() == normalizedName);
    }

    protected static void UpdateExistingCapacite(Capacite existing, Capacite capacite)
    {
        if (!string.IsNullOrWhiteSpace(capacite.Description) && string.IsNullOrWhiteSpace(existing.Description))
        {
            existing.Description = capacite.Description;
        }

        if (!string.IsNullOrWhiteSpace(capacite.Icon) && string.IsNullOrWhiteSpace(existing.Icon))
        {
            existing.Icon = capacite.Icon;
        }
    }

    protected static Capacite CreateNewCapacite(Capacite capacite)
    {
        return new Capacite
        {
            Nom = capacite.Nom.Trim(),
            Description = capacite.Description ?? string.Empty,
            Icon = capacite.Icon ?? string.Empty
        };
    }

    #endregion

    #region Méthodes de gestion des dates d'import/export

    public async Task<string?> GetLastImportedFileName()
    {
        var settings = await _context.AppSettings.OrderBy(x => x.Id).FirstOrDefaultAsync();
        return settings?.LastImportedFileName;
    }

    public async Task<DateTime?> GetLastImportedDateAsync()
    {
        var settings = await _context.AppSettings.OrderBy(x => x.Id).FirstOrDefaultAsync();
        return settings?.LastImportedDate;
    }

    public async Task<DateTime?> GetLastExportDate()
    {
        var settings = await _context.AppSettings.OrderBy(x => x.Id).FirstOrDefaultAsync();
        return settings?.LastExportDate;
    }

    protected async Task SaveLastImportedFileName(string fileName)
    {
        var settings = await _context.AppSettings.OrderBy(x => x.Id).FirstOrDefaultAsync();
        if (settings == null)
        {
            settings = new AppSettings();
            _context.AppSettings.Add(settings);
        }

        settings.LastImportedFileName = fileName;
        settings.LastImportedDate = DateTime.Now;
        await _context.SaveChangesAsync();
    }

    protected async Task SaveLastExportDate()
    {
        var settings = await _context.AppSettings.OrderBy(x => x.Id).FirstOrDefaultAsync();
        if (settings == null)
        {
            settings = new AppSettings();
            _context.AppSettings.Add(settings);
        }

        settings.LastExportDate = DateTime.Now;
        await _context.SaveChangesAsync();
    }

    #endregion
}





