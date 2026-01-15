using CharacterManager.Server.Models;
using CharacterManager.Server.Data;
using CharacterManager.Server.Constants;
using Microsoft.EntityFrameworkCore;
using System.Xml.Linq;
using System.Globalization;

namespace CharacterManager.Server.Services;

/// <summary>
/// Service pour importer les données au format PML (XML personnalisé)
/// Extension .pml pour les fichiers d'import
/// Supporte les sections : HistoriqueClassements, inventaire, template
/// </summary>
public class PmlImportService(ApplicationDbContext context) : PmlServiceBase(context)
{
    /// <summary>
    /// Importe les données du format PML (inventaire, templates, etc.)
    /// </summary>
    public async Task<ImportResult> ImportPmlAsync(Stream pmlStream, string fileName = "",
        bool importInventory = true, bool importTemplates = true,
        bool importBestSquad = true, bool importHistories = true, bool importLeagueHistory = false)
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
                result.Error = AppConstants.Messages.ErrorFileEmpty + " ou invalide";
                return result;
            }

            result.SuccessCount += await ProcessImportSections(doc.Root, importInventory, importTemplates, importBestSquad, importHistories, importLeagueHistory, errors);

            result.Errors = errors;

            if (result.SuccessCount == 0 && string.IsNullOrEmpty(result.Error))
            {
                result.Error = AppConstants.Messages.ErrorNoSectionsFound;
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
            result.Error = $"{AppConstants.Messages.ErrorXmlParsing}: {ex.Message}";
            result.IsSuccess = false;
        }

        return result;
    }

    /// <summary>
    /// Traite toutes les sections d'import
    /// </summary>
    private async Task<int> ProcessImportSections(XElement root, bool importInventory, bool importTemplates,
        bool importBestSquad, bool importHistories, bool importLeagueHistory, List<string> errors)
    {
        int totalCount = 0;

        if (importInventory)
        {
            totalCount += await ProcessInventorySection(root, errors);
        }

        if (importTemplates)
        {
            totalCount += await ProcessTemplatesSection(root, errors);
        }

        if (importBestSquad)
        {
            totalCount += await ProcessBestSquadSection(root, errors);
        }

        if (importHistories)
        {
            totalCount += await ProcessHistoriesSection(root, errors);
        }

        if (importLeagueHistory)
        {
            totalCount += await ProcessLeagueHistorySection(root, errors);
        }

        return totalCount;
    }

    private async Task<int> ProcessInventorySection(XElement root, List<string> errors)
    {
        var inventaireElements = root.Elements(AppConstants.XmlElements.Inventaire);
        if (!inventaireElements.Any() && root.Name.LocalName.Equals(AppConstants.XmlElements.Inventaire, StringComparison.OrdinalIgnoreCase))
        {
            inventaireElements = [root];
        }

        return inventaireElements.Any() ? await ImportInventaireAsync(inventaireElements, errors) : 0;
    }

    private async Task<int> ProcessTemplatesSection(XElement root, List<string> errors)
    {
        var directTemplates = root.Elements(AppConstants.XmlElements.Template);
        var nestedTemplates = root.Element(AppConstants.XmlElements.Templates)?.Elements(AppConstants.XmlElements.Template) ?? Enumerable.Empty<XElement>();
        var templateElements = directTemplates.Concat(nestedTemplates);

        return templateElements.Any() ? await ImportTemplatesAsync(templateElements, errors) : 0;
    }

    private async Task<int> ProcessBestSquadSection(XElement root, List<string> errors)
    {
        var bestSquadElements = root.Elements(AppConstants.XmlElements.MeilleurEscouade);
        return bestSquadElements.Any() ? await ImportBestSquadAsync(bestSquadElements, errors) : 0;
    }

    private async Task<int> ProcessHistoriesSection(XElement root, List<string> errors)
    {
        var historiqueClassementElements = root.Elements(AppConstants.XmlElements.HistoriqueClassement);
        return historiqueClassementElements.Any() ? await ImportHistoriquesClassementAsync(historiqueClassementElements, errors) : 0;
    }

    private async Task<int> ProcessLeagueHistorySection(XElement root, List<string> errors)
    {
        var historiqueLigueElements = root.Elements(AppConstants.XmlElements.HistoriqueLigue);
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
            var personnageElements = inventaire.Name.LocalName.Equals(AppConstants.XmlElements.Personnage, StringComparison.OrdinalIgnoreCase)
                ? [inventaire]
                : inventaire.Elements(AppConstants.XmlElements.Personnage);

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
                    errors.Add($"{AppConstants.Messages.ErrorImportPersonnageInventaire} {ex.Message}");
                }
            }

            // Traiter la section Lucie House
            var lucieHouseElement = inventaire.Name.LocalName.Equals(AppConstants.XmlElements.LucieHouse, StringComparison.OrdinalIgnoreCase)
                ? new[] { inventaire }
                : inventaire.Elements(AppConstants.XmlElements.LucieHouse);

            foreach (var lucieElement in lucieHouseElement)
            {
                importedCount += await ImportLucieHouseAsync(lucieElement, errors);
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
                var templateName = template.Element(AppConstants.XmlElements.Nom)?.Value;
                var description = template.Element(AppConstants.XmlElements.Description)?.Value;

                if (string.IsNullOrWhiteSpace(templateName))
                {
                    errors.Add(AppConstants.Messages.ErrorTemplateNoName);
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
                errors.Add($"{AppConstants.Messages.ErrorImportTemplate} {ex.Message}");
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
        var personnagesElements = template.Elements(AppConstants.XmlElements.Personnage);

        foreach (var personElement in personnagesElements)
        {
            try
            {
                var nom = personElement.Element(AppConstants.XmlElements.Nom)?.Value;
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
                errors.Add($"{AppConstants.Messages.ErrorImportPersonnageTemplate} '{templateName}': {ex.Message}");
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
                errors.Add($"{AppConstants.Messages.ErrorImportBestSquad} {ex.Message}");
            }
        }

        return Task.FromResult(importedCount);
    }

    private int ImportBestSquadMercenaires(XElement bestSquad)
    {
        int count = 0;
        var mercenairesElements = bestSquad.Elements(AppConstants.XmlElements.Mercenaire);
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
        var commandantElement = bestSquad.Element(AppConstants.XmlElements.Commandant);
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
        var androidesElements = bestSquad.Elements(AppConstants.XmlElements.Androide);
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
                var dateStr = historiqueLigueElement.Element(AppConstants.XmlElements.DateMontee)?.Value;
                var ligueStr = historiqueLigueElement.Element(AppConstants.XmlElements.Ligue)?.Value;
                var notes = historiqueLigueElement.Element(AppConstants.XmlElements.Notes)?.Value;

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
    /// </summary>
    private async Task<int> ImportHistoriquesClassementAsync(IEnumerable<XElement> historiqueClassementElements, List<string> errors)
    {
        int importedCount = 0;

        foreach (var historiqueClassementElement in historiqueClassementElements)
        {
            try
            {
                var historiqueClassement = ParseHistoriqueClassementHeader(historiqueClassementElement, errors);
                if (historiqueClassement == null)
                    continue;

                ImportHistoriqueClassements(historiqueClassementElement, historiqueClassement);
                ImportHistoriqueMercenaires(historiqueClassementElement, historiqueClassement);
                ImportHistoriqueCommandant(historiqueClassementElement, historiqueClassement);
                ImportHistoriqueAndroides(historiqueClassementElement, historiqueClassement);
                ImportHistoriquePieces(historiqueClassementElement, historiqueClassement);

                _context.HistoriquesClassement.Add(historiqueClassement);
                await _context.SaveChangesAsync();
                importedCount++;
            }
            catch (Exception ex)
            {
                errors.Add($"Erreur lors de l'import d'un historique de classement: {ex.Message}");
            }
        }

        return importedCount;
    }

    private HistoriqueClassement? ParseHistoriqueClassementHeader(XElement element, List<string> errors)
    {
        var dateStr = element.Element(AppConstants.XmlElements.DateEnregistrement)?.Value;

        if (string.IsNullOrWhiteSpace(dateStr))
        {
            errors.Add("Historique de classement invalide: date manquante");
            return null;
        }

        if (!DateOnly.TryParse(dateStr, CultureInfo.InvariantCulture, out var dateEnregistrement))
        {
            errors.Add($"Date d'enregistrement invalide: {dateStr}");
            return null;
        }

        var ligueStr = element.Element(AppConstants.XmlElements.Ligue)?.Value;
        var scoreStr = element.Element(AppConstants.XmlElements.Score)?.Value;
        var puissanceTotalStr = element.Element(AppConstants.XmlElements.PuissanceTotal)?.Value;
        var puissanceCommandantStr = element.Element(AppConstants.XmlElements.PuissanceCommandant)?.Value;
        var puissanceMercenairesStr = element.Element(AppConstants.XmlElements.PuissanceMercenaires)?.Value;
        var puissanceLucieStr = element.Element(AppConstants.XmlElements.PuissanceLucie)?.Value;

        return new HistoriqueClassement
        {
            DateEnregistrement = dateEnregistrement,
            Ligue = int.TryParse(ligueStr, out var ligue) ? ligue : 0,
            Score = int.TryParse(scoreStr, out var score) ? score : 0,
            PuissanceTotale = int.TryParse(puissanceTotalStr, out var puissanceTotal) ? puissanceTotal : 0,
            PuissanceCommandant = int.TryParse(puissanceCommandantStr, out var puissanceCommandant) ? puissanceCommandant : 0,
            PuissanceMercenaires = int.TryParse(puissanceMercenairesStr, out var puissanceMercenaires) ? puissanceMercenaires : 0,
            PuissanceLucie = int.TryParse(puissanceLucieStr, out var puissanceLucie) ? puissanceLucie : 0
        };
    }

    private void ImportHistoriqueClassements(XElement element, HistoriqueClassement historiqueClassement)
    {
        var classementsElement = element.Element(AppConstants.XmlElements.Classements);
        if (classementsElement == null)
            return;

        foreach (var classementElement in classementsElement.Elements(AppConstants.XmlElements.ClassementItem))
        {
            var nom = classementElement.Element(AppConstants.XmlElements.Nom)?.Value ?? "";
            var typeStr = classementElement.Element(AppConstants.XmlElements.TypeClassement)?.Value;
            var valeurStr = classementElement.Element(AppConstants.XmlElements.Valeur)?.Value;

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
        var mercenairesElement = element.Element(AppConstants.XmlElements.Mercenaires);
        if (mercenairesElement == null)
            return;

        foreach (var personnageElement in mercenairesElement.Elements(AppConstants.XmlElements.Personnage))
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
        var commandantElement = element.Element(AppConstants.XmlElements.Commandant);
        if (commandantElement == null)
            return;

        var personnageElement = commandantElement.Element(AppConstants.XmlElements.Personnage) ?? commandantElement;
        var personnage = ParsePersonnageFromXml(personnageElement);
        if (personnage != null)
        {
            historiqueClassement.Commandant = ConvertToPersonnageHistorique(personnage);
        }
    }

    private void ImportHistoriqueAndroides(XElement element, HistoriqueClassement historiqueClassement)
    {
        var androidesElement = element.Element(AppConstants.XmlElements.Androides);
        if (androidesElement == null)
            return;

        foreach (var personnageElement in androidesElement.Elements(AppConstants.XmlElements.Personnage))
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
        var piecesElement = element.Element(AppConstants.XmlElements.Pieces);
        if (piecesElement == null)
            return;

        foreach (var pieceElement in piecesElement.Elements(AppConstants.XmlElements.Piece))
        {
            var nom = pieceElement.Element(AppConstants.XmlElements.Nom)?.Value;
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

            var piecesElements = lucieHouseElement.Elements(AppConstants.XmlElements.Piece);

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
                    errors.Add($"{AppConstants.Messages.ErrorImportPieceLucieHouse} {ex.Message}");
                }
            }

            // Valider que max 2 pièces sont sélectionnées
            if (lucieHouse.NombrePiecesSelectionnees > LucieHouse.MaxPiecesSelectionnees)
            {
                errors.Add(string.Format(AppConstants.Messages.WarningTooManyLucieHousePieces, LucieHouse.MaxPiecesSelectionnees));
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
            errors.Add($"{AppConstants.Messages.ErrorImportLucieHouse} {ex.Message}");
        }

        return importedCount;
    }

    private static void ImportAffection(XElement lucieHouseElement, LucieHouse lucieHouse)
    {
        var affectionStr = lucieHouseElement.Element(AppConstants.XmlElements.Affection)?.Value;
        if (!string.IsNullOrWhiteSpace(affectionStr) && int.TryParse(affectionStr, out var affection))
        {
            lucieHouse.Affection = affection;
        }
    }

    private void ImportOrUpdatePersonnage(Personnage nouveauPersonnage)
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
            .FirstOrDefault(p => p.Nom == normalizedName);

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
        }
        else
        {
            // Ajouter le nouveau personnage
            nouveauPersonnage.Nom = normalizedName;
            nouveauPersonnage.Id = 0; // laisser EF générer l'identité
            nouveauPersonnage.Capacites = resolvedCapacites;
            _context.Personnages.Add(nouveauPersonnage);
        }

        _context.SaveChanges();
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

                    var id = int.TryParse(capaciteElement.Attribute(AppConstants.XmlElements.Id)?.Value, out var parsedId) ? parsedId : 0;
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
