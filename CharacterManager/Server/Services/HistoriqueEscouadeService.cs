using CharacterManager.Server.Data;
using CharacterManager.Server.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Xml.Linq;

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
                Rang = m.Rang,
                Rarete = m.Rarete.ToString(),
                Puissance = m.Puissance,
                ImageUrl = m.ImageUrlSelected
            }).ToList();

            var commandantData = commandant != null ? new PersonnelHistorique
            {
                Id = commandant.Id,
                Nom = commandant.Nom,
                Niveau = commandant.Niveau,
                Rang = commandant.Rang,
                Rarete = commandant.Rarete.ToString(),
                Puissance = commandant.Puissance,
                ImageUrl = commandant.ImageUrlPreview
            } : null;

            var androidsData = androides.Select(a => new PersonnelHistorique
            {
                Id = a.Id,
                Nom = a.Nom,
                Niveau = a.Niveau,
                Rang = a.Rang,
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

    public async Task<List<HistoriqueEscouade>> GetHistoriqueAsync()
    {
        return await dbContext.HistoriquesEscouade
            .OrderByDescending(h => h.DateEnregistrement)
            .ToListAsync();
    }

    public async Task<List<HistoriqueEscouade>> GetHistoriqueAsync(DateTime dateDebut, DateTime dateFin)
    {
        return await dbContext.HistoriquesEscouade
            .Where(h => h.DateEnregistrement >= dateDebut && h.DateEnregistrement <= dateFin)
            .OrderByDescending(h => h.DateEnregistrement)
            .ToListAsync();
    }

    public async Task<List<HistoriqueEscouade>> GetHistoriqueRecentAsync(int nombre = 50)
    {
        return await dbContext.HistoriquesEscouade
            .OrderByDescending(h => h.DateEnregistrement)
            .Take(nombre)
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
        var historique = await dbContext.HistoriquesEscouade.FindAsync(id);
        if (historique != null)
        {
            dbContext.HistoriquesEscouade.Remove(historique);
            await dbContext.SaveChangesAsync();
        }
    }

    public async Task ViderHistoriqueAsync()
    {
        dbContext.HistoriquesEscouade.RemoveRange(dbContext.HistoriquesEscouade);
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

                var donnees = DeserializerEscouade(historique.DonneesEscouadeJson);

                writer.WriteStartElement("informations");
                writer.WriteElementString("Date", historique.DateEnregistrement.ToString("yyyy-MM-ddTHH:mm:ssZ"));
                writer.WriteElementString("Ligue", (donnees?.Ligue ?? 0).ToString());
                writer.WriteElementString("Score", (donnees?.Score ?? 0).ToString());
                writer.WriteElementString("Puissance", historique.PuissanceTotal.ToString());
                writer.WriteEndElement();

                writer.WriteStartElement("Classement");
                writer.WriteElementString("Nutaku", (donnees?.Nutaku ?? 0).ToString());
                writer.WriteElementString("Top150", (donnees?.Top150 ?? 0).ToString());
                writer.WriteElementString("Pays", (donnees?.Pays ?? 0).ToString());
                writer.WriteEndElement();

                writer.WriteStartElement("Commandant");
                WritePerson(writer, donnees?.Commandant);
                writer.WriteEndElement();

                writer.WriteStartElement("Escouade");
                if (donnees?.Mercenaires != null)
                {
                    foreach (var mercenaire in donnees.Mercenaires.Take(8))
                    {
                        writer.WriteStartElement("Mercenaire");
                        WritePerson(writer, mercenaire);
                        writer.WriteEndElement();
                    }
                }
                writer.WriteEndElement();

                writer.WriteStartElement("Androides");
                if (donnees?.Androides != null)
                {
                    foreach (var androide in donnees.Androides.Take(3))
                    {
                        writer.WriteStartElement("Androide");
                        WritePerson(writer, androide);
                        writer.WriteEndElement();
                    }
                }
                writer.WriteEndElement();

                writer.WriteStartElement("Lucie");
                writer.WriteElementString("Puissance", (donnees?.LuciePuissance ?? 0).ToString());
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

    private static void WritePerson(System.Xml.XmlWriter writer, PersonnelHistorique? p)
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

    /// <summary>
    /// Importe l'historique depuis un XML avec validations métier
    /// </summary>
    public async Task<int> ImporterHistoriqueAsync(Stream stream)
    {
        var errors = new List<string>();
        var personnages = await dbContext.Personnages.AsNoTracking().ToListAsync();
        var personByName = personnages.ToDictionary(p => p.Nom.Trim(), StringComparer.OrdinalIgnoreCase);

        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer);
        buffer.Position = 0;
        var doc = await XDocument.LoadAsync(buffer, LoadOptions.None, CancellationToken.None);
        int count = 0;

        foreach (var enregistrement in doc.Root?.Elements("Enregistrement") ?? Enumerable.Empty<XElement>())
        {
            int errorCountBefore = errors.Count;

            // Parse ID attribute for upsert logic
            string? idAttr = enregistrement.Attribute("ID")?.Value;
            int? recordId = null;
            if (!string.IsNullOrWhiteSpace(idAttr) && int.TryParse(idAttr, out var parsedId))
            {
                recordId = parsedId;
            }

            // Lire depuis le groupe informations
            var informations = enregistrement.Element("informations");
            var date = ParseDate(informations?.Element("Date")?.Value, errors, "Date");
            var ligue = ParseOptionalInt(informations?.Element("Ligue")?.Value, "Ligue", errors, min: 0) ?? 0;
            var puissanceTotale = ParseRequiredInt(informations?.Element("Puissance")?.Value, "Puissance", errors, min: 0);
            var score = ParseOptionalInt(informations?.Element("Score")?.Value, "Score", errors, min: 0) ?? 0;
            int? classement = ParseClassement(enregistrement.Element("Classement"), errors) ?? (int?)score;

            var nutaku = ParseOptionalInt(enregistrement.Element("Classement")?.Element("Nutaku")?.Value, "Classement/Nutaku", errors, min: 0) ?? 0;
            var top150 = ParseOptionalInt(enregistrement.Element("Classement")?.Element("Top150")?.Value ?? enregistrement.Element("Classement")?.Element("Top 150")?.Value, "Classement/Top150", errors, min: 0) ?? 0;
            var pays = ParseOptionalInt(enregistrement.Element("Classement")?.Element("Pays")?.Value, "Classement/Pays", errors, min: 0) ?? 0;
            var luciePuissance = ParseOptionalInt(enregistrement.Element("Lucie")?.Element("Puissance")?.Value, "Lucie", errors, min: 0) ?? 0;

            var donnees = new DonneesEscouadeSerialisees
            {
                Commandant = ParsePerson(enregistrement.Element("Commandant"), "Commandant", TypePersonnage.Commandant, personByName, errors),
                Mercenaires = ParsePersons(enregistrement.Element("Escouade"), "Mercenaire", TypePersonnage.Mercenaire, personByName, errors, maxCount: 8),
                Androides = ParsePersons(enregistrement.Element("Androides"), "Androide", TypePersonnage.Androïde, personByName, errors, maxCount: 3),
                LuciePuissance = luciePuissance,
                Ligue = ligue,
                Nutaku = nutaku,
                Top150 = top150,
                Pays = pays,
                Score = score
            };

            if (errors.Count > errorCountBefore)
            {
                continue;
            }

            // Upsert: check if record exists by ID
            HistoriqueEscouade? historique = null;
            if (recordId.HasValue)
            {
                historique = await dbContext.HistoriquesEscouade.FindAsync(recordId.Value);
            }

            if (historique != null)
            {
                // Update existing record
                historique.DateEnregistrement = (date ?? DateTime.UtcNow).ToUniversalTime();
                historique.PuissanceTotal = puissanceTotale ?? 0;
                historique.Classement = classement ?? ligue;
                historique.DonneesEscouadeJson = JsonSerializer.Serialize(donnees);
                dbContext.HistoriquesEscouade.Update(historique);
            }
            else
            {
                // Insert new record
                historique = new HistoriqueEscouade
                {
                    DateEnregistrement = (date ?? DateTime.UtcNow).ToUniversalTime(),
                    PuissanceTotal = puissanceTotale ?? 0,
                    Classement = classement ?? ligue,
                    DonneesEscouadeJson = JsonSerializer.Serialize(donnees)
                };
                dbContext.HistoriquesEscouade.Add(historique);
            }

            count++;
        }

        if (errors.Count > 0)
        {
            var message = string.Join(" | ", errors.Take(5));
            if (errors.Count > 5)
            {
                message += $" (+{errors.Count - 5} autres erreurs)";
            }

            throw new InvalidOperationException($"Import interrompu: {message}");
        }

        if (count > 0)
        {
            await dbContext.SaveChangesAsync();
        }

        return count;
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
