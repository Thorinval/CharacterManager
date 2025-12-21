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
    public async Task<ImportResult> ImportPmlAsync(Stream pmlStream, string fileName = "", 
        bool importInventory = true, bool importTemplates = true, 
        bool importBestSquad = true, bool importHistories = true, 
        bool importLucieHouse = true)
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
            if (importInventory)
            {
                var inventaireElements = doc.Root.Elements("inventaire");
                if (!inventaireElements.Any() && doc.Root.Name.LocalName.Equals("inventaire", StringComparison.OrdinalIgnoreCase))
                {
                    inventaireElements = new[] { doc.Root };
                }

                if (inventaireElements.Any())
                {
                    result.SuccessCount += await ImportInventaireAsync(inventaireElements, errors);
                }
            }

            // Traiter la section templates (directs ou encapsulés dans <templates>)
            if (importTemplates)
            {
                var directTemplates = doc.Root.Elements("template");
                var nestedTemplates = doc.Root.Element("templates")?.Elements("template") ?? Enumerable.Empty<XElement>();
                var templateElements = directTemplates.Concat(nestedTemplates);

                if (templateElements.Any())
                {
                    result.SuccessCount += await ImportTemplatesAsync(templateElements, errors);
                }
            }

            // Traiter la section meilleure escouade
            if (importBestSquad)
            {
                var bestSquadElements = doc.Root.Elements("meilleurEscouade");
                if (bestSquadElements.Any())
                {
                    result.SuccessCount += await ImportBestSquadAsync(bestSquadElements, errors);
                }
            }

            // Traiter la section historiques
            if (importHistories)
            {
                var historiqueElements = doc.Root.Elements("HistoriqueEscouade");
                if (historiqueElements.Any())
                {
                    result.SuccessCount += await ImportHistoriquesAsync(historiqueElements, errors);
                }
            }

            // Traiter la section Lucie House
            if (importLucieHouse)
            {
                var lucieHouseElement = doc.Root.Element("LucieHouse");
                if (lucieHouseElement != null)
                {
                    result.SuccessCount += await ImportLucieHouseAsync(lucieHouseElement, errors);
                }
            }

            result.Errors = errors;

            if (result.SuccessCount == 0 && string.IsNullOrEmpty(result.Error))
            {
                result.Error = "Aucune donnée à importer";
            }

            result.IsSuccess = result.SuccessCount > 0 && string.IsNullOrEmpty(result.Error);

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
            var personnageElements = inventaire.Name.LocalName.Equals("Personnage", StringComparison.OrdinalIgnoreCase)
                ? new[] { inventaire }
                : inventaire.Elements("Personnage");

            foreach (var personnageElement in personnageElements)
            {
                try
                {
                    var personnage = ParsePersonnageFromXml(personnageElement);
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
    /// Importe les données de la meilleure escouade
    /// </summary>
    private Task<int> ImportBestSquadAsync(IEnumerable<XElement> bestSquadElements, List<string> errors)
    {
        int importedCount = 0;

        foreach (var bestSquad in bestSquadElements)
        {
            try
            {
                // Import mercenaires
                var mercenairesElements = bestSquad.Elements("Mercenaire");
                foreach (var mercElement in mercenairesElements)
                {
                    var personnage = ParsePersonnageFromXml(mercElement);
                    if (personnage != null)
                    {
                        ImportOrUpdatePersonnage(personnage);
                        importedCount++;
                    }
                }

                // Import commandant
                var commandantElement = bestSquad.Element("Commandant");
                if (commandantElement != null)
                {
                    var personnage = ParsePersonnageFromXml(commandantElement);
                    if (personnage != null)
                    {
                        ImportOrUpdatePersonnage(personnage);
                        importedCount++;
                    }
                }

                // Import androides
                var androidesElements = bestSquad.Elements("Androide");
                foreach (var androidElement in androidesElements)
                {
                    var personnage = ParsePersonnageFromXml(androidElement);
                    if (personnage != null)
                    {
                        ImportOrUpdatePersonnage(personnage);
                        importedCount++;
                    }
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Erreur lors de l'import de la meilleure escouade: {ex.Message}");
            }
        }

        return Task.FromResult(importedCount);
    }

    /// <summary>
    /// Importe les données d'historiques
    /// </summary>
    private async Task<int> ImportHistoriquesAsync(IEnumerable<XElement> historiqueElements, List<string> errors)
    {
        int importedCount = 0;

        foreach (var historiqueElement in historiqueElements)
        {
            try
            {
                var dateStr = historiqueElement.Element("DateEnregistrement")?.Value;
                var puissanceStr = historiqueElement.Element("PuissanceTotal")?.Value;
                var classementStr = historiqueElement.Element("Classement")?.Value;
                var donneesJson = historiqueElement.Element("DonneesEscouadeJson")?.Value;

                if (string.IsNullOrWhiteSpace(dateStr) || string.IsNullOrWhiteSpace(donneesJson))
                {
                    errors.Add("Historique invalide: date ou données manquantes");
                    continue;
                }

                var historique = new HistoriqueEscouade
                {
                    DateEnregistrement = DateTime.TryParse(dateStr, out var date) ? date : DateTime.UtcNow,
                    PuissanceTotal = int.TryParse(puissanceStr, out var puissance) ? puissance : 0,
                    Classement = int.TryParse(classementStr, out var classement) ? classement : null,
                    DonneesEscouadeJson = donneesJson
                };

                _context.HistoriquesEscouade.Add(historique);
                await _context.SaveChangesAsync();
                importedCount++;
            }
            catch (Exception ex)
            {
                errors.Add($"Erreur lors de l'import d'un historique: {ex.Message}");
            }
        }

        return importedCount;
    }

    /// <summary>
    /// Importe les données de la Lucie House
    /// </summary>
    private async Task<int> ImportLucieHouseAsync(XElement lucieHouseElement, List<string> errors)
    {
        int importedCount = 0;

        try
        {
            var lucieHouse = new LucieHouse();
            var piecesElements = lucieHouseElement.Elements("Piece");

            foreach (var pieceElement in piecesElements)
            {
                try
                {
                    var nom = pieceElement.Element("Nom")?.Value;
                    if (string.IsNullOrWhiteSpace(nom))
                        continue;

                    var piece = new Piece
                    {
                        Nom = nom,
                        Niveau = int.TryParse(pieceElement.Element("Niveau")?.Value, out var niveau) ? niveau : 1,
                        Selectionnee = bool.TryParse(pieceElement.Element("Selectionnee")?.Value, out var sel) && sel
                    };

                    var puissanceImportee = int.TryParse(pieceElement.Element("Puissance")?.Value, out var puissance) ? puissance : (int?)null;

                    // Parser les bonus tactiques
                    var bonusTactiquesElement = pieceElement.Element("BonusTactiques");
                    if (bonusTactiquesElement != null)
                    {
                        piece.AspectsTactiques.Bonus = bonusTactiquesElement.Elements("Bonus")
                            .Select(b => b.Value)
                            .Where(b => !string.IsNullOrWhiteSpace(b))
                            .ToList();
                        piece.AspectsTactiques.Puissance = piece.AspectsTactiques.Bonus.Count;
                    }

                    // Parser les bonus stratégiques
                    var bonusStrategiquesElement = pieceElement.Element("BonusStrategiques");
                    if (bonusStrategiquesElement != null)
                    {
                        piece.AspectsStrategiques.Bonus = bonusStrategiquesElement.Elements("Bonus")
                            .Select(b => b.Value)
                            .Where(b => !string.IsNullOrWhiteSpace(b))
                            .ToList();
                        piece.AspectsStrategiques.Puissance = piece.AspectsStrategiques.Bonus.Count;
                    }

                    // Si une puissance totale est fournie, conserver la valeur maximale
                    if (puissanceImportee.HasValue && puissanceImportee.Value > piece.Puissance)
                    {
                        piece.AspectsTactiques.Puissance += Math.Max(0, puissanceImportee.Value - piece.Puissance);
                    }

                    lucieHouse.Pieces.Add(piece);
                    importedCount++;
                }
                catch (Exception ex)
                {
                    errors.Add($"Erreur lors de l'import d'une pièce Lucie House: {ex.Message}");
                }
            }

            // Valider que max 2 pièces sont sélectionnées
            if (lucieHouse.NombrePiecesSelectionnees > LucieHouse.MaxPiecesSelectionnees)
            {
                errors.Add($"Attention: Plus de {LucieHouse.MaxPiecesSelectionnees} pièces sélectionnées dans l'import");
            }

            // Sauvegarder dans la base de données
            var existingLucieHouse = await _context.LucieHouses.Include(l => l.Pieces).FirstOrDefaultAsync();
            if (existingLucieHouse != null)
            {
                _context.LucieHouses.Remove(existingLucieHouse);
            }

            _context.LucieHouses.Add(lucieHouse);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            errors.Add($"Erreur lors de l'import de Lucie House: {ex.Message}");
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
            TypeAttaque = ParseTypeAttaque(element.Element("TypeAttaque")?.Value),
            Selectionne = bool.TryParse(element.Element("Selectionne")?.Value, out var sel) && sel,
            Description = element.Element("Description")?.Value ?? $"Personnage {nom}",
        };

        // Gérer les URLs des images
        string nomLower = personnage.Nom.ToLower();
        // Les images sont maintenant calculées automatiquement selon le nom du personnage

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
                writer.WriteElementString("TypeAttaque", personnage.TypeAttaque.ToString());
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

    /// <summary>
    /// Exporte les données sélectionnées au format PML
    /// </summary>
    public async Task<byte[]> ExportPmlAsync(
        bool exportInventory = true, 
        bool exportTemplates = true, 
        bool exportBestSquad = true, 
        bool exportHistories = true,
        bool exportLucieHouse = true)
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
            writer.WriteStartElement("CharacterManagerPML");
            writer.WriteAttributeString("version", "1.0");
            writer.WriteAttributeString("exportDate", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"));

            // Export inventaire
            if (exportInventory)
            {
                writer.WriteStartElement("inventaire");
                var personnages = await _context.Personnages.ToListAsync();
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
            }

            // Export templates
            if (exportTemplates)
            {
                var templates = await _context.Templates.ToListAsync();
                foreach (var template in templates)
                {
                    writer.WriteStartElement("template");
                    writer.WriteElementString("Nom", template.Nom);
                    writer.WriteElementString("Description", template.Description ?? "");

                    var personnageIds = template.GetPersonnageIds();
                    foreach (var personnageId in personnageIds)
                    {
                        var personnage = await _context.Personnages.FirstOrDefaultAsync(p => p.Id == personnageId);
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
            }

            // Export meilleure escouade
            if (exportBestSquad)
            {
                writer.WriteStartElement("meilleurEscouade");
                
                // Mercenaires (top 10 par puissance)
                var topMercenaires = await _context.Personnages
                    .Where(p => p.Type == TypePersonnage.Mercenaire)
                    .OrderByDescending(p => p.Puissance)
                    .Take(10)
                    .ToListAsync();
                
                foreach (var merc in topMercenaires)
                {
                    writer.WriteStartElement("Mercenaire");
                    WritePersonnageData(writer, merc);
                    writer.WriteEndElement();
                }

                // Commandant (le plus puissant)
                var topCommandant = await _context.Personnages
                    .Where(p => p.Type == TypePersonnage.Commandant)
                    .OrderByDescending(p => p.Puissance)
                    .FirstOrDefaultAsync();
                
                if (topCommandant != null)
                {
                    writer.WriteStartElement("Commandant");
                    WritePersonnageData(writer, topCommandant);
                    writer.WriteEndElement();
                }

                // Androides (top 5 par puissance)
                var topAndroides = await _context.Personnages
                    .Where(p => p.Type == TypePersonnage.Androïde)
                    .OrderByDescending(p => p.Puissance)
                    .Take(5)
                    .ToListAsync();
                
                foreach (var android in topAndroides)
                {
                    writer.WriteStartElement("Androide");
                    WritePersonnageData(writer, android);
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
            }

            // Export historiques
            if (exportHistories)
            {
                var historiques = await _context.HistoriquesEscouade
                    .OrderByDescending(h => h.DateEnregistrement)
                    .Take(50)
                    .ToListAsync();

                foreach (var historique in historiques)
                {
                    writer.WriteStartElement("HistoriqueEscouade");
                    writer.WriteElementString("DateEnregistrement", historique.DateEnregistrement.ToString("yyyy-MM-ddTHH:mm:ssZ"));
                    writer.WriteElementString("PuissanceTotal", historique.PuissanceTotal.ToString());
                    if (historique.Classement.HasValue)
                    {
                        writer.WriteElementString("Classement", historique.Classement.Value.ToString());
                    }
                    writer.WriteElementString("DonneesEscouadeJson", historique.DonneesEscouadeJson);
                    writer.WriteEndElement();
                }
            }

            // Export Lucie House
            if (exportLucieHouse)
            {
                var lucieHouse = await _context.LucieHouses.Include(l => l.Pieces).FirstOrDefaultAsync();
                if (lucieHouse != null)
                {
                    writer.WriteStartElement("LucieHouse");

                    foreach (var piece in lucieHouse.Pieces)
                    {
                        writer.WriteStartElement("Piece");
                        writer.WriteElementString("Nom", piece.Nom);
                        writer.WriteElementString("Niveau", piece.Niveau.ToString());
                        writer.WriteElementString("Puissance", piece.Puissance.ToString());
                        writer.WriteElementString("Selectionnee", piece.Selectionnee.ToString());

                        // Export des bonus tactiques
                        if (piece.AspectsTactiques.Bonus.Count > 0)
                        {
                            writer.WriteStartElement("BonusTactiques");
                            foreach (var bonus in piece.AspectsTactiques.Bonus)
                            {
                                writer.WriteElementString("Bonus", bonus);
                            }
                            writer.WriteEndElement();
                        }

                        // Export des bonus stratégiques
                        if (piece.AspectsStrategiques.Bonus.Count > 0)
                        {
                            writer.WriteStartElement("BonusStrategiques");
                            foreach (var bonus in piece.AspectsStrategiques.Bonus)
                            {
                                writer.WriteElementString("Bonus", bonus);
                            }
                            writer.WriteEndElement();
                        }

                        writer.WriteEndElement();
                    }

                    writer.WriteEndElement();
                }
            }

            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        return memoryStream.ToArray();
    }

    /// <summary>
    /// Helper method to write personnage data to XML
    /// </summary>
    private static void WritePersonnageData(System.Xml.XmlWriter writer, Personnage personnage)
    {
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
        writer.WriteElementString("TypeAttaque", personnage.TypeAttaque.ToString());
        writer.WriteElementString("Selectionne", personnage.Selectionne.ToString());
        writer.WriteElementString("Description", personnage.Description ?? "");
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

    private static TypeAttaque ParseTypeAttaque(string? value)
    {
        return value switch
        {
            "Mêlée" or "Melee" => TypeAttaque.Mêlée,
            "Distance" => TypeAttaque.Distance,
            "Androïde" or "Androide" => TypeAttaque.Androïde,
            _ => TypeAttaque.Inconnu
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
        var settings = await _context.AppSettings.OrderBy(x => x.Id).FirstOrDefaultAsync();
        return settings?.LastImportedFileName;
    }

    private async Task SaveLastImportedFileName(string fileName)
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
}
