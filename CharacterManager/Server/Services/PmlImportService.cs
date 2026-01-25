using CharacterManager.Server.Models;
using CharacterManager.Server.Data;
using CharacterManager.Server.Constants;
using Microsoft.EntityFrameworkCore;
using System.Xml.Linq;
using System.Globalization;
using System.Text.Json;

namespace CharacterManager.Server.Services;

/// <summary>
/// DTO pour les options d'import PML
/// </summary>
public class PmlImportOptions
{
    public bool ImportInventory { get; set; } = true;
    public bool ImportTemplates { get; set; } = true;
    public bool ImportBestSquad { get; set; } = true;
    public bool ImportHistories { get; set; } = true;
    public bool ImportLeagueHistory { get; set; } = false;
}

/// <summary>
/// DTO pour la création d'une entrée d'historique de modification lors de l'import
/// </summary>
public class HistoriqueModificationImportRequest
{
    public string NomPersonnage { get; set; } = string.Empty;
    public string ChampModifie { get; set; } = string.Empty;
    public int NouvelleValeur { get; set; }
    public DateOnly DateClassement { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<ImportLogEntry>? Logs { get; set; }
    public ImportLogCategory Category { get; set; }
    public Dictionary<string, bool>? ConflictResolutions { get; set; }
    public bool ForceOverwriteSameDate { get; set; } = false;
}

/// <summary>
/// Service pour importer les données au format PML (XML personnalisé)
/// Extension .pml pour les fichiers d'import
/// Supporte les sections : HistoriqueClassements, inventaire, template
/// </summary>
public class PmlImportService(ApplicationDbContext context, HistoriqueModificationService? historiqueService = null) : PmlServiceBase(context)
{
    private readonly HistoriqueModificationService? _historiqueService = historiqueService;

    // Constantes pour les types de données dans les logs
    private const string DataTypePuissance = "Puissance";
    private const string DataTypeComposition = "Composition";
    private const string DataTypeClassement = "Classement";

    private static void AddLog(List<ImportLogEntry>? logs, ImportLogLevel level, ImportLogCategory category, string dataType, string message, List<string>? legacyErrors = null)
    {
        logs?.Add(new ImportLogEntry
        {
            Level = level,
            Category = category,
            DataType = dataType,
            Message = message
        });

        if (legacyErrors != null && level != ImportLogLevel.Ok)
        {
            legacyErrors.Add(message);
        }
    }
    /// <summary>
    /// Importe les données du format PML (inventaire, templates, etc.)
    /// </summary>
    public async Task<ImportResult> ImportPmlAsync(Stream pmlStream, string fileName = "",
        bool importInventory = true, bool importTemplates = true,
        bool importBestSquad = true, bool importHistories = true, bool importLeagueHistory = false)
    {
        var options = new PmlImportOptions
        {
            ImportInventory = importInventory,
            ImportTemplates = importTemplates,
            ImportBestSquad = importBestSquad,
            ImportHistories = importHistories,
            ImportLeagueHistory = importLeagueHistory
        };

        return await ImportPmlAsync(pmlStream, fileName, options);
    }

    /// <summary>
    /// Importe les données du format PML (inventaire, templates, etc.)
    /// </summary>
    public async Task<ImportResult> ImportPmlAsync(Stream pmlStream, string fileName, PmlImportOptions options)
    {
        var result = new ImportResult();
        var errors = new List<string>();
        var logs = new List<ImportLogEntry>();

        try
        {
            using var buffer = new MemoryStream();
            await pmlStream.CopyToAsync(buffer);
            buffer.Position = 0;
            var doc = await XDocument.LoadAsync(buffer, LoadOptions.None, CancellationToken.None);

            if (doc.Root == null)
            {
                result.Error = ImportExportConstants.ErrorMessages.ErrorFileEmpty + " ou invalide";
                return result;
            }

            result.SuccessCount += await ProcessImportSections(doc.Root, options, errors, logs);

            result.Errors = errors;
            result.Logs = logs;

            if (result.SuccessCount == 0 && string.IsNullOrEmpty(result.Error))
            {
                result.Error = ImportExportConstants.ErrorMessages.ErrorNoSectionsFound;
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
            result.Error = $"{ImportExportConstants.ErrorMessages.ErrorXmlParsing}: {ex.Message}";
            result.IsSuccess = false;
            result.Logs = logs;
        }

        return result;
    }

    /// <summary>
    /// Détecte les conflits potentiels avant d'importer les historiques de classement
    /// </summary>
    public async Task<ImportResultWithConflicts> DetectHistoriqueClassementConflicts(Stream pmlStream)
    {
        var result = new ImportResultWithConflicts();

        try
        {
            using var buffer = new MemoryStream();
            await pmlStream.CopyToAsync(buffer);
            buffer.Position = 0;
            var doc = await XDocument.LoadAsync(buffer, LoadOptions.None, CancellationToken.None);

            if (doc.Root == null)
                return result;

            var historiqueClassementElements = doc.Root.Elements(ClassementConstants.XmlElements.HistoriqueClassement);

            foreach (var historiqueClassementElement in historiqueClassementElements)
            {
                var errors = new List<string>();
                var historiqueClassement = ParseHistoriqueClassementHeader(historiqueClassementElement, errors);
                if (historiqueClassement == null)
                    continue;

                ImportHistoriqueClassements(historiqueClassementElement, historiqueClassement);
                ImportHistoriqueMercenaires(historiqueClassementElement, historiqueClassement);
                ImportHistoriqueCommandant(historiqueClassementElement, historiqueClassement);
                ImportHistoriqueAndroides(historiqueClassementElement, historiqueClassement);

                // Valider la composition du classement
                if (historiqueClassement.Commandant == null || historiqueClassement.Commandant.Puissance == 0 ||
                    historiqueClassement.Mercenaires.Count != 8 || historiqueClassement.Androides.Count != 3)
                    continue;

                // Détecter les conflits
                var conflicts = await DetectHistoriqueConflicts(historiqueClassement);
                result.Conflicts.AddRange(conflicts.Conflicts);
            }
        }
        catch
        {
            // Ignorer les erreurs de parsing
        }

        return result;
    }

    /// <summary>
    /// Traite un élément d'historique de classement dans le contexte d'une prévisualisation
    /// </summary>
    private async Task ProcessPreviewHistoriqueClassementAsync(XElement historiqueClassementElement, List<ImportLogEntry> logs, ImportPreviewResult preview)
    {
        var errors = new List<string>();
        var historiqueClassement = ParseHistoriqueClassementHeader(historiqueClassementElement, errors, logs);
        if (historiqueClassement == null)
            return;

        ImportHistoriqueClassements(historiqueClassementElement, historiqueClassement);
        ImportHistoriqueMercenaires(historiqueClassementElement, historiqueClassement);
        ImportHistoriqueCommandant(historiqueClassementElement, historiqueClassement);
        ImportHistoriqueAndroides(historiqueClassementElement, historiqueClassement);
        ImportHistoriquePieces(historiqueClassementElement, historiqueClassement);

        // Valider la composition du classement
        if (!ValidateHistoriqueClassementComposition(historiqueClassement, logs, errors))
            return;

        // Vérifier la présence d'un classement existant pour la même date
        var sameDateEntries = await _context.HistoriquesClassement
            .Include(h => h.Classements)
            .Include(h => h.Commandant)
            .Include(h => h.Mercenaires)
            .Include(h => h.Androides)
            .Include(h => h.Pieces)
            .Where(h => h.DateEnregistrement == historiqueClassement.DateEnregistrement)
            .ToListAsync();

        if (sameDateEntries.Any())
        {
            if (sameDateEntries.Any(existing => AreClassementsStrictementIdentiques(existing, historiqueClassement)))
            {
                AddLog(logs, ImportLogLevel.Warning, ImportLogCategory.Classement, DataTypeClassement, $"Import du {historiqueClassement.DateEnregistrement} ignoré: données identiques déjà présentes.", errors);
                return;
            }

            if (!IsBetterRankingForSameDate(historiqueClassement, sameDateEntries))
            {
                AddLog(logs, ImportLogLevel.Warning, ImportLogCategory.Classement, DataTypeClassement, $"Import du {historiqueClassement.DateEnregistrement} ignoré: classements identiques ou moins bons que ceux déjà importés ce jour.", errors);
                return;
            }
        }

        // Détecter les conflits sur les historiques de modification
        var conflictsResult = await DetectHistoriqueConflicts(historiqueClassement);
        preview.Conflicts.AddRange(conflictsResult.Conflicts);

        AddLog(logs, ImportLogLevel.Ok, ImportLogCategory.Classement, DataTypeClassement, $"Import du {historiqueClassement.DateEnregistrement} prêt à être appliqué.");
        preview.ValidCount++;
    }

    /// <summary>
    /// Valide la composition d'un historique de classement (commandant, mercenaires, androides)
    /// </summary>
    private static bool ValidateHistoriqueClassementComposition(HistoriqueClassement historiqueClassement, List<ImportLogEntry> logs, List<string> errors)
    {
        if (historiqueClassement.Commandant == null)
        {
            AddLog(logs, ImportLogLevel.Error, ImportLogCategory.Commandant, "Structure", $"Import du {historiqueClassement.DateEnregistrement} ignoré: pas de commandant trouvé.", errors);
            return false;
        }

        if (historiqueClassement.Commandant.Puissance == 0)
        {
            AddLog(logs, ImportLogLevel.Error, ImportLogCategory.Commandant, DataTypePuissance, $"Import du {historiqueClassement.DateEnregistrement} ignoré: la puissance du commandant est 0.", errors);
            return false;
        }

        if (historiqueClassement.Mercenaires.Count != 8)
        {
            AddLog(logs, ImportLogLevel.Error, ImportLogCategory.Mercenaires, DataTypeComposition, $"Import du {historiqueClassement.DateEnregistrement} ignoré: doit avoir exactement 8 mercenaires, trouvé {historiqueClassement.Mercenaires.Count}.", errors);
            return false;
        }

        if (historiqueClassement.Androides.Count != 3)
        {
            AddLog(logs, ImportLogLevel.Error, ImportLogCategory.Androides, DataTypeComposition, $"Import du {historiqueClassement.DateEnregistrement} ignoré: doit avoir exactement 3 androides, trouvé {historiqueClassement.Androides.Count}.", errors);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Prépare un pré-rapport d'import (sans écriture en base) pour les historiques de classement
    /// </summary>
    public async Task<ImportPreviewResult> PreviewPmlClassementsAsync(Stream pmlStream)
    {
        var preview = new ImportPreviewResult();
        var logs = new List<ImportLogEntry>();

        try
        {
            using var buffer = new MemoryStream();
            await pmlStream.CopyToAsync(buffer);
            buffer.Position = 0;
            var doc = await XDocument.LoadAsync(buffer, LoadOptions.None, CancellationToken.None);

            if (doc.Root == null)
            {
                preview.Error = ImportExportConstants.ErrorMessages.ErrorFileEmpty + " ou invalide";
                return preview;
            }

            var historiqueClassementElements = doc.Root.Elements(ClassementConstants.XmlElements.HistoriqueClassement);

            foreach (var historiqueClassementElement in historiqueClassementElements)
            {
                await ProcessPreviewHistoriqueClassementAsync(historiqueClassementElement, logs, preview);
            }

            preview.Logs = logs;
            preview.IsSuccess = string.IsNullOrEmpty(preview.Error);
        }
        catch (Exception ex)
        {
            preview.Error = $"{ImportExportConstants.ErrorMessages.ErrorXmlParsing}: {ex.Message}";
            preview.IsSuccess = false;
            preview.Logs = logs;
        }

        return preview;
    }

    /// <summary>
    /// Réimporte les historiques avec résolution des conflits fournie par l'utilisateur
    /// </summary>
    public async Task<ImportResult> ImportPmlWithConflictResolution(Stream pmlStream, string fileName, 
        Dictionary<string, bool> conflictResolutions, List<ImportConflict>? originalConflicts = null)
    {
        var result = new ImportResult();
        var errors = new List<string>();
        var logs = new List<ImportLogEntry>();

        try
        {
            using var buffer = new MemoryStream();
            await pmlStream.CopyToAsync(buffer);
            buffer.Position = 0;
            var doc = await XDocument.LoadAsync(buffer, LoadOptions.None, CancellationToken.None);

            if (doc.Root == null)
            {
                result.Error = ImportExportConstants.ErrorMessages.ErrorFileEmpty + " ou invalide";
                return result;
            }

            // Traiter seulement la section historiques avec les résolutions de conflits
            var historiqueClassementElements = doc.Root.Elements(ClassementConstants.XmlElements.HistoriqueClassement);
            result.SuccessCount = await ImportHistoriquesClassementWithResolution(historiqueClassementElements, errors, logs, conflictResolutions);

            result.Errors = errors;
            result.Logs = logs;
            result.IsSuccess = result.SuccessCount > 0;

            // Construire le rapport de résolution des conflits
            if (originalConflicts != null && conflictResolutions.Any())
            {
                foreach (var conflict in originalConflicts)
                {
                    if (conflictResolutions.TryGetValue(conflict.ConflictKey, out var overwrite))
                    {
                        result.ConflictsApplied.Add(new ConflictResolutionApplied
                        {
                            PersonnageName = conflict.PersonnageName,
                            ChampModifie = conflict.ChampModifie,
                            DateClassement = conflict.DateClassement,
                            AncienneValeur = conflict.AncienneValeur,
                            NouvelleValeur = conflict.NouvelleValeur,
                            Overwritten = overwrite
                        });
                    }
                }
            }

            if (!string.IsNullOrEmpty(fileName))
            {
                await SaveLastImportedFileName(fileName);
            }
        }
        catch (Exception ex)
        {
            result.Error = $"{ImportExportConstants.ErrorMessages.ErrorXmlParsing}: {ex.Message}";
            result.IsSuccess = false;
            result.Logs = logs;
        }

        return result;
    }

    /// <summary>
    /// Traite toutes les sections d'import
    /// </summary>
    private async Task<int> ProcessImportSections(XElement root, PmlImportOptions options, List<string> errors, List<ImportLogEntry> logs)
    {
        int totalCount = 0;

        if (options.ImportInventory)
        {
            totalCount += await ProcessInventorySection(root, errors, options);
        }

        if (options.ImportTemplates)
        {
            totalCount += await ProcessTemplatesSection(root, errors);
        }

        if (options.ImportBestSquad)
        {
            totalCount += await ProcessBestSquadSection(root, errors);
        }

        if (options.ImportHistories)
        {
            totalCount += await ProcessHistoriesSection(root, errors, logs);
        }

        if (options.ImportLeagueHistory)
        {
            totalCount += await ProcessLeagueHistorySection(root, errors);
        }

        return totalCount;
    }

    private async Task<int> ProcessInventorySection(XElement root, List<string> errors, PmlImportOptions options)
    {
        // Vérifier si des personnages existent déjà
        var existingPersonnagesCount = await _context.Personnages.CountAsync();
        var allowInventoryOnNonEmpty = options.ImportTemplates
            || options.ImportBestSquad
            || options.ImportHistories
            || options.ImportLeagueHistory;

        if (existingPersonnagesCount > 0 && !allowInventoryOnNonEmpty)
        {
            errors.Add($"Import d'inventaire impossible : {existingPersonnagesCount} personnage(s) existe(nt) déjà dans la base de données. L'import d'inventaire n'est autorisé que sur une base vide.");
            return 0;
        }

        var inventaireElements = root.Elements(ImportExportConstants.XmlElements.Inventaire);
        if (!inventaireElements.Any() && root.Name.LocalName.Equals(ImportExportConstants.XmlElements.Inventaire, StringComparison.OrdinalIgnoreCase))
        {
            inventaireElements = [root];
        }

        return inventaireElements.Any() ? await ImportInventaireAsync(inventaireElements, errors) : 0;
    }

    private async Task<int> ProcessTemplatesSection(XElement root, List<string> errors)
    {
        var directTemplates = root.Elements(TemplateConstants.XmlElements.Template);
        var nestedTemplates = root.Element(TemplateConstants.XmlElements.Templates)?.Elements(TemplateConstants.XmlElements.Template) ?? Enumerable.Empty<XElement>();
        var templateElements = directTemplates.Concat(nestedTemplates);

        return templateElements.Any() ? await ImportTemplatesAsync(templateElements, errors) : 0;
    }

    private async Task<int> ProcessBestSquadSection(XElement root, List<string> errors)
    {
        var bestSquadElements = root.Elements(SquadConstants.XmlElements.MeilleurEscouade);
        return bestSquadElements.Any() ? await ImportBestSquadAsync(bestSquadElements, errors) : 0;
    }

    private async Task<int> ProcessHistoriesSection(XElement root, List<string> errors, List<ImportLogEntry> logs)
    {
        var historiqueClassementElements = root.Elements(ClassementConstants.XmlElements.HistoriqueClassement);
        return historiqueClassementElements.Any() ? await ImportHistoriquesClassementAsync(historiqueClassementElements, errors, logs) : 0;
    }

    private async Task<int> ProcessLeagueHistorySection(XElement root, List<string> errors)
    {
        var historiqueLigueElements = root.Elements(HistoryConstants.XmlElements.HistoriqueLigue);
        return historiqueLigueElements.Any() ? await ImportHistoriquesLigueAsync(historiqueLigueElements, errors) : 0;
    }

    /// <summary>
    /// Importe les données depuis la section inventaire
    /// </summary>
    private async Task<int> ImportInventaireAsync(IEnumerable<XElement> inventaireElements, List<string> errors)
    {
        int importedCount = 0;

        foreach (var inventaire in inventaireElements)
        {
            importedCount += await ProcessInventairePersonnagesAsync(inventaire, errors);
            importedCount += await ProcessLuciHouseSectionAsync(inventaire, errors);
        }

        return importedCount;
    }

    /// <summary>
    /// Traite les personnages dans un élément d'inventaire
    /// </summary>
    private async Task<int> ProcessInventairePersonnagesAsync(XElement inventaire, List<string> errors)
    {
        int importedCount = 0;
        var personnageElements = inventaire.Name.LocalName.Equals(ImportExportConstants.XmlElements.Personnage, StringComparison.OrdinalIgnoreCase)
            ? [inventaire]
            : inventaire.Elements(ImportExportConstants.XmlElements.Personnage);

        foreach (var personnageElement in personnageElements)
        {
            try
            {
                var personnage = ParsePersonnageFromXml(personnageElement);
                if (personnage != null)
                {
                    var persisted = ImportOrUpdatePersonnage(personnage);
                    importedCount++;

                    if (_historiqueService != null)
                    {
                        await _historiqueService.EnregistrerCreationAsync(TypeEntite.Personnage, persisted.Id, persisted.Nom, persisted, "Import inventaire", DateTime.UtcNow, estImportation: true);
                    }
                }
            }
            catch (Exception ex)
            {
                errors.Add($"{ImportExportConstants.ErrorMessages.ErrorImportPersonnageInventaire} {ex.Message}");
            }
        }

        return importedCount;
    }

    /// <summary>
    /// Traite la section Lucie House dans un élément d'inventaire
    /// </summary>
    private async Task<int> ProcessLuciHouseSectionAsync(XElement inventaire, List<string> errors)
    {
        int importedCount = 0;
        var lucieHouseElement = inventaire.Name.LocalName.Equals(LucieHouseConstants.XmlElements.LucieHouse, StringComparison.OrdinalIgnoreCase)
            ? new[] { inventaire }
            : inventaire.Elements(LucieHouseConstants.XmlElements.LucieHouse);

        foreach (var lucieElement in lucieHouseElement)
        {
            importedCount += await ImportLucieHouseAsync(lucieElement, errors);
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
                var templateName = template.Element(ImportExportConstants.XmlElements.Nom)?.Value;
                var description = template.Element(ImportExportConstants.XmlElements.Description)?.Value;

                if (string.IsNullOrWhiteSpace(templateName))
                {
                    errors.Add(TemplateConstants.Validation.ErrorTemplateNoName);
                    continue;
                }

                var existingTemplate = await GetOrCreateTemplate(templateName, description);
                var personnageIds = await ImportTemplatePersonnages(template, templateName, errors);

                if (personnageIds.Count > 0)
                {
                    existingTemplate.SetPersonnageIds(personnageIds);
                }

                await _context.SaveChangesAsync();
                importedCount += personnageIds.Count;
            }
            catch (Exception ex)
            {
                errors.Add($"{TemplateConstants.Validation.ErrorImportTemplate} {ex.Message}");
            }
        }

        return importedCount;
    }

    private async Task<Template> GetOrCreateTemplate(string templateName, string? description)
    {
        var normalizedTemplate = NormalizeUpper(templateName);

        var existingTemplate = await _context.Templates
            .FirstOrDefaultAsync(t => t.Nom.Equals(normalizedTemplate, StringComparison.OrdinalIgnoreCase));

        if (existingTemplate == null)
        {
            existingTemplate = new Template { Nom = templateName };
            _context.Templates.Add(existingTemplate);
            await _context.SaveChangesAsync();
        }

        if (!string.IsNullOrWhiteSpace(description))
        {
            existingTemplate.Description = description;
        }

        return existingTemplate;
    }

    private async Task<List<int>> ImportTemplatePersonnages(XElement template, string templateName, List<string> errors)
    {
        var personnageIds = new List<int>();
        var personnagesElements = template.Elements(ImportExportConstants.XmlElements.Personnage);

        foreach (var personElement in personnagesElements)
        {
            try
            {
                var nom = personElement.Element(ImportExportConstants.XmlElements.Nom)?.Value;
                if (string.IsNullOrWhiteSpace(nom))
                    continue;

                var personnage = await _context.Personnages
                    .FirstOrDefaultAsync(p => p.Nom.Equals(nom, StringComparison.OrdinalIgnoreCase));

                if (personnage != null && !personnageIds.Contains(personnage.Id))
                {
                    personnageIds.Add(personnage.Id);
                }
            }
            catch (Exception ex)
            {
                errors.Add($"{TemplateConstants.Validation.ErrorImportPersonnageTemplate} '{templateName}': {ex.Message}");
            }
        }

        return personnageIds;
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
                importedCount += ImportBestSquadMercenaires(bestSquad);
                importedCount += ImportBestSquadCommandant(bestSquad);
                importedCount += ImportBestSquadAndroides(bestSquad);
            }
            catch (Exception ex)
            {
                errors.Add($"{SquadConstants.ErrorMessages.ErrorImportBestSquad} {ex.Message}");
            }
        }

        return Task.FromResult(importedCount);
    }

    private int ImportBestSquadMercenaires(XElement bestSquad)
    {
        int count = 0;
        var mercenairesElements = bestSquad.Elements(PersonnageConstants.Types.Mercenaire);
        foreach (var mercElement in mercenairesElements)
        {
            var personnage = ParsePersonnageFromXml(mercElement);
            if (personnage != null)
            {
                ImportOrUpdatePersonnage(personnage);
                count++;
            }
        }
        return count;
    }

    private int ImportBestSquadCommandant(XElement bestSquad)
    {
        var commandantElement = bestSquad.Element(PersonnageConstants.Types.Commandant);
        if (commandantElement == null)
            return 0;

        var personnage = ParsePersonnageFromXml(commandantElement);
        if (personnage != null)
        {
            ImportOrUpdatePersonnage(personnage);
            return 1;
        }
        return 0;
    }

    private int ImportBestSquadAndroides(XElement bestSquad)
    {
        int count = 0;
        var androidesElements = bestSquad.Elements(PersonnageConstants.Types.Androide)
            .Concat(bestSquad.Elements("Androide"));

        foreach (var androidElement in androidesElements)
        {
            var personnage = ParsePersonnageFromXml(androidElement);
            if (personnage != null)
            {
                ImportOrUpdatePersonnage(personnage);
                count++;
            }
        }
        return count;
    }

    /// <summary>
    /// Importe les données d'historiques de ligue
    /// </summary>
    private async Task<int> ImportHistoriquesLigueAsync(IEnumerable<XElement> historiqueLigueElements, List<string> errors)
    {
        int importedCount = 0;

        foreach (var historiqueLigueElement in historiqueLigueElements)
        {
            try
            {
                var dateStr = historiqueLigueElement.Element(HistoryConstants.XmlElements.DateMontee)?.Value;
                var ligueStr = historiqueLigueElement.Element(ClassementConstants.XmlElements.Ligue)?.Value;
                var notes = historiqueLigueElement.Element(HistoryConstants.XmlElements.Notes)?.Value;

                if (string.IsNullOrWhiteSpace(dateStr) || string.IsNullOrWhiteSpace(ligueStr))
                {
                    errors.Add("Historique de ligue invalide: date ou ligue manquante");
                    continue;
                }

                if (!DateOnly.TryParse(dateStr, CultureInfo.InvariantCulture, out var DateMontee))
                {
                    errors.Add($"Date de montée invalide: {dateStr}");
                    continue;
                }

                if (!int.TryParse(ligueStr, out var ligue) || ligue < 1 || ligue > 50)
                {
                    errors.Add($"Numéro de ligue invalide: {ligueStr}");
                    continue;
                }

                var historiqueLigue = new HistoriqueLigue
                {
                    DateMontee = DateMontee,
                    Ligue = ligue,
                    Notes = notes
                };

                _context.HistoriquesLigue.Add(historiqueLigue);
                await _context.SaveChangesAsync();
                importedCount++;
            }
            catch (Exception ex)
            {
                errors.Add($"Erreur lors de l'import d'un historique de ligue: {ex.Message}");
            }
        }

        return importedCount;
    }

    /// <summary>
    /// Importe les données d'historiques de classement
    /// Validation: doit avoir exactement 1 commandant + 8 mercenaires + 3 androides
    /// Validation: la puissance du commandant (Lucie) ne doit pas être 0
    /// </summary>
    private async Task<int> ImportHistoriquesClassementAsync(IEnumerable<XElement> historiqueClassementElements, List<string> errors, List<ImportLogEntry> logs)
    {
        int importedCount = 0;

        foreach (var historiqueClassementElement in historiqueClassementElements)
        {
            try
            {
                var historiqueClassement = ParseHistoriqueClassementHeader(historiqueClassementElement, errors, logs);
                if (historiqueClassement == null)
                    continue;

                ImportHistoriqueClassements(historiqueClassementElement, historiqueClassement);
                ImportHistoriqueMercenaires(historiqueClassementElement, historiqueClassement);
                ImportHistoriqueCommandant(historiqueClassementElement, historiqueClassement);
                ImportHistoriqueAndroides(historiqueClassementElement, historiqueClassement);
                ImportHistoriquePieces(historiqueClassementElement, historiqueClassement);

                // Valider la composition du classement
                if (historiqueClassement.Commandant == null)
                {
                    AddLog(logs, ImportLogLevel.Error, ImportLogCategory.Commandant, "Structure", $"Import du {historiqueClassement.DateEnregistrement} ignoré: pas de commandant trouvé.", errors);
                    continue;
                }

                if (historiqueClassement.Commandant.Puissance == 0)
                {
                    AddLog(logs, ImportLogLevel.Error, ImportLogCategory.Commandant, DataTypePuissance, $"Import du {historiqueClassement.DateEnregistrement} ignoré: la puissance du commandant est 0.", errors);
                    continue;
                }

                if (historiqueClassement.Mercenaires.Count != 8)
                {
                    AddLog(logs, ImportLogLevel.Error, ImportLogCategory.Mercenaires, DataTypeComposition, $"Import du {historiqueClassement.DateEnregistrement} ignoré: doit avoir exactement 8 mercenaires, trouvé {historiqueClassement.Mercenaires.Count}.", errors);
                    continue;
                }

                if (historiqueClassement.Androides.Count != 3)
                {
                    AddLog(logs, ImportLogLevel.Error, ImportLogCategory.Androides, DataTypeComposition, $"Import du {historiqueClassement.DateEnregistrement} ignoré: doit avoir exactement 3 androides, trouvé {historiqueClassement.Androides.Count}.", errors);
                    continue;
                }

                bool forceOverwriteSameDate = false;

                // Vérifier si un classement existe déjà pour cette date
                var sameDateEntries = await _context.HistoriquesClassement
                    .Include(h => h.Classements)
                    .Include(h => h.Commandant)
                    .Include(h => h.Mercenaires)
                    .Include(h => h.Androides)
                    .Include(h => h.Pieces)
                    .Where(h => h.DateEnregistrement == historiqueClassement.DateEnregistrement)
                    .ToListAsync();

                if (sameDateEntries.Any())
                {
                    // Si les données sont strictement identiques, ne rien importer
                    if (sameDateEntries.Any(existing => AreClassementsStrictementIdentiques(existing, historiqueClassement)))
                    {
                        AddLog(logs, ImportLogLevel.Warning, ImportLogCategory.Classement, DataTypeClassement, $"Import du {historiqueClassement.DateEnregistrement} ignoré: données identiques déjà présentes.", errors);
                        continue;
                    }

                    // Si la date est identique mais que les classements ne sont pas meilleurs, ignorer
                    if (!IsBetterRankingForSameDate(historiqueClassement, sameDateEntries))
                    {
                        AddLog(logs, ImportLogLevel.Warning, ImportLogCategory.Classement, DataTypeClassement, $"Import du {historiqueClassement.DateEnregistrement} ignoré: classements identiques ou moins bons que ceux déjà importés ce jour.", errors);
                        continue;
                    }

                    // Nouvelle version meilleure pour la même date → on force la mise à jour des modifications du jour
                    forceOverwriteSameDate = true;
                }

                _context.HistoriquesClassement.Add(historiqueClassement);
                await _context.SaveChangesAsync();
                importedCount++;

                AddLog(logs, ImportLogLevel.Ok, ImportLogCategory.Classement, "Synthese", $"Import du {historiqueClassement.DateEnregistrement} validé.");

                // Créer les entrées d'historique de modification pour chaque personnage du classement
                if (_historiqueService != null)
                {
                    await CreateHistoriqueModificationsForClassement(historiqueClassement, errors, logs, null, forceOverwriteSameDate);
                }
            }
            catch (Exception ex)
            {
                AddLog(logs, ImportLogLevel.Error, ImportLogCategory.Classement, "Exception", $"Erreur lors de l'import d'un historique de classement: {ex.Message}", errors);
            }
        }

        return importedCount;
    }

    /// <summary>
    /// Importe les historiques avec résolution des conflits
    /// </summary>
    private async Task<int> ImportHistoriquesClassementWithResolution(IEnumerable<XElement> historiqueClassementElements, List<string> errors, List<ImportLogEntry> logs, Dictionary<string, bool> conflictResolutions)
    {
        int importedCount = 0;

        foreach (var historiqueClassementElement in historiqueClassementElements)
        {
            importedCount += await ProcessHistoriqueWithConflictResolutionAsync(historiqueClassementElement, errors, logs, conflictResolutions);
        }

        return importedCount;
    }

    /// <summary>
    /// Traite un historique de classement avec résolution de conflits
    /// </summary>
    private async Task<int> ProcessHistoriqueWithConflictResolutionAsync(XElement historiqueClassementElement, List<string> errors, List<ImportLogEntry> logs, Dictionary<string, bool> conflictResolutions)
    {
        try
        {
            var historiqueClassement = ParseHistoriqueClassementHeader(historiqueClassementElement, errors, logs);
            if (historiqueClassement == null)
                return 0;

            ImportHistoriqueClassements(historiqueClassementElement, historiqueClassement);
            ImportHistoriqueMercenaires(historiqueClassementElement, historiqueClassement);
            ImportHistoriqueCommandant(historiqueClassementElement, historiqueClassement);
            ImportHistoriqueAndroides(historiqueClassementElement, historiqueClassement);
            ImportHistoriquePieces(historiqueClassementElement, historiqueClassement);

            // Valider la composition du classement
            if (!ValidateHistoriqueClassementComposition(historiqueClassement, logs, errors))
                return 0;

            var (shouldContinue, forceOverwriteSameDate) = await ValidateExistingClassementsForSameDate(historiqueClassement, logs, errors);
            if (!shouldContinue)
                return 0;

            _context.HistoriquesClassement.Add(historiqueClassement);
            await _context.SaveChangesAsync();

            AddLog(logs, ImportLogLevel.Ok, ImportLogCategory.Classement, "Synthese", $"Import du {historiqueClassement.DateEnregistrement} validé (résolution de conflits).");

            // Créer les entrées d'historique de modification avec résolutions des conflits
            if (_historiqueService != null)
            {
                await CreateHistoriqueModificationsForClassement(historiqueClassement, errors, logs, conflictResolutions, forceOverwriteSameDate);
            }

            return 1;
        }
        catch (Exception ex)
        {
            AddLog(logs, ImportLogLevel.Error, ImportLogCategory.Classement, "Exception", $"Erreur lors de l'import d'un historique de classement: {ex.Message}", errors);
            return 0;
        }
    }

    /// <summary>
    /// Valide les classements existants à la même date
    /// </summary>
    private async Task<(bool shouldContinue, bool forceOverwriteSameDate)> ValidateExistingClassementsForSameDate(HistoriqueClassement historiqueClassement, List<ImportLogEntry> logs, List<string> errors)
    {
        var sameDateEntries = await _context.HistoriquesClassement
            .Include(h => h.Classements)
            .Include(h => h.Commandant)
            .Include(h => h.Mercenaires)
            .Include(h => h.Androides)
            .Include(h => h.Pieces)
            .Where(h => h.DateEnregistrement == historiqueClassement.DateEnregistrement)
            .ToListAsync();

        if (!sameDateEntries.Any())
            return (true, false);

        if (sameDateEntries.Any(existing => AreClassementsStrictementIdentiques(existing, historiqueClassement)))
        {
            AddLog(logs, ImportLogLevel.Warning, ImportLogCategory.Classement, DataTypeClassement, $"Import du {historiqueClassement.DateEnregistrement} ignoré: données identiques déjà présentes.", errors);
            return (false, false);
        }

        if (!IsBetterRankingForSameDate(historiqueClassement, sameDateEntries))
        {
            AddLog(logs, ImportLogLevel.Warning, ImportLogCategory.Classement, DataTypeClassement, $"Import du {historiqueClassement.DateEnregistrement} ignoré: classements identiques ou moins bons que ceux déjà importés ce jour.", errors);
            return (false, false);
        }

        return (true, true);
    }

    /// <summary>
    /// Détecte les conflits lors de l'import des historiques de classement
    /// </summary>
    private async Task<ImportResultWithConflicts> DetectHistoriqueConflicts(HistoriqueClassement historiqueClassement)
    {
        var result = new ImportResultWithConflicts();
        if (_historiqueService == null)
            return result;

        var dateClassement = historiqueClassement.DateEnregistrement;
        var dateTimeClassement = dateClassement.ToDateTime(TimeOnly.MinValue);

        // Vérifier les conflits pour le commandant
        if (historiqueClassement.Commandant != null)
        {
            await CheckPersonnageConflicts(historiqueClassement.Commandant, dateClassement, dateTimeClassement, result);
        }

        // Vérifier les conflits pour chaque mercenaire
        foreach (var mercenaire in historiqueClassement.Mercenaires)
        {
            await CheckPersonnageConflicts(mercenaire, dateClassement, dateTimeClassement, result);
        }

        // Vérifier les conflits pour chaque androïde
        foreach (var androide in historiqueClassement.Androides)
        {
            await CheckPersonnageConflicts(androide, dateClassement, dateTimeClassement, result);
        }

        return result;
    }

    /// <summary>
    /// Vérifie les conflits pour un personnage (Puissance, Niveau, Rang)
    /// </summary>
    private async Task CheckPersonnageConflicts(PersonnageClassement personnage, DateOnly dateClassement, DateTime dateTimeClassement, ImportResultWithConflicts result)
    {
        var champs = new[] { ("Puissance", personnage.Puissance), ("Niveau", personnage.Niveau), ("Rang", personnage.Rang) };
        
        foreach (var (champ, valeur) in champs)
        {
            var existingModification = await _context.HistoriquesModifications
                .Where(h => h.NomEntite == personnage.Nom
                    && h.ChampModifie == champ
                    && h.DateModification.Date == dateTimeClassement.Date)
                .FirstOrDefaultAsync();

            if (existingModification != null)
            {
                var conflict = new ImportConflict
                {
                    PersonnageName = personnage.Nom,
                    ChampModifie = champ,
                    DateClassement = dateClassement,
                    AncienneValeur = existingModification.AncienneValeur,
                    NouvelleValeur = valeur
                };
                result.Conflicts.Add(conflict);
            }
        }
    }

    /// <summary>
    /// Crée les entrées d'historique de modification pour chaque personnage du classement
    /// avec option de résoudre les conflits
    /// </summary>
    private async Task CreateHistoriqueModificationsForClassement(HistoriqueClassement historiqueClassement, List<string> errors, List<ImportLogEntry> logs, Dictionary<string, bool>? conflictResolutions = null, bool forceOverwriteSameDate = false)
    {
        if (_historiqueService == null)
            return;

        var dateClassement = historiqueClassement.DateEnregistrement;
        
        // Vérifier et créer l'historique pour le commandant
        if (historiqueClassement.Commandant != null)
        {
            await TryCreateHistoriqueModifications(historiqueClassement.Commandant, dateClassement, errors, logs, ImportLogCategory.Commandant, conflictResolutions, forceOverwriteSameDate);
        }

        // Vérifier et créer l'historique pour chaque mercenaire
        foreach (var mercenaire in historiqueClassement.Mercenaires)
        {
            await TryCreateHistoriqueModifications(mercenaire, dateClassement, errors, logs, ImportLogCategory.Mercenaires, conflictResolutions, forceOverwriteSameDate);
        }

        // Vérifier et créer l'historique pour chaque androïde
        foreach (var androide in historiqueClassement.Androides)
        {
            await TryCreateHistoriqueModifications(androide, dateClassement, errors, logs, ImportLogCategory.Androides, conflictResolutions, forceOverwriteSameDate);
        }
    }

    /// <summary>
    /// Crée les trois modifications (Puissance, Niveau, Rang) pour un personnage
    /// </summary>
    private async Task TryCreateHistoriqueModifications(PersonnageClassement personnage, DateOnly dateClassement, List<string> errors, List<ImportLogEntry> logs, ImportLogCategory category, Dictionary<string, bool>? conflictResolutions = null, bool forceOverwriteSameDate = false)
    {
        var request = new HistoriqueModificationImportRequest
        {
            NomPersonnage = personnage.Nom,
            ChampModifie = "Puissance",
            NouvelleValeur = personnage.Puissance,
            DateClassement = dateClassement,
            Errors = errors,
            Logs = logs,
            Category = category,
            ConflictResolutions = conflictResolutions,
            ForceOverwriteSameDate = forceOverwriteSameDate
        };
        await TryCreateHistoriqueModification(request);

        request.ChampModifie = "Niveau";
        request.NouvelleValeur = personnage.Niveau;
        await TryCreateHistoriqueModification(request);

        request.ChampModifie = "Rang";
        request.NouvelleValeur = personnage.Rang;
        await TryCreateHistoriqueModification(request);
    }

    /// <summary>
    /// Tente de créer une entrée d'historique de modification si elle n'existe pas déjà
    /// Si le personnage n'existe pas, il est créé automatiquement en inventaire
    /// Si aucune modification n'existe pour ce champ avant la date de l'import, l'ancienne valeur = nouvelle valeur
    /// </summary>
    /// <summary>
    /// Traite la création d'une modification d'historique pour un personnage spécifique
    /// </summary>
    private async Task ProcessHistoriqueModificationForPersonnageAsync(HistoriqueModificationImportRequest request)
    {
        if (_historiqueService == null)
            return;

        var dateTimeClassement = request.DateClassement.ToDateTime(TimeOnly.MinValue);
        var typeEntite = TypeEntite.Personnage;

        var personnage = await GetOrCreatePersonnageForModification(request);
        if (personnage == null)
            return;

        // Vérifier si une modification existe déjà ce jour-là
        var existingModification = await _context.HistoriquesModifications
            .Where(h => h.TypeEntite == typeEntite
                && h.EntiteId == personnage.Id
                && h.ChampModifie == request.ChampModifie
                && h.DateModification.Date == dateTimeClassement.Date)
            .FirstOrDefaultAsync();

        if (existingModification != null)
        {
            await HandleExistingModification(existingModification, request, typeEntite, personnage.Id, dateTimeClassement);
            return;
        }

        await CreateNewModification(request, typeEntite, personnage.Id, dateTimeClassement);
    }

    /// <summary>
    /// Obtient ou crée un personnage pour la modification d'historique
    /// </summary>
    private async Task<Personnage?> GetOrCreatePersonnageForModification(HistoriqueModificationImportRequest request)
    {
        // Normaliser le nom en majuscules pour la recherche
        var nomNormalise = request.NomPersonnage.ToUpperInvariant();
        
        var personnage = await _context.Personnages
            .FirstOrDefaultAsync(p => p.Nom.ToUpper() == nomNormalise);
        
        if (personnage == null)
        {
            personnage = new Personnage
            {
                Nom = nomNormalise, // Stocker en majuscules
                Niveau = 1,
                Type = TypePersonnage.Mercenaire,
                Rang = 1,
                Puissance = request.NouvelleValeur,
                Rarete = Rarete.Inconnu,
                Role = Role.Inconnu,
                Faction = Faction.Inconnu,
                TypeAttaque = TypeAttaque.Inconnu
            };
            _context.Personnages.Add(personnage);
            await _context.SaveChangesAsync();
            AddLog(request.Logs, ImportLogLevel.Warning, request.Category, "Creation", $"Personnage créé automatiquement lors de l'import: {nomNormalise}", request.Errors);
        }

        return personnage;
    }

    /// <summary>
    /// Gère le cas où une modification existe déjà pour le même jour
    /// </summary>
    private async Task HandleExistingModification(HistoriqueModification existingModification, HistoriqueModificationImportRequest request, TypeEntite typeEntite, int personnageId, DateTime dateTimeClassement)
    {
        var conflictKey = $"{request.NomPersonnage}_{request.ChampModifie}_{request.DateClassement}";
        var shouldOverwrite = request.ForceOverwriteSameDate;

        if (!shouldOverwrite && request.ConflictResolutions != null && request.ConflictResolutions.TryGetValue(conflictKey, out var overwriteDecision))
        {
            shouldOverwrite = overwriteDecision;
        }

        if (shouldOverwrite)
        {
            await UpdateExistingModification(existingModification, request, typeEntite, personnageId, dateTimeClassement);
        }
        else
        {
            AddLog(request.Logs, ImportLogLevel.Warning, request.Category, request.ChampModifie, $"Modification du {request.ChampModifie} pour {request.NomPersonnage} existe déjà le {request.DateClassement}. Cette modification n'a pas été importée.", request.Errors);
        }
    }

    /// <summary>
    /// Met à jour une modification d'historique existante
    /// </summary>
    private async Task UpdateExistingModification(HistoriqueModification existingModification, HistoriqueModificationImportRequest request, TypeEntite typeEntite, int personnageId, DateTime dateTimeClassement)
    {
        var previousMod = await _context.HistoriquesModifications
            .Where(h => h.TypeEntite == typeEntite
                && h.EntiteId == personnageId
                && h.ChampModifie == request.ChampModifie
                && h.DateModification.Date < dateTimeClassement.Date)
            .OrderByDescending(h => h.DateModification)
            .FirstOrDefaultAsync();

        object? ancienneVal = previousMod?.NouvelleValeur != null 
            ? JsonSerializer.Deserialize<object>(previousMod.NouvelleValeur)
            : request.NouvelleValeur;

        existingModification.AncienneValeur = ancienneVal != null ? JsonSerializer.Serialize(ancienneVal) : null;
        existingModification.NouvelleValeur = JsonSerializer.Serialize(request.NouvelleValeur);
        existingModification.DateMiseAJour = dateTimeClassement;
        _context.HistoriquesModifications.Update(existingModification);
        await _context.SaveChangesAsync();
        AddLog(request.Logs, ImportLogLevel.Warning, request.Category, request.ChampModifie, $"Modification du {request.ChampModifie} pour {request.NomPersonnage} mise à jour pour le {request.DateClassement}.", request.Errors);

        await UpdateNextFutureModification(typeEntite, personnageId, request.ChampModifie, dateTimeClassement, request.NouvelleValeur);
    }

    /// <summary>
    /// Crée une nouvelle modification d'historique
    /// </summary>
    private async Task CreateNewModification(HistoriqueModificationImportRequest request, TypeEntite typeEntite, int personnageId, DateTime dateTimeClassement)
    {
        var previousModification = await _context.HistoriquesModifications
            .Where(h => h.TypeEntite == typeEntite
                && h.EntiteId == personnageId
                && h.ChampModifie == request.ChampModifie
                && h.DateModification.Date < dateTimeClassement.Date)
            .OrderByDescending(h => h.DateModification)
            .FirstOrDefaultAsync();

        object? ancienneValeurNew = previousModification?.NouvelleValeur != null 
            ? JsonSerializer.Deserialize<object>(previousModification.NouvelleValeur)
            : request.NouvelleValeur;

        try
        {
            await _historiqueService!.EnregistrerModificationAsync(
                typeEntite,
                personnageId,
                request.NomPersonnage,
                request.ChampModifie,
                ancienneValeurNew,
                request.NouvelleValeur,
                $"Mise à jour du classement du {request.DateClassement}",
                dateTimeClassement,
                estImportation: true);

            AddLog(request.Logs, ImportLogLevel.Ok, request.Category, request.ChampModifie, $"{request.ChampModifie} de {request.NomPersonnage} enregistré pour le {request.DateClassement} (valeur {request.NouvelleValeur}).");

            await UpdateNextFutureModification(typeEntite, personnageId, request.ChampModifie, dateTimeClassement, request.NouvelleValeur);
        }
        catch (Exception ex)
        {
            AddLog(request.Logs, ImportLogLevel.Error, request.Category, request.ChampModifie, $"Erreur création historique pour {request.NomPersonnage}: {ex.Message}", request.Errors);
        }
    }

    private async Task TryCreateHistoriqueModification(HistoriqueModificationImportRequest request)
    {
        await ProcessHistoriqueModificationForPersonnageAsync(request);
    }

    /// <summary>
    /// Tente de créer une entrée d'historique de modification si elle n'existe pas déjà (surcharge compatible).
    /// Si le personnage n'existe pas, il est créé automatiquement en inventaire
    /// Si aucune modification n'existe pour ce champ avant la date de l'import, l'ancienne valeur = nouvelle valeur
    /// </summary>

    private static bool AreClassementsStrictementIdentiques(HistoriqueClassement existing, HistoriqueClassement incoming)
    {
        // Comparer les trois classements principaux + métadonnées de base
        return
            GetClassementValue(existing, TypeClassement.Nutaku) == GetClassementValue(incoming, TypeClassement.Nutaku) &&
            GetClassementValue(existing, TypeClassement.Top150) == GetClassementValue(incoming, TypeClassement.Top150) &&
            GetClassementValue(existing, TypeClassement.France) == GetClassementValue(incoming, TypeClassement.France) &&
            existing.Ligue == incoming.Ligue &&
            existing.Score == incoming.Score &&
            existing.PuissanceCommandant == incoming.PuissanceCommandant &&
            existing.PuissanceMercenaires == incoming.PuissanceMercenaires &&
            existing.PuissanceLucie == incoming.PuissanceLucie &&
            existing.PuissanceTotale == incoming.PuissanceTotale;
    }

    private static bool IsBetterRankingForSameDate(HistoriqueClassement incoming, List<HistoriqueClassement> existingSameDate)
    {
        // On prend la meilleure (plus petite) valeur déjà importée pour cette date
        var bestNutaku = existingSameDate.Min(h => GetClassementValue(h, TypeClassement.Nutaku));
        var bestTop150 = existingSameDate.Min(h => GetClassementValue(h, TypeClassement.Top150));
        var bestFrance = existingSameDate.Min(h => GetClassementValue(h, TypeClassement.France));

        var newNutaku = GetClassementValue(incoming, TypeClassement.Nutaku);
        var newTop150 = GetClassementValue(incoming, TypeClassement.Top150);
        var newFrance = GetClassementValue(incoming, TypeClassement.France);

        // Règle: au moins un classement strictement meilleur, aucun classement moins bon
        var atLeastOneBetter = (newNutaku < bestNutaku) || (newTop150 < bestTop150) || (newFrance < bestFrance);
        var noneWorse = (newNutaku <= bestNutaku) && (newTop150 <= bestTop150) && (newFrance <= bestFrance);

        return atLeastOneBetter && noneWorse;
    }

    private static int GetClassementValue(HistoriqueClassement historique, TypeClassement type)
    {
        return historique.Classements.FirstOrDefault(c => c.Type == type)?.Valeur ?? int.MaxValue;
    }

    /// <summary>
    /// Met à jour l'ancienne valeur de la prochaine modification future
    /// </summary>
    private async Task UpdateNextFutureModification(TypeEntite typeEntite, int entiteId, string champModifie, 
        DateTime dateTimeClassement, int nouvelleValeur)
    {
        // Chercher la prochaine modification la plus proche dans le futur
        var nextModification = await _context.HistoriquesModifications
            .Where(h => h.TypeEntite == typeEntite
                && h.EntiteId == entiteId
                && h.ChampModifie == champModifie
                && h.DateModification.Date > dateTimeClassement.Date)
            .OrderBy(h => h.DateModification)
            .FirstOrDefaultAsync();

        if (nextModification != null)
        {
            // Mettre à jour son ancienne valeur avec la nouvelle valeur qu'on vient de créer
            nextModification.AncienneValeur = JsonSerializer.Serialize(nouvelleValeur);
            nextModification.DateMiseAJour = DateTime.UtcNow;
            _context.HistoriquesModifications.Update(nextModification);
            await _context.SaveChangesAsync();
        }
    }

    private HistoriqueClassement? ParseHistoriqueClassementHeader(XElement element, List<string> errors, List<ImportLogEntry>? logs = null)
    {
        var dateStr = element.Element(ClassementConstants.XmlElements.DateEnregistrement)?.Value;

        if (string.IsNullOrWhiteSpace(dateStr))
        {
            AddLog(logs, ImportLogLevel.Error, ImportLogCategory.Classement, "Date", "Historique de classement invalide: date manquante", errors);
            return null;
        }

        if (!DateOnly.TryParse(dateStr, CultureInfo.InvariantCulture, out var dateEnregistrement))
        {
            AddLog(logs, ImportLogLevel.Error, ImportLogCategory.Classement, "Date", $"Date d'enregistrement invalide: {dateStr}", errors);
            return null;
        }

        var ligueStr = element.Element(ClassementConstants.XmlElements.Ligue)?.Value;
        var scoreStr = element.Element(ClassementConstants.XmlElements.Score)?.Value;
        var puissanceTotalStr = element.Element(ClassementConstants.XmlElements.PuissanceTotal)?.Value;
        var puissanceCommandantStr = element.Element(ClassementConstants.XmlElements.PuissanceCommandant)?.Value;
        var puissanceMercenairesStr = element.Element(ClassementConstants.XmlElements.PuissanceMercenaires)?.Value;
        var puissanceLucieStr = element.Element(ClassementConstants.XmlElements.PuissanceLucie)?.Value;

        var historique = new HistoriqueClassement
        {
            DateEnregistrement = dateEnregistrement,
            Ligue = int.TryParse(ligueStr, out var ligue) ? ligue : 0,
            Score = int.TryParse(scoreStr, out var score) ? score : 0,
            PuissanceTotale = int.TryParse(puissanceTotalStr, out var puissanceTotal) ? puissanceTotal : 0,
            PuissanceCommandant = int.TryParse(puissanceCommandantStr, out var puissanceCommandant) ? puissanceCommandant : 0,
            PuissanceMercenaires = int.TryParse(puissanceMercenairesStr, out var puissanceMercenaires) ? puissanceMercenaires : 0,
            PuissanceLucie = int.TryParse(puissanceLucieStr, out var puissanceLucie) ? puissanceLucie : 0
        };

        AddLog(logs, ImportLogLevel.Ok, ImportLogCategory.Classement, "Date", $"Classement du {historique.DateEnregistrement} prêt à être importé.");
        AddLog(logs, ImportLogLevel.Ok, ImportLogCategory.Classement, "Ligue", $"Ligue: {historique.Ligue}");
        AddLog(logs, ImportLogLevel.Ok, ImportLogCategory.Classement, "Score", $"Score: {historique.Score}");
        AddLog(logs, ImportLogLevel.Ok, ImportLogCategory.Classement, "PuissanceTotale", $"Puissance totale: {historique.PuissanceTotale}");
        AddLog(logs, ImportLogLevel.Ok, ImportLogCategory.Classement, "PuissanceCommandant", $"Puissance commandant: {historique.PuissanceCommandant}");
        AddLog(logs, ImportLogLevel.Ok, ImportLogCategory.Classement, "PuissanceMercenaires", $"Puissance mercenaires: {historique.PuissanceMercenaires}");
        AddLog(logs, ImportLogLevel.Ok, ImportLogCategory.Lucie, "PuissanceLucie", $"Puissance Lucie: {historique.PuissanceLucie}");

        return historique;
    }

    private void ImportHistoriqueClassements(XElement element, HistoriqueClassement historiqueClassement)
    {
        var classementsElement = element.Element(ClassementConstants.XmlElements.Classements);
        if (classementsElement == null)
            return;

        foreach (var classementElement in classementsElement.Elements(ClassementConstants.XmlElements.ClassementItem))
        {
            var nom = classementElement.Element(ImportExportConstants.XmlElements.Nom)?.Value ?? "";
            var typeStr = classementElement.Element(ClassementConstants.XmlElements.TypeClassement)?.Value;
            var valeurStr = classementElement.Element(ClassementConstants.XmlElements.Valeur)?.Value;

            if (Enum.TryParse<TypeClassement>(typeStr, out var type) && int.TryParse(valeurStr, out var valeur))
            {
                historiqueClassement.Classements.Add(new Classement
                {
                    Nom = nom,
                    Type = type,
                    Valeur = valeur
                });
            }
        }
    }

    private void ImportHistoriqueMercenaires(XElement element, HistoriqueClassement historiqueClassement)
    {
        var mercenairesElement = element.Element(ClassementConstants.XmlElements.Mercenaires);
        if (mercenairesElement == null)
            return;

        foreach (var personnageElement in mercenairesElement.Elements(ImportExportConstants.XmlElements.Personnage))
        {
            var personnage = ParsePersonnageFromXml(personnageElement);
            if (personnage != null)
            {
                historiqueClassement.Mercenaires.Add(ConvertToPersonnageHistorique(personnage));
            }
        }
    }

    private void ImportHistoriqueCommandant(XElement element, HistoriqueClassement historiqueClassement)
    {
        var commandantElement = element.Element(PersonnageConstants.Types.Commandant);
        if (commandantElement == null)
            return;

        var personnageElement = commandantElement.Element(ImportExportConstants.XmlElements.Personnage) ?? commandantElement;
        var personnage = ParsePersonnageFromXml(personnageElement);
        if (personnage != null)
        {
            historiqueClassement.Commandant = ConvertToPersonnageHistorique(personnage);
        }
    }

    private void ImportHistoriqueAndroides(XElement element, HistoriqueClassement historiqueClassement)
    {
        var androidesElement = element.Element(SquadConstants.XmlElements.Androides);
        if (androidesElement == null)
            return;

        foreach (var personnageElement in androidesElement.Elements(ImportExportConstants.XmlElements.Personnage))
        {
            var personnage = ParsePersonnageFromXml(personnageElement);
            if (personnage != null)
            {
                historiqueClassement.Androides.Add(ConvertToPersonnageHistorique(personnage));
            }
        }
    }

    private static new PersonnageClassement ConvertToPersonnageHistorique(Personnage personnage)
    {
        return PersonnageClassement.FromPersonnage(personnage);
    }

    private static void ImportHistoriquePieces(XElement element, HistoriqueClassement historiqueClassement)
    {
        var piecesElement = element.Element(LucieHouseConstants.XmlElements.Pieces);
        if (piecesElement == null)
            return;

        foreach (var pieceElement in piecesElement.Elements(LucieHouseConstants.XmlElements.Piece))
        {
            var nom = pieceElement.Element(ImportExportConstants.XmlElements.Nom)?.Value;
            if (!string.IsNullOrWhiteSpace(nom))
            {
                var pieceHistorique = ParsePieceHistorique(pieceElement, nom);
                historiqueClassement.Pieces.Add(pieceHistorique);
            }
        }
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

            // Importer l'affection
            ImportAffection(lucieHouseElement, lucieHouse);

            var piecesElements = lucieHouseElement.Elements(LucieHouseConstants.XmlElements.Piece);

            foreach (var pieceElement in piecesElements)
            {
                try
                {
                    var piece = ParseLuciePiece(pieceElement);
                    if (piece != null)
                    {
                        piece.Id = 0; // réinsère toujours comme nouvelle entrée
                        lucieHouse.Pieces.Add(piece);
                        importedCount++;
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"{LucieHouseConstants.ErrorMessages.ErrorImportPieceLucieHouse} {ex.Message}");
                }
            }

            // Valider que max 2 pièces sont sélectionnées
            if (lucieHouse.NombrePiecesSelectionnees > LucieHouse.MaxPiecesSelectionnees)
            {
                errors.Add(string.Format(LucieHouseConstants.ErrorMessages.WarningTooManyLucieHousePieces, LucieHouse.MaxPiecesSelectionnees));
            }

            // Sauvegarder dans la base de données
            var existingLucieHouse = await _context.LucieHouses.Include(l => l.Pieces).OrderBy(l => l.Id).FirstOrDefaultAsync();
            if (existingLucieHouse != null)
            {
                _context.LucieHouses.Remove(existingLucieHouse);
            }

            _context.LucieHouses.Add(lucieHouse);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            errors.Add($"{LucieHouseConstants.ErrorMessages.ErrorImportLucieHouse} {ex.Message}");
        }

        return importedCount;
    }

    private static void ImportAffection(XElement lucieHouseElement, LucieHouse lucieHouse)
    {
        var affectionStr = lucieHouseElement.Element(LucieHouseConstants.XmlElements.Affection)?.Value;
        if (!string.IsNullOrWhiteSpace(affectionStr) && int.TryParse(affectionStr, out var affection))
        {
            lucieHouse.Affection = affection;
        }
    }

    private Personnage ImportOrUpdatePersonnage(Personnage nouveauPersonnage)
    {
        var normalizedName = NormalizeUpper(nouveauPersonnage.Nom);
        var resolvedCapacites = ResolveCapacites(nouveauPersonnage.Capacites);

        Personnage? existing = null;

        if (nouveauPersonnage.Id > 0)
        {
            existing = _context.Personnages
                .Include(p => p.Capacites)
                .FirstOrDefault(p => p.Id == nouveauPersonnage.Id);
        }

        existing ??= _context.Personnages
            .Include(p => p.Capacites)
            .FirstOrDefault(p => p.Nom.ToUpper() == normalizedName);

        if (existing != null)
        {
            // Mettre à jour le personnage existant
            existing.Nom = normalizedName;
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
            existing.HasRelation = nouveauPersonnage.HasRelation;
            existing.NivRelation = nouveauPersonnage.NivRelation;

            existing.Capacites.Clear();
            foreach (var capacite in resolvedCapacites)
            {
                existing.Capacites.Add(capacite);
            }

            _context.Personnages.Update(existing);
            _context.SaveChanges();
            return existing;
        }
        else
        {
            // Ajouter le nouveau personnage
            nouveauPersonnage.Nom = normalizedName;
            nouveauPersonnage.Id = 0; // laisser EF générer l'identité
            nouveauPersonnage.Capacites = resolvedCapacites;
            _context.Personnages.Add(nouveauPersonnage);
            _context.SaveChanges();
            return nouveauPersonnage;
        }

    }

    /// <summary>
    /// Importe les capacités au format PML
    /// </summary>
    public async Task<ImportResult> ImportCapacitesAsync(Stream pmlStream, string fileName = "")
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
                result.Error = "Le fichier est vide ou invalide";
                return result;
            }

            // Trouver la section Capacites
            var capacitesElements = doc.Root.Elements("Capacites");
            if (!capacitesElements.Any())
            {
                result.Error = "Aucune section Capacites trouvée dans le fichier";
                return result;
            }

            var capacitesElement = capacitesElements.First();
            var capaciteElements = capacitesElement.Elements("Capacite");

            var importedCount = 0;
            foreach (var capaciteElement in capaciteElements)
            {
                try
                {
                    var nom = capaciteElement.Element("Nom")?.Value?.Trim();
                    if (string.IsNullOrWhiteSpace(nom))
                    {
                        errors.Add("Une capacité sans nom a été ignorée");
                        continue;
                    }

                    var id = int.TryParse(capaciteElement.Attribute(ImportExportConstants.XmlElements.Id)?.Value, out var parsedId) ? parsedId : 0;
                    var description = capaciteElement.Element("Description")?.Value ?? "";
                    var icon = capaciteElement.Element("Icon")?.Value ?? "";

                    // Vérifier si la capacité existe déjà (id prioritaire, sinon nom)
                    var existing = id > 0
                        ? await _context.Capacites.FirstOrDefaultAsync(c => c.Id == id)
                        : null;

                    existing ??= await _context.Capacites.FirstOrDefaultAsync(c => c.Nom == nom);
                    if (existing != null)
                    {
                        // Mettre à jour
                        existing.Description = description;
                        existing.Icon = icon;
                    }
                    else
                    {
                        // Créer
                        _context.Capacites.Add(new Capacite
                        {
                            Nom = nom,
                            Description = description,
                            Icon = icon
                        });
                    }

                    importedCount++;
                }
                catch (Exception ex)
                {
                    errors.Add($"Erreur lors de l'import d'une capacité: {ex.Message}");
                }
            }

            await _context.SaveChangesAsync();
            result.IsSuccess = true;
            result.SuccessCount = importedCount;
            result.Errors = errors;
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.Error = $"Erreur lors de l'import des capacités: {ex.Message}";
        }

        return result;
    }
}






