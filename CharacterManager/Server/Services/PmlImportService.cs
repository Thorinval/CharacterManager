using CharacterManager.Server.Models;
using CharacterManager.Server.Data;
using CharacterManager.Server.Constants;
using Microsoft.EntityFrameworkCore;
using System.Xml.Linq;
using System.Text;
using System.Globalization;
using System.Xml;

namespace CharacterManager.Server.Services;

/// <summary>
/// Service pour importer/exporter les données au format PML (XML personnalisé)
/// Extension .pml pour les fichiers d'import/export
/// Supporte les sections : HistoriqueClassements, inventaire, template
/// </summary>
public class PmlImportService(ApplicationDbContext context)
{
    private readonly ApplicationDbContext _context = context;

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

    private static PersonnageHistorique ConvertToPersonnageHistorique(Personnage personnage)
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

    private static PieceHistorique ParsePieceHistorique(XElement pieceElement, string nom)
    {
        var pieceHistorique = new PieceHistorique
        {
            Nom = nom,
            Niveau = int.TryParse(pieceElement.Element(AppConstants.XmlElements.Niveau)?.Value, out var niveau) ? niveau : 1,
            Selectionnee = ParseBool(pieceElement.Element(AppConstants.XmlElements.Selectionne)?.Value),
            IdOrigine = int.TryParse(pieceElement.Attribute(AppConstants.XmlElements.Id)?.Value, out var parsedId) ? parsedId : 0
        };

        if (int.TryParse(pieceElement.Element(AppConstants.XmlElements.PuissanceTactique)?.Value, out var pTact))
        {
            pieceHistorique.AspectsTactiques.Puissance = pTact;
        }

        if (int.TryParse(pieceElement.Element(AppConstants.XmlElements.PuissanceStrategique)?.Value, out var pStrat))
        {
            pieceHistorique.AspectsStrategiques.Puissance = pStrat;
        }

        return pieceHistorique;
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

    private Piece? ParseLuciePiece(XElement pieceElement)
    {
        var nom = pieceElement.Element(AppConstants.XmlElements.Nom)?.Value;
        if (string.IsNullOrWhiteSpace(nom))
            return null;

        var piece = new Piece
        {
            Nom = nom,
            Niveau = int.TryParse(pieceElement.Element(AppConstants.XmlElements.Niveau)?.Value, out var niveau) ? niveau : 1,
            Selectionnee = ParseBool(pieceElement.Element(AppConstants.XmlElements.Selectionne)?.Value)
        };

        ParseLuciePieceBonus(pieceElement, piece);
        ParseLuciePiecePuissance(pieceElement, piece);

        return piece;
    }

    private static void ParseLuciePieceBonus(XElement pieceElement, Piece piece)
    {
        // Parser les bonus tactiques
        var bonusTactiquesElement = pieceElement.Element(AppConstants.XmlElements.BonusTactiques);
        if (bonusTactiquesElement != null)
        {
            piece.AspectsTactiques.Bonus = [.. bonusTactiquesElement.Elements(AppConstants.XmlElements.Bonus)
                .Select(b => b.Value)
                .Where(b => !string.IsNullOrWhiteSpace(b))];
            piece.AspectsTactiques.Puissance = piece.AspectsTactiques.Bonus.Count;
        }

        // Parser les bonus stratégiques
        var bonusStrategiquesElement = pieceElement.Element(AppConstants.XmlElements.BonusStrategiques);
        if (bonusStrategiquesElement != null)
        {
            piece.AspectsStrategiques.Bonus = bonusStrategiquesElement.Elements(AppConstants.XmlElements.Bonus)
                .Select(b => b.Value)
                .Where(b => !string.IsNullOrWhiteSpace(b))
                .ToList();
            piece.AspectsStrategiques.Puissance = piece.AspectsStrategiques.Bonus.Count;
        }
    }

    private static void ParseLuciePiecePuissance(XElement pieceElement, Piece piece)
    {
        // Puissance tactiques et stratégiques (nouveau format). Fallback: ancienne balise "Puissance" alimente les tactiques.
        if (int.TryParse(pieceElement.Element(AppConstants.XmlElements.PuissanceTactique)?.Value, out var pTact))
        {
            piece.AspectsTactiques.Puissance = pTact;
        }
        else if (int.TryParse(pieceElement.Element(AppConstants.XmlElements.PuissanceLegacy)?.Value, out var pLegacy))
        {
            piece.AspectsTactiques.Puissance = pLegacy;
        }

        if (int.TryParse(pieceElement.Element(AppConstants.XmlElements.PuissanceStrategique)?.Value, out var pStrat))
        {
            piece.AspectsStrategiques.Puissance = pStrat;
        }
    }

    /// <summary>
    /// Parse un personnage depuis un élément XML
    /// </summary>
    private Personnage? ParsePersonnageFromXml(XElement element)
    {
        var nom = element.Element(AppConstants.XmlElements.Nom)?.Value;
        if (string.IsNullOrWhiteSpace(nom))
            return null;

        var id = int.TryParse(element.Attribute(AppConstants.XmlElements.Id)?.Value, out var parsedId) ? parsedId : 0;

        var personnage = new Personnage
        {
            Id = id,
            Nom = nom.Trim(),
            Rarete = ParseRarete(element.Element(AppConstants.XmlElements.Rarete)?.Value),
            Type = ParseType(element.Element(AppConstants.XmlElements.Type)?.Value),
            Puissance = int.TryParse(element.Element(AppConstants.XmlElements.Puissance)?.Value, out var p) ? p : 0,
            PA = int.TryParse(element.Element(AppConstants.XmlElements.PA)?.Value, out var pa) ? pa : 0,
            PV = int.TryParse(element.Element(AppConstants.XmlElements.PV)?.Value, out var pv) ? pv : 0,
            Niveau = int.TryParse(element.Element(AppConstants.XmlElements.Niveau)?.Value, out var n) ? n : 1,
            Rang = int.TryParse(element.Element(AppConstants.XmlElements.Rang)?.Value, out var r) ? r : 1,
            Role = ParseRole(element.Element(AppConstants.XmlElements.Role)?.Value),
            Faction = ParseFaction(element.Element(AppConstants.XmlElements.Faction)?.Value),
            TypeAttaque = ParseTypeAttaque(element.Element(AppConstants.XmlElements.TypeAttaque)?.Value),
            Selectionne = ParseBool(element.Element(AppConstants.XmlElements.Selectionne)?.Value),
            HasRelation = ParseBool(element.Element(AppConstants.XmlElements.HasRelation)?.Value),
            NivRelation = int.TryParse(element.Element(AppConstants.XmlElements.NivRelation)?.Value, out var nivRel) ? nivRel : 0,
            Capacites = ParseCapacites(element)
        };

        return personnage;
    }

    private static List<Capacite> ParseCapacites(XElement element)
    {
        var capacitesElement = element.Element(AppConstants.XmlElements.Capacites);
        if (capacitesElement == null)
        {
            return [];
        }

        var capacites = new List<Capacite>();

        foreach (var capaciteElement in capacitesElement.Elements(AppConstants.XmlElements.Capacite))
        {
            var nom = capaciteElement.Element(AppConstants.XmlElements.Nom)?.Value ?? capaciteElement.Value;
            if (string.IsNullOrWhiteSpace(nom))
            {
                continue;
            }

            var description = capaciteElement.Element(AppConstants.XmlElements.Description)?.Value ?? string.Empty;
            var icon = capaciteElement.Element(AppConstants.XmlElements.Icon)?.Value ?? string.Empty;

            capacites.Add(new Capacite
            {
                Nom = nom.Trim(),
                Description = description,
                Icon = icon
            });
        }

        return capacites;
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
            writer.WriteStartElement(AppConstants.XmlElements.InventairePML);
            writer.WriteAttributeString(AppConstants.XmlElements.Version, "1.0");
            writer.WriteAttributeString(AppConstants.XmlElements.ExportDate, DateTime.UtcNow.ToString(AppConstants.DateTimeFormats.IsoDateTime));

            writer.WriteStartElement(AppConstants.XmlElements.Inventaire);

            WritePersonnagesDatas(personnages, writer);

            // Export Lucie House as part of the inventory payload (no extra checkbox/UI toggle)
            var lucieHouse = _context.LucieHouses.Include(l => l.Pieces).FirstOrDefault();
            if (lucieHouse != null)
            {
                writer.WriteStartElement(AppConstants.XmlElements.LucieHouse);
                writer.WriteElementString(AppConstants.XmlElements.Affection, lucieHouse.Affection.ToString());

                foreach (var piece in lucieHouse.Pieces)
                {
                    WriteLuciePieceData(writer, piece);
                }

                writer.WriteEndElement();
            }

            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        return Task.FromResult(memoryStream.ToArray());
    }

    private static void WriteLuciePieceData(System.Xml.XmlWriter writer, Piece piece, bool avecBonus = true)
    {
        writer.WriteStartElement(AppConstants.XmlElements.Piece);
        if (piece.Id > 0)
        {
            writer.WriteAttributeString(AppConstants.XmlElements.Id, piece.Id.ToString());
        }
        writer.WriteElementString(AppConstants.XmlElements.Nom, piece.Nom);
        writer.WriteElementString(AppConstants.XmlElements.Niveau, piece.Niveau.ToString());
        writer.WriteElementString(AppConstants.XmlElements.PuissanceTactique, piece.AspectsTactiques.Puissance.ToString());
        writer.WriteElementString(AppConstants.XmlElements.PuissanceStrategique, piece.AspectsStrategiques.Puissance.ToString());
        writer.WriteElementString(AppConstants.XmlElements.Selectionne, piece.Selectionnee.ToString());

        if (avecBonus)
        {
            if (piece.AspectsTactiques.Bonus.Count > 0)
            {
                writer.WriteStartElement(AppConstants.XmlElements.BonusTactiques);
                foreach (var bonus in piece.AspectsTactiques.Bonus)
                {
                    writer.WriteElementString(AppConstants.XmlElements.Bonus, bonus);
                }
                writer.WriteEndElement();
            }

            if (piece.AspectsStrategiques.Bonus.Count > 0)
            {
                writer.WriteStartElement(AppConstants.XmlElements.BonusStrategiques);
                foreach (var bonus in piece.AspectsStrategiques.Bonus)
                {
                    writer.WriteElementString(AppConstants.XmlElements.Bonus, bonus);
                }
                writer.WriteEndElement();
            }
        }

        writer.WriteEndElement();
    }

    private static void WritePersonnagesDatas(IEnumerable<Personnage> personnages, System.Xml.XmlWriter writer)
    {
        foreach (var personnage in personnages)
        {
            writer.WriteStartElement(AppConstants.XmlElements.Personnage);
            if (personnage.Id > 0)
            {
                writer.WriteAttributeString(AppConstants.XmlElements.Id, personnage.Id.ToString());
            }
            WritePersonnageData(writer, personnage);

            writer.WriteEndElement();
        }
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
            writer.WriteStartElement(AppConstants.XmlElements.TemplatesPML);
            writer.WriteAttributeString(AppConstants.XmlElements.Version, "1.0");
            writer.WriteAttributeString(AppConstants.XmlElements.ExportDate, DateTime.UtcNow.ToString(AppConstants.DateTimeFormats.IsoDateTime));

            foreach (var template in templates)
            {
                writer.WriteStartElement(AppConstants.XmlElements.Template);
                writer.WriteElementString(AppConstants.XmlElements.Nom, template.Nom);
                writer.WriteElementString(AppConstants.XmlElements.Description, template.Description ?? "");

                // Récupérer les personnages du template via les IDs stockés
                var personnageIds = template.GetPersonnageIds();
                foreach (var personnageId in personnageIds)
                {
                    var personnage = _context.Personnages
                        .Include(p => p.Capacites)
                        .FirstOrDefault(p => p.Id == personnageId);
                    if (personnage != null)
                    {
                        writer.WriteStartElement(AppConstants.XmlElements.Personnage);
                        if (personnage.Id > 0)
                        {
                            writer.WriteAttributeString(AppConstants.XmlElements.Id, personnage.Id.ToString());
                        }
                        WritePersonnageData(writer, personnage, isTemplate: true);
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
    public async Task<byte[]> ExportPmlAsync(PmlExportOptions? options = null)
    {
        // Rétro-compatibilité: créer des options par défaut
        options ??= new PmlExportOptions(
            PmlExportOptions.EXPORT_TYPE_INVENTORY,
            PmlExportOptions.EXPORT_TYPE_TEMPLATES,
            PmlExportOptions.EXPORT_TYPE_BEST_SQUAD
        );

        var settings = new System.Xml.XmlWriterSettings
        {
            Indent = true,
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
        };

        using var memoryStream = new MemoryStream();
        using (var writer = System.Xml.XmlWriter.Create(memoryStream, settings))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement(AppConstants.XmlElements.CharacterManagerPML);
            writer.WriteAttributeString(AppConstants.XmlElements.Version, "1.0");
            writer.WriteAttributeString(AppConstants.XmlElements.ExportDate, DateTime.UtcNow.ToString(AppConstants.DateTimeFormats.IsoDateTime));

            // Export inventaire
            await ExporterInventairePmlAsync(options, writer);

            // Export templates
            await ExporterTemplatesPmlAsync(options, writer);

            // Export meilleure escouade
            await ExporterBestSquadPmlAsync(options, writer);

            // Export historiques de classement
            await ExporterHistoriquesClassementPmlAsync(options, writer);

            // Export historiques de ligue
            await ExporterHistoriquesLiguePmlAsync(options, writer);

            // Export capacités
            await ExporterCapacitesPmlAsync(options, writer);

            writer.WriteEndDocument();
        }

        var bytes = memoryStream.ToArray();
        await SaveLastExportDate();
        return bytes;
    }

    private async Task ExporterHistoriquesLiguePmlAsync(PmlExportOptions options, System.Xml.XmlWriter writer)
    {
        if (options.IsExporting(PmlExportOptions.EXPORT_TYPE_LEAGUE_HISTORY))
        {
            var historiquesLigue = await _context.HistoriquesLigue
                .OrderByDescending(h => h.DateMontee)
                .Take(100)
                .ToListAsync();

            foreach (var historiqueLigue in historiquesLigue)
            {
                writer.WriteStartElement(AppConstants.XmlElements.HistoriqueLigue);
                writer.WriteElementString(AppConstants.XmlElements.DateMontee, historiqueLigue.DateMontee.ToString("yyyy-MM-dd"));
                writer.WriteElementString(AppConstants.XmlElements.Ligue, historiqueLigue.Ligue.ToString());
                if (!string.IsNullOrWhiteSpace(historiqueLigue.Notes))
                {
                    writer.WriteElementString(AppConstants.XmlElements.Notes, historiqueLigue.Notes);
                }
                writer.WriteEndElement();
            }
        }
    }

    private async Task ExporterCapacitesPmlAsync(PmlExportOptions options, System.Xml.XmlWriter writer)
    {
        if (options.IsExporting(PmlExportOptions.EXPORT_TYPE_CAPACITES))
        {
            var capacites = await _context.Capacites.ToListAsync();
            if (capacites.Any())
            {
                writer.WriteStartElement("Capacites");
                foreach (var capacite in capacites)
                {
                    writer.WriteStartElement("Capacite");
                    if (capacite.Id > 0)
                    {
                        writer.WriteAttributeString(AppConstants.XmlElements.Id, capacite.Id.ToString());
                    }
                    writer.WriteElementString("Nom", capacite.Nom);
                    writer.WriteElementString("Description", capacite.Description ?? "");
                    writer.WriteElementString("Icon", capacite.Icon ?? "");
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
            }
        }

        writer.WriteEndElement();
    }

    private static void ExportListClassements(System.Xml.XmlWriter writer, List<Classement> listClassements)
    {
        if (listClassements.Count != 0)
        {
            writer.WriteStartElement(AppConstants.XmlElements.Classements);
            foreach (var classement in listClassements)
            {
                writer.WriteStartElement(AppConstants.XmlElements.ClassementItem);
                writer.WriteElementString(AppConstants.XmlElements.Nom, classement.Nom);
                writer.WriteElementString(AppConstants.XmlElements.TypeClassement, classement.Type.ToString());
                writer.WriteElementString(AppConstants.XmlElements.Valeur, classement.Valeur.ToString());
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }
    }

    private async Task ExporterHistoriquesClassementPmlAsync(PmlExportOptions options, System.Xml.XmlWriter writer)
    {
        if (options.IsExporting(PmlExportOptions.EXPORT_TYPE_HISTORIES))
        {
            // Export historiques de classement (version structurée complète)
            var historiquesClassement = await _context.HistoriquesClassement
                .Include(h => h.Mercenaires)
                .Include(h => h.Commandant)
                .Include(h => h.Androides)
                .Include(h => h.Pieces)
                .Include(h => h.Classements)
                .OrderByDescending(h => h.DateEnregistrement)
                .Take(50)
                .ToListAsync();

            foreach (var historiqueClassement in historiquesClassement)
            {
                ExportHistoriqueClassementElement(writer, historiqueClassement);
            }
        }
    }

    private static void ExportHistoriqueClassementElement(System.Xml.XmlWriter writer, HistoriqueClassement historiqueClassement)
    {
        writer.WriteStartElement(AppConstants.XmlElements.HistoriqueClassement);
        writer.WriteElementString(AppConstants.XmlElements.DateEnregistrement, historiqueClassement.DateEnregistrement.ToString("yyyy-MM-dd"));
        writer.WriteElementString(AppConstants.XmlElements.Ligue, historiqueClassement.Ligue.ToString());
        writer.WriteElementString(AppConstants.XmlElements.Score, historiqueClassement.Score.ToString());
        writer.WriteElementString(AppConstants.XmlElements.PuissanceTotal, historiqueClassement.PuissanceTotale.ToString());
        writer.WriteElementString(AppConstants.XmlElements.PuissanceCommandant, historiqueClassement.PuissanceCommandant.ToString());
        writer.WriteElementString(AppConstants.XmlElements.PuissanceMercenaires, historiqueClassement.PuissanceMercenaires.ToString());
        writer.WriteElementString(AppConstants.XmlElements.PuissanceLucie, historiqueClassement.PuissanceLucie.ToString());

        // Export des classements
        ExportListClassements(writer, historiqueClassement.Classements);

        // Export des mercenaires
        ExportListMercenaires(writer, historiqueClassement.Mercenaires);

        // Export du commandant
        ExportHistoriqueCommandantElement(writer, historiqueClassement.Commandant);

        // Export des androïdes
        ExportListAndroides(writer, historiqueClassement.Androides);

        // Export des pièces
        ExportHistoriquePiecesElement(writer, historiqueClassement.Pieces);

        writer.WriteEndElement();
    }

    private static void ExportHistoriqueCommandantElement(System.Xml.XmlWriter writer, PersonnageHistorique? commandant)
    {
        if (commandant != null)
        {
            writer.WriteStartElement(AppConstants.XmlElements.Commandant);
            if (commandant.Id > 0)
            {
                writer.WriteAttributeString(AppConstants.XmlElements.Id, commandant.Id.ToString());
            }
            WritePersonnageData(writer, commandant);
            writer.WriteEndElement();
        }
    }

    private static void ExportHistoriquePiecesElement(System.Xml.XmlWriter writer, List<PieceHistorique> pieces)
    {
        if (pieces.Count != 0)
        {
            writer.WriteStartElement(AppConstants.XmlElements.Pieces);
            foreach (var piece in pieces)
            {
                WriteLuciePieceData(writer, piece, avecBonus: false);
            }
            writer.WriteEndElement();
        }
    }

    private static void ExportListAndroides(XmlWriter writer, List<PersonnageHistorique> listAndroides)
    {
        if (listAndroides.Count != 0)
        {
            writer.WriteStartElement(AppConstants.XmlElements.Androides);
            foreach (var androide in listAndroides)
            {
                writer.WriteStartElement(AppConstants.XmlElements.Personnage);
                if (androide.Id > 0)
                {
                    writer.WriteAttributeString(AppConstants.XmlElements.Id, androide.Id.ToString());
                }
                WritePersonnageData(writer, androide);
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }
    }

    private static void ExportListMercenaires(XmlWriter writer, List<PersonnageHistorique> listMercenaires)
    {
        if (listMercenaires.Count != 0)
        {
            writer.WriteStartElement(AppConstants.XmlElements.Mercenaires);
            foreach (var mercenaire in listMercenaires)
            {
                writer.WriteStartElement(AppConstants.XmlElements.Personnage);
                if (mercenaire.Id > 0)
                {
                    writer.WriteAttributeString(AppConstants.XmlElements.Id, mercenaire.Id.ToString());
                }
                WritePersonnageData(writer, mercenaire);
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }
    }

    private async Task ExporterBestSquadPmlAsync(PmlExportOptions options, System.Xml.XmlWriter writer)
    {
        if (options.IsExporting(PmlExportOptions.EXPORT_TYPE_BEST_SQUAD))
        {
            writer.WriteStartElement(AppConstants.XmlElements.MeilleurEscouade);

            await ExportBestSquadMercenairesAsync(writer);
            await ExportBestSquadCommandantAsync(writer);
            await ExportBestSquadAndroidesAsync(writer);

            writer.WriteEndElement();
        }
    }

    private async Task ExportBestSquadMercenairesAsync(System.Xml.XmlWriter writer)
    {
        var topMercenaires = await _context.Personnages
            .Include(p => p.Capacites)
            .Where(p => p.Type == TypePersonnage.Mercenaire)
            .OrderByDescending(p => p.Puissance)
            .Take(10)
            .ToListAsync();

        foreach (var merc in topMercenaires)
        {
            writer.WriteStartElement(AppConstants.XmlElements.Mercenaire);
            if (merc.Id > 0)
            {
                writer.WriteAttributeString(AppConstants.XmlElements.Id, merc.Id.ToString());
            }
            WritePersonnageData(writer, merc);
            writer.WriteEndElement();
        }
    }

    private async Task ExportBestSquadCommandantAsync(System.Xml.XmlWriter writer)
    {
        var topCommandant = await _context.Personnages
            .Include(p => p.Capacites)
            .Where(p => p.Type == TypePersonnage.Commandant)
            .OrderByDescending(p => p.Puissance)
            .FirstOrDefaultAsync();

        if (topCommandant != null)
        {
            writer.WriteStartElement(AppConstants.XmlElements.Commandant);
            if (topCommandant.Id > 0)
            {
                writer.WriteAttributeString(AppConstants.XmlElements.Id, topCommandant.Id.ToString());
            }
            WritePersonnageData(writer, topCommandant);
            writer.WriteEndElement();
        }
    }

    private async Task ExportBestSquadAndroidesAsync(System.Xml.XmlWriter writer)
    {
        var topAndroides = await _context.Personnages
            .Include(p => p.Capacites)
            .Where(p => p.Type == TypePersonnage.Androide)
            .OrderByDescending(p => p.Puissance)
            .Take(5)
            .ToListAsync();

        foreach (var android in topAndroides)
        {
            writer.WriteStartElement(AppConstants.XmlElements.Androide);
            if (android.Id > 0)
            {
                writer.WriteAttributeString(AppConstants.XmlElements.Id, android.Id.ToString());
            }
            WritePersonnageData(writer, android);
            writer.WriteEndElement();
        }
    }

    private async Task ExporterTemplatesPmlAsync(PmlExportOptions options, System.Xml.XmlWriter writer)
    {
        if (options.IsExporting(PmlExportOptions.EXPORT_TYPE_TEMPLATES))
        {
            var templates = await _context.Templates.ToListAsync();
            foreach (var template in templates)
            {
                writer.WriteStartElement(AppConstants.XmlElements.Template);
                writer.WriteElementString(AppConstants.XmlElements.Nom, template.Nom);
                writer.WriteElementString(AppConstants.XmlElements.Description, template.Description ?? "");

                var personnageIds = template.GetPersonnageIds();
                foreach (var personnageId in personnageIds)
                {
                    var personnage = await _context.Personnages
                        .Include(p => p.Capacites)
                        .FirstOrDefaultAsync(p => p.Id == personnageId);
                    if (personnage != null)
                    {
                        writer.WriteStartElement(AppConstants.XmlElements.Personnage);
                        if (personnage.Id > 0)
                        {
                            writer.WriteAttributeString(AppConstants.XmlElements.Id, personnage.Id.ToString());
                        }
                        WritePersonnageData(writer, personnage, isTemplate: true);
                        writer.WriteEndElement();
                    }
                }
                writer.WriteEndElement();
            }
        }
    }

    private async Task ExporterInventairePmlAsync(PmlExportOptions options, System.Xml.XmlWriter writer)
    {
        if (options.IsExporting(PmlExportOptions.EXPORT_TYPE_INVENTORY))
        {
            writer.WriteStartElement(AppConstants.XmlElements.Inventaire);

            await WritePersonnageDatas(writer);

            // Export Lucie House
            await WriteLucieHouseDatas(writer);
        }
    }

    private async Task WriteLucieHouseDatas(System.Xml.XmlWriter writer)
    {
        var lucieHouse = await _context.LucieHouses.Include(l => l.Pieces).FirstOrDefaultAsync();
        if (lucieHouse != null)
        {
            writer.WriteStartElement(AppConstants.XmlElements.LucieHouse);
            writer.WriteElementString(AppConstants.XmlElements.Affection, lucieHouse.Affection.ToString());

            foreach (var piece in lucieHouse.Pieces)
            {
                WriteLuciePieceData(writer, piece);
            }

            writer.WriteEndElement();
        }
    }

    private async Task WritePersonnageDatas(System.Xml.XmlWriter writer)
    {
        var personnages = await _context.Personnages
            .Include(static p => p.Capacites)
            .ToListAsync();
        WritePersonnagesDatas(personnages, writer);
    }

    /// <summary>
    /// Helper method to write personnage data to XML
    /// </summary>
    private static void WritePersonnageData(System.Xml.XmlWriter writer, Personnage personnage, bool isTemplate = false)
    {
        writer.WriteElementString(AppConstants.XmlElements.Nom, personnage.Nom);
        writer.WriteElementString(AppConstants.XmlElements.Rarete, personnage.Rarete.ToString());
        writer.WriteElementString(AppConstants.XmlElements.Type, personnage.Type.ToString());
        writer.WriteElementString(AppConstants.XmlElements.Puissance, personnage.Puissance.ToString());
        writer.WriteElementString(AppConstants.XmlElements.Niveau, personnage.Niveau.ToString());
        writer.WriteElementString(AppConstants.XmlElements.HasRelation, personnage.HasRelation.ToString());
        writer.WriteElementString(AppConstants.XmlElements.NivRelation, personnage.NivRelation.ToString());

        if (personnage.Capacites?.Count > 0)
        {
            writer.WriteStartElement(AppConstants.XmlElements.Capacites);
            foreach (var capacite in personnage.Capacites)
            {
                writer.WriteStartElement(AppConstants.XmlElements.Capacite);
                if (capacite.Id > 0)
                {
                    writer.WriteAttributeString(AppConstants.XmlElements.Id, capacite.Id.ToString());
                }
                writer.WriteElementString(AppConstants.XmlElements.Nom, capacite.Nom);

                if (!string.IsNullOrWhiteSpace(capacite.Description))
                {
                    writer.WriteElementString(AppConstants.XmlElements.Description, capacite.Description);
                }

                if (!string.IsNullOrWhiteSpace(capacite.Icon))
                {
                    writer.WriteElementString(AppConstants.XmlElements.Icon, capacite.Icon);
                }

                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        if (!isTemplate)
        {
            writer.WriteElementString(AppConstants.XmlElements.PA, personnage.PA.ToString());
            writer.WriteElementString(AppConstants.XmlElements.PV, personnage.PV.ToString());

            writer.WriteElementString(AppConstants.XmlElements.Rang, personnage.Rang.ToString());
            writer.WriteElementString(AppConstants.XmlElements.Role, personnage.Role.ToString());
            writer.WriteElementString(AppConstants.XmlElements.Faction, personnage.Faction.ToString());
            writer.WriteElementString(AppConstants.XmlElements.TypeAttaque, personnage.TypeAttaque.ToString());
            writer.WriteElementString(AppConstants.XmlElements.Selectionne, personnage.Selectionne.ToString());
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

    private List<Capacite> ResolveCapacites(IEnumerable<Capacite> importedCapacites)
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

    private Capacite? FindExistingCapacite(Capacite capacite)
    {
        if (capacite.Id > 0)
        {
            return _context.Capacites.FirstOrDefault(c => c.Id == capacite.Id);
        }

        return _context.Capacites.FirstOrDefault(c => c.Nom.Equals(capacite.Nom, StringComparison.OrdinalIgnoreCase));
    }

    private static void UpdateExistingCapacite(Capacite existing, Capacite capacite)
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

    private static Capacite CreateNewCapacite(Capacite capacite)
    {
        return new Capacite
        {
            Nom = capacite.Nom.Trim(),
            Description = capacite.Description ?? string.Empty,
            Icon = capacite.Icon ?? string.Empty
        };
    }

    private static string NormalizeUpper(string? value) => (value ?? string.Empty).Trim().ToUpper();
    private static Rarete ParseRarete(string? value)
    {
        return value switch
        {
            AppConstants.XmlElements.SSR => Rarete.SSR,
            AppConstants.XmlElements.SR => Rarete.SR,
            AppConstants.XmlElements.R => Rarete.R,
            _ => Rarete.R
        };
    }

    private static bool ParseBool(string? value)
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

    private static TypePersonnage ParseType(string? value)
    {
        return value switch
        {
            AppConstants.XmlElements.Mercenaire => TypePersonnage.Mercenaire,
            AppConstants.XmlElements.Androïde or AppConstants.XmlElements.Androide => TypePersonnage.Androide,
            AppConstants.XmlElements.Commandant => TypePersonnage.Commandant,
            _ => TypePersonnage.Mercenaire
        };
    }

    private static Role ParseRole(string? value)
    {
        return value switch
        {
            AppConstants.XmlElements.Sentinelle => Role.Sentinelle,
            AppConstants.XmlElements.Combattante => Role.Combattante,
            AppConstants.XmlElements.Androide => Role.Androide,
            AppConstants.XmlElements.Commandant => Role.Commandant,
            _ => Role.Combattante
        };
    }

    private static Faction ParseFaction(string? value)
    {
        return value switch
        {
            AppConstants.XmlElements.Syndicat => Faction.Syndicat,
            AppConstants.XmlElements.Pacificateurs => Faction.Pacificateurs,
            AppConstants.XmlElements.HommesLibres => Faction.HommesLibres,
            _ => Faction.Syndicat
        };
    }

    private static TypeAttaque ParseTypeAttaque(string? value)
    {
        return value switch
        {
            AppConstants.XmlElements.MeleeAccent or AppConstants.XmlElements.Melee => TypeAttaque.Melee,
            "Distance" => TypeAttaque.Distance,
            AppConstants.XmlElements.Androïde or AppConstants.XmlElements.Androide => TypeAttaque.Androide,
            _ => TypeAttaque.Inconnu
        };
    }

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

    private async Task SaveLastExportDate()
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

    /// <summary>
    /// Exporte uniquement les capacités au format PML
    /// </summary>
    public Task<byte[]> ExporterCapacitesPmlAsync(IEnumerable<Capacite> capacites)
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
            writer.WriteStartElement("CapacitesPML");
            writer.WriteAttributeString("version", "1.0");
            writer.WriteAttributeString("exportDate", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"));

            writer.WriteStartElement("Capacites");
            foreach (var capacite in capacites)
            {
                writer.WriteStartElement("Capacite");
                if (capacite.Id > 0)
                {
                    writer.WriteAttributeString(AppConstants.XmlElements.Id, capacite.Id.ToString());
                }
                writer.WriteElementString("Nom", capacite.Nom);
                writer.WriteElementString("Description", capacite.Description ?? "");
                writer.WriteElementString("Icon", capacite.Icon ?? "");
                writer.WriteEndElement();
            }
            writer.WriteEndElement();

            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        return Task.FromResult(memoryStream.ToArray());
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
