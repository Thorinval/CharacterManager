using CharacterManager.Server.Data;
using CharacterManager.Server.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Text.Json;
using System.Xml.Linq;

namespace CharacterManager.Server.Services;

public class HistoriqueClassementService(ApplicationDbContext dbContext)
{
    public async Task<List<HistoriqueClassement>> GetHistoriqueAsync()
    {
        return await dbContext.HistoriquesClassement
            .Include(h => h.Classements)
            .Include(h => h.Commandant)
            .Include(h => h.Mercenaires)
            .Include(h => h.Androides)
            .Include(h => h.Pieces)
            .OrderByDescending(h => h.DateEnregistrement)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<HistoriqueClassement>> GetHistoriqueAsync(DateTime dateDebut, DateTime dateFin)
    {
        return await dbContext.HistoriquesClassement
            .Where(h =>
                h.DateEnregistrement.ToDateTime(TimeOnly.MinValue) >= dateDebut.Date &&
                h.DateEnregistrement.ToDateTime(TimeOnly.MinValue) <= dateFin.Date)
            .Include(h => h.Classements)
            .Include(h => h.Commandant)
            .Include(h => h.Mercenaires)
            .Include(h => h.Androides)
            .Include(h => h.Pieces)
            .OrderByDescending(h => h.DateEnregistrement)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<HistoriqueClassement>> GetHistoriqueRecentAsync(int nombre = 50)
    {
        return await dbContext.HistoriquesClassement
            .Include(h => h.Classements)
            .Include(h => h.Commandant)
            .Include(h => h.Mercenaires)
            .Include(h => h.Androides)
            .Include(h => h.Pieces)
            .OrderByDescending(h => h.DateEnregistrement)
            .Take(nombre)
            .AsNoTracking()
            .ToListAsync();
    }

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

    public async Task SupprimerEnregistrementAsync(int id)
    {
        var historique = await dbContext.HistoriquesClassement.FindAsync(id);
        if (historique != null)
        {
            dbContext.HistoriquesClassement.Remove(historique);
            await dbContext.SaveChangesAsync();
        }
    }

    public async Task ViderHistoriqueAsync()
    {
        dbContext.HistoriquesClassement.RemoveRange(dbContext.HistoriquesClassement);
        await dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Met à jour uniquement les champs éditables d'un historique existant
    /// (date, ligue, score et valeurs de classement). Les personnages et pièces historiques ne sont pas modifiés.
    /// </summary>
    public async Task UpdateClassementEditableAsync(int id, DateOnly date, int ligue, int score, int nutaku, int top150, int france)
    {
        var historique = await dbContext.HistoriquesClassement
            .Include(h => h.Classements)
            .FirstOrDefaultAsync(h => h.Id == id);

        if (historique == null)
        {
            throw new InvalidOperationException($"Aucun historique avec l'id {id} n'a été trouvé.");
        }

        historique.DateEnregistrement = date;
        historique.Ligue = ligue;
        historique.Score = score;

        void UpsertClassement(TypeClassement type, int valeur, string nom)
        {
            var existing = historique.Classements.FirstOrDefault(c => c.Type == type);
            if (existing != null)
            {
                existing.Valeur = valeur;
            }
            else
            {
                historique.Classements.Add(new Classement
                {
                    Nom = nom,
                    Type = type,
                    Valeur = valeur
                });
            }
        }

        UpsertClassement(TypeClassement.Nutaku, nutaku, "Classement Nutaku");
        UpsertClassement(TypeClassement.Top150, top150, "Classement Top150");
        UpsertClassement(TypeClassement.France, france, "Classement France");

        await dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Exporte l'historique au format PML avec support pour inventaire et templates
    /// </summary>
    public async Task<byte[]> ExporterHistoriqueXmlAsync()
    {
        var historiques = await GetHistoriqueAsync();
        var settings = new System.Xml.XmlWriterSettings
        {
            Indent = true,
            Encoding = new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
        };

        using var memoryStream = new MemoryStream();
        using (var writer = System.Xml.XmlWriter.Create(memoryStream, settings))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("HistoriqueEscouadePML");
            writer.WriteAttributeString("version", "1.0");
            writer.WriteAttributeString("exportDate", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"));

            // Écrire la section HistoriqueClassements
            writer.WriteStartElement("HistoriqueClassements");

            foreach (var historique in historiques)
            {
                writer.WriteStartElement("Enregistrement");
                writer.WriteAttributeString("ID", historique.Id.ToString());

                writer.WriteStartElement("informations");
                writer.WriteElementString("Date", historique.DateEnregistrement.ToString("yyyy-MM-ddTHH:mm:ssZ"));
                writer.WriteElementString("Ligue", historique.Ligue.ToString());
                writer.WriteElementString("Score", historique.Score.ToString());
                writer.WriteElementString("Puissance", historique.PuissanceTotale.ToString());
                writer.WriteEndElement();

                writer.WriteStartElement("Classement");
                writer.WriteElementString("Nutaku", historique.Classements.First(c => c.Type == TypeClassement.Nutaku).Valeur.ToString());
                writer.WriteElementString("Top150", historique.Classements.First(c => c.Type == TypeClassement.Top150).Valeur.ToString());
                writer.WriteElementString("Pays", historique.Classements.First(c => c.Type == TypeClassement.France).Valeur.ToString());
                writer.WriteEndElement();

                writer.WriteStartElement("Commandant");
                WritePerson(writer, historique.Commandant);
                writer.WriteEndElement();

                writer.WriteStartElement("Escouade");
                if (historique.Mercenaires != null)
                {
                    foreach (var mercenaire in historique.Mercenaires.Take(8))
                    {
                        writer.WriteStartElement("Mercenaire");
                        WritePerson(writer, mercenaire);
                        writer.WriteEndElement();
                    }
                }
                writer.WriteEndElement();

                writer.WriteStartElement("Androides");
                if (historique.Androides != null)
                {
                    foreach (var androide in historique.Androides.Take(3))
                    {
                        writer.WriteStartElement("Androide");
                        WritePerson(writer, androide);
                        writer.WriteEndElement();
                    }
                }
                writer.WriteEndElement();

                writer.WriteStartElement("Lucie");
                writer.WriteElementString("Puissance", historique.Pieces.Sum(p => p.PuissanceLegacy).ToString());
                writer.WriteEndElement();

                writer.WriteEndElement();
            }

            writer.WriteEndElement();

            // Écrire la section inventaire
            writer.WriteStartElement("inventaire");
            var personnages = await dbContext.Personnages.AsNoTracking().ToListAsync();
            foreach (var personnage in personnages)
            {
                writer.WriteStartElement("Personnage");
                writer.WriteElementString("Nom", personnage.Nom);
                writer.WriteElementString("Rarete", personnage.Rarete.ToString());
                writer.WriteElementString("Type", personnage.Type.ToString());
                writer.WriteElementString("Puissance", personnage.Puissance.ToString());
                writer.WriteElementString("PA", personnage.PA.ToString());
                writer.WriteElementString("PV", personnage.PV.ToString());
                writer.WriteElementString("Niveau", personnage.Niveau.ToString());
                writer.WriteElementString("Rang", personnage.Rang.ToString());
                writer.WriteElementString("Role", personnage.Role.ToString());
                writer.WriteElementString("Faction", personnage.Faction.ToString());
                writer.WriteElementString("Selectionne", personnage.Selectionne.ToString());
                writer.WriteElementString("Description", personnage.Description ?? "");
                writer.WriteEndElement();
            }
            writer.WriteEndElement();

            // Écrire la section templates
            writer.WriteStartElement("templates");
            var templates = await dbContext.Templates.AsNoTracking().ToListAsync();
            foreach (var template in templates)
            {
                writer.WriteStartElement("template");
                writer.WriteElementString("Nom", template.Nom);
                writer.WriteElementString("Description", template.Description ?? "");

                var personnageIds = template.GetPersonnageIds();
                foreach (var personnageId in personnageIds)
                {
                    var personnage = await dbContext.Personnages.FirstOrDefaultAsync(p => p.Id == personnageId);
                    if (personnage != null)
                    {
                        writer.WriteStartElement("Personnage");
                        writer.WriteElementString("Nom", personnage.Nom);
                        writer.WriteElementString("Rarete", personnage.Rarete.ToString());
                        writer.WriteElementString("Puissance", personnage.Puissance.ToString());
                        writer.WriteElementString("Niveau", personnage.Niveau.ToString());
                        writer.WriteEndElement();
                    }
                }

                writer.WriteEndElement();
            }
            writer.WriteEndElement();

            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        return memoryStream.ToArray();
    }

    private static void WritePerson(System.Xml.XmlWriter writer, Personnage? p)
    {
        if (p == null)
        {
            writer.WriteElementString("Nom", string.Empty);
            writer.WriteElementString("Niveau", string.Empty);
            writer.WriteElementString("Rang", string.Empty);
            writer.WriteElementString("Puissance", string.Empty);
            return;
        }

        writer.WriteElementString("Nom", p.Nom);
        writer.WriteElementString("Niveau", p.Niveau.ToString());
        writer.WriteElementString("Rang", p.Rang.ToString());
        writer.WriteElementString("Puissance", p.Puissance.ToString());
    }

    private static int? ParseRequiredInt(string? value, string label, List<string> errors, int min, int? max = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"{label} est requis.");
            return null;
        }

        if (!int.TryParse(value, out var number))
        {
            errors.Add($"{label} doit être un nombre.");
            return null;
        }

        if (number < min || (max.HasValue && number > max.Value))
        {
            errors.Add($"{label} doit être compris entre {min} et {max ?? int.MaxValue}.");
            return null;
        }

        return number;
    }

    private static int? ParseOptionalInt(string? value, string label, List<string> errors, int min = 0, int? max = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!int.TryParse(value, out var number))
        {
            errors.Add($"{label} doit être un nombre.");
            return null;
        }

        if (number < min || (max.HasValue && number > max.Value))
        {
            errors.Add($"{label} doit être compris entre {min} et {max ?? int.MaxValue}.");
            return null;
        }

        return number;
    }

    private static DateTime? ParseDate(string? value, List<string> errors, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"{label} est requis.");
            return null;
        }

        if (DateTime.TryParse(value, out var date))
        {
            return date;
        }

        errors.Add($"{label} est invalide.");
        return null;
    }

    private static int? ParseClassement(XElement? classementElement, List<string> errors)
    {
        if (classementElement == null)
        {
            return null;
        }

        var allowedNames = new[] { "Nutaku", "Top150", "Top 150", "Pays" };
        foreach (var name in allowedNames)
        {
            var value = classementElement.Element(name)?.Value;
            if (!string.IsNullOrWhiteSpace(value))
            {
                var parsed = ParseRequiredInt(value, $"Classement/{name}", errors, min: 0);
                if (parsed.HasValue)
                {
                    return parsed;
                }
            }
        }

        var plateformeValue = classementElement.Element("Plateforme")?.Value;
        if (!string.IsNullOrWhiteSpace(plateformeValue))
        {
            var allowedPlateformes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Nutaku", "Top 150", "Pays" };
            if (!allowedPlateformes.Contains(plateformeValue.Trim()))
            {
                errors.Add($"Plateforme '{plateformeValue}' doit être Nutaku, Top 150 ou Pays.");
            }
        }

        var positionValue = classementElement.Element("Position")?.Value;
        if (!string.IsNullOrWhiteSpace(positionValue))
        {
            var parsed = ParseRequiredInt(positionValue, "Position", errors, min: 0);
            if (parsed.HasValue)
            {
                return parsed;
            }
        }

        return null;
    }

    private static PersonnelHistorique? ParsePerson(
        XElement? personElement,
        string label,
        TypePersonnage expectedType,
        Dictionary<string, Personnage> personByName,
        List<string> errors)
    {
        if (personElement == null)
        {
            return null;
        }

        var name = (personElement.Element("Nom")?.Value ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            // Entrée vide : on ignore silencieusement (évite de bloquer l'import sur des lignes vides)
            return null;
        }

        if (!personByName.TryGetValue(name, out var reference))
        {
            errors.Add($"{label} '{name}' n'existe pas dans l'inventaire.");
            return null;
        }

        if (reference.Type != expectedType)
        {
            errors.Add($"{label} '{name}' doit être de type {expectedType}.");
            return null;
        }

        var niveau = ParseRequiredInt(personElement.Element("Niveau")?.Value, $"{label}/Niveau", errors, min: 1, max: 200);
        var rang = ParseOptionalInt(personElement.Element("Rang")?.Value, $"{label}/Rang", errors, min: 0, max: 7) ?? 0;
        var puissance = ParseRequiredInt(personElement.Element("Puissance")?.Value, $"{label}/Puissance", errors, min: 0);

        if (!niveau.HasValue || !puissance.HasValue)
        {
            return null;
        }

        return new PersonnelHistorique
        {
            Nom = name,
            Niveau = niveau.Value,
            Rang = rang,
            Rarete = reference.Rarete.ToString(),
            Puissance = puissance.Value,
            ImageUrl = reference.ImageUrlSelected
        };
    }

    private static List<PersonnelHistorique> ParsePersons(
        XElement? container,
        string elementName,
        TypePersonnage expectedType,
        Dictionary<string, Personnage> personByName,
        List<string> errors,
        int? maxCount = null)
    {
        var list = new List<PersonnelHistorique>();
        if (container == null)
        {
            return list;
        }

        foreach (var element in container.Elements(elementName))
        {
            var person = ParsePerson(element, elementName, expectedType, personByName, errors);
            if (person != null)
            {
                list.Add(person);
                if (maxCount.HasValue && list.Count >= maxCount.Value)
                {
                    break;
                }
            }
        }

        return list;
    }
}
