using CharacterManager.Server.Models;
using CharacterManager.Server.Data;
using CharacterManager.Server.Constants;
using Microsoft.EntityFrameworkCore;
using System.Xml.Linq;
using System.Text;
using System.Text.Json;

namespace CharacterManager.Server.Services;

/// <summary>
/// Service pour importer/exporter les données au format PML (XML personnalisé)
/// Extension .pml pour les fichiers d'import/export
/// Supporte les sections : HistoriqueClassements, inventaire, template
/// </summary>
public class PmlImportService(PersonnageService personnageService, ApplicationDbContext context)
{
    private readonly PersonnageService _personnageService = personnageService;
    private readonly ApplicationDbContext _context = context;

    /// <summary>
    /// Importe les données du format PML (inventaire, templates, etc.)
    /// </summary>
    public async Task<ImportResult> ImportPmlAsync(Stream pmlStream, string fileName = "")
    {
        var result = new ImportResult();
        var errors = new List<string>();

        try
        {
            using var buffer = new MemoryStream();
            await pmlStream.CopyToAsync(buffer);
            buffer.Position = 0;
            var doc = await XDocument.LoadAsync(buffer, LoadOptions.None, CancellationToken.None);

            if (doc.Root == null)
            {
                result.Error = "Le fichier PML est vide ou invalide";
                return result;
            }

            // Traiter la section inventaire
            var inventaireElements = doc.Root.Elements("inventaire");
            if (inventaireElements.Any())
            {
                result.SuccessCount += await ImportInventaireAsync(inventaireElements, errors);
            }

            // Traiter la section templates
            var templateElements = doc.Root.Elements("template");
            if (templateElements.Any())
            {
                result.SuccessCount += await ImportTemplatesAsync(templateElements, errors);
            }

            result.IsSuccess = result.SuccessCount > 0 || errors.Count == 0;
            result.Errors = errors;

            // Enregistrer le nom du fichier importé
            if (!string.IsNullOrEmpty(fileName))
            {
                await SaveLastImportedFileName(fileName);
            }
        }
        catch (Exception ex)
        {
            result.Error = $"Erreur lors de la lecture du fichier PML: {ex.Message}";
            result.IsSuccess = false;
        }

        return result;
    }

    /// <summary>
    /// Importe les données depuis la section inventaire
    /// </summary>
    private async Task<int> ImportInventaireAsync(IEnumerable<XElement> inventaireElements, List<string> errors)
    {
        int importedCount = 0;
        var personnages = await _context.Personnages.AsNoTracking().ToListAsync();

        foreach (var inventaire in inventaireElements)
        {
            try
            {
                var personnage = ParsePersonnageFromXml(inventaire);
                if (personnage != null)
                {
                    ImportOrUpdatePersonnage(personnage);
                    importedCount++;
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Erreur lors de l'import de personnage (inventaire): {ex.Message}");
            }
        }

        return importedCount;
    }

    /// <summary>
    /// Importe les données depuis la section templates
    /// </summary>
    private async Task<int> ImportTemplatesAsync(IEnumerable<XElement> templateElements, List<string> errors)
    {
        int importedCount = 0;

        foreach (var template in templateElements)
        {
            try
            {
                var templateName = template.Element("Nom")?.Value;
                var description = template.Element("Description")?.Value;

                if (string.IsNullOrWhiteSpace(templateName))
                {
                    errors.Add("Un template doit avoir un nom");
                    continue;
                }

                // Chercher le template existant ou en créer un nouveau
                var existingTemplate = await _context.Templates
                    .FirstOrDefaultAsync(t => t.Nom.Equals(templateName, StringComparison.OrdinalIgnoreCase));

                if (existingTemplate == null)
                {
                    existingTemplate = new Template { Nom = templateName };
                    _context.Templates.Add(existingTemplate);
                    await _context.SaveChangesAsync();
                }

                // Mettre à jour la description si fournie
                if (!string.IsNullOrWhiteSpace(description))
                {
                    existingTemplate.Description = description;
                }

                // Importer les personnages du template
                var personnageIds = new List<int>();
                var personnagesElements = template.Elements("Personnage");
                foreach (var personElement in personnagesElements)
                {
                    try
                    {
                        var nom = personElement.Element("Nom")?.Value;
                        if (string.IsNullOrWhiteSpace(nom))
                            continue;

                        var personnage = await _context.Personnages
                            .FirstOrDefaultAsync(p => p.Nom.Equals(nom, StringComparison.OrdinalIgnoreCase));

                        if (personnage != null && !personnageIds.Contains(personnage.Id))
                        {
                            personnageIds.Add(personnage.Id);
                            importedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Erreur lors de l'import du personnage au template '{templateName}': {ex.Message}");
                    }
                }

                // Mettre à jour les IDs de personnages du template
                if (personnageIds.Count > 0)
                {
                    existingTemplate.SetPersonnageIds(personnageIds);
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                errors.Add($"Erreur lors de l'import du template: {ex.Message}");
            }
        }

        return importedCount;
    }

    /// <summary>
    /// Parse un personnage depuis un élément XML
    /// </summary>
    private Personnage? ParsePersonnageFromXml(XElement element)
    {
        var nom = element.Element("Nom")?.Value;
        if (string.IsNullOrWhiteSpace(nom))
            return null;

        var personnage = new Personnage
        {
            Nom = nom.Trim(),
            Rarete = ParseRarete(element.Element("Rarete")?.Value),
            Type = ParseType(element.Element("Type")?.Value),
            Puissance = int.TryParse(element.Element("Puissance")?.Value, out var p) ? p : 0,
            PA = int.TryParse(element.Element("PA")?.Value, out var pa) ? pa : 0,
            PV = int.TryParse(element.Element("PV")?.Value, out var pv) ? pv : 0,
            Niveau = int.TryParse(element.Element("Niveau")?.Value, out var n) ? n : 1,
            Rang = int.TryParse(element.Element("Rang")?.Value, out var r) ? r : 1,
            Role = ParseRole(element.Element("Role")?.Value),
            Faction = ParseFaction(element.Element("Faction")?.Value),
            Selectionne = bool.TryParse(element.Element("Selectionne")?.Value, out var sel) && sel,
            Description = element.Element("Description")?.Value ?? $"Personnage {nom}",
        };

        // Déterminer le type d'attaque
        personnage.TypeAttaque = personnage.Type == TypePersonnage.Androïde
            ? TypeAttaque.Androïde
            : (personnage.Type == TypePersonnage.Mercenaire ? TypeAttaque.Inconnu : TypeAttaque.Inconnu);

        // Gérer les URLs des images
        string nomLower = personnage.Nom.ToLower();
        personnage.ImageUrlDetail = EnsureImageOrDefault($"{AppConstants.Paths.ImagesPersonnages}/{nomLower}{AppConstants.FileExtensions.Png}");
        personnage.ImageUrlPreview = EnsureImageOrDefault($"{AppConstants.Paths.ImagesPersonnages}/{nomLower}{AppConstants.ImageSuffixes.SmallPortrait}{AppConstants.FileExtensions.Png}");
        personnage.ImageUrlSelected = EnsureImageOrDefault($"{AppConstants.Paths.ImagesPersonnages}/{nomLower}{AppConstants.ImageSuffixes.SmallSelect}{AppConstants.FileExtensions.Png}");

        return personnage;
    }

    /// <summary>
    /// Exporte les données d'inventaire au format PML
    /// </summary>
    public Task<byte[]> ExporterInventairePmlAsync(IEnumerable<Personnage> personnages)
    {
        var settings = new System.Xml.XmlWriterSettings
        {
            Indent = true,
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
        };

        using var memoryStream = new MemoryStream();
        using (var writer = System.Xml.XmlWriter.Create(memoryStream, settings))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("InventairePML");
            writer.WriteAttributeString("version", "1.0");
            writer.WriteAttributeString("exportDate", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"));

            writer.WriteStartElement("inventaire");

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
            writer.WriteEndDocument();
        }

        return Task.FromResult(memoryStream.ToArray());
    }

    /// <summary>
    /// Exporte les templates au format PML
    /// </summary>
    public Task<byte[]> ExporterTemplatesPmlAsync(IEnumerable<Template> templates)
    {
        var settings = new System.Xml.XmlWriterSettings
        {
            Indent = true,
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
        };

        using var memoryStream = new MemoryStream();
        using (var writer = System.Xml.XmlWriter.Create(memoryStream, settings))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("TemplatesPML");
            writer.WriteAttributeString("version", "1.0");
            writer.WriteAttributeString("exportDate", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"));

            foreach (var template in templates)
            {
                writer.WriteStartElement("template");
                writer.WriteElementString("Nom", template.Nom);
                writer.WriteElementString("Description", template.Description ?? "");

                // Récupérer les personnages du template via les IDs stockés
                var personnageIds = template.GetPersonnageIds();
                foreach (var personnageId in personnageIds)
                {
                    var personnage = _context.Personnages.FirstOrDefault(p => p.Id == personnageId);
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
            writer.WriteEndDocument();
        }

        return Task.FromResult(memoryStream.ToArray());
    }

    private void ImportOrUpdatePersonnage(Personnage nouveauPersonnage)
    {
        var existing = _personnageService.GetAll()
            .FirstOrDefault(p => p.Nom.Equals(nouveauPersonnage.Nom, StringComparison.OrdinalIgnoreCase));

        if (existing != null)
        {
            // Mettre à jour le personnage existant
            existing.Rarete = nouveauPersonnage.Rarete;
            existing.Type = nouveauPersonnage.Type;
            existing.Puissance = nouveauPersonnage.Puissance;
            existing.PA = nouveauPersonnage.PA;
            existing.PV = nouveauPersonnage.PV;
            existing.Niveau = nouveauPersonnage.Niveau;
            existing.Rang = nouveauPersonnage.Rang;
            existing.Role = nouveauPersonnage.Role;
            existing.Faction = nouveauPersonnage.Faction;
            existing.Selectionne = nouveauPersonnage.Selectionne;
            existing.TypeAttaque = nouveauPersonnage.TypeAttaque;
            existing.Description = nouveauPersonnage.Description;

            _context.Personnages.Update(existing);
        }
        else
        {
            // Ajouter le nouveau personnage
            _context.Personnages.Add(nouveauPersonnage);
        }

        _context.SaveChanges();
    }

    private static Rarete ParseRarete(string? value)
    {
        return value switch
        {
            "SSR" => Rarete.SSR,
            "SR" => Rarete.SR,
            "R" => Rarete.R,
            _ => Rarete.R
        };
    }

    private static TypePersonnage ParseType(string? value)
    {
        return value switch
        {
            "Mercenaire" => TypePersonnage.Mercenaire,
            "Androïde" or "Androide" => TypePersonnage.Androïde,
            "Commandant" => TypePersonnage.Commandant,
            _ => TypePersonnage.Mercenaire
        };
    }

    private static Role ParseRole(string? value)
    {
        return value switch
        {
            "Sentinelle" => Role.Sentinelle,
            "Combattante" => Role.Combattante,
            "Androide" => Role.Androide,
            "Commandant" => Role.Commandant,
            _ => Role.Combattante
        };
    }

    private static Faction ParseFaction(string? value)
    {
        return value switch
        {
            "Syndicat" => Faction.Syndicat,
            "Pacificateurs" => Faction.Pacificateurs,
            "HommesLibres" => Faction.HommesLibres,
            _ => Faction.Syndicat
        };
    }

    private static string EnsureImageOrDefault(string relativePath)
    {
        var fullPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            AppConstants.Paths.WwwRoot,
            relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)
        );
        return File.Exists(fullPath) ? relativePath : AppConstants.Paths.DefaultPortrait;
    }

    public async Task<string?> GetLastImportedFileName()
    {
        var settings = await _context.AppSettings.FirstOrDefaultAsync();
        return settings?.LastImportedFileName;
    }

    private async Task SaveLastImportedFileName(string fileName)
    {
        var settings = await _context.AppSettings.FirstOrDefaultAsync();
        if (settings == null)
        {
            settings = new AppSettings();
            _context.AppSettings.Add(settings);
        }

        settings.LastImportedFileName = fileName;
        settings.LastImportedDate = DateTime.Now;
        await _context.SaveChangesAsync();
    }
}
