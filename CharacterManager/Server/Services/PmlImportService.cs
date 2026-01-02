using CharacterManager.Server.Models;
using CharacterManager.Server.Data;
using CharacterManager.Server.Constants;
using Microsoft.EntityFrameworkCore;
using System.Xml.Linq;
using System.Text;


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

            // Traiter la section inventaire
            if (importInventory)
            {
                var inventaireElements = doc.Root.Elements(AppConstants.XmlElements.Inventaire);
                if (!inventaireElements.Any() && doc.Root.Name.LocalName.Equals(AppConstants.XmlElements.Inventaire, StringComparison.OrdinalIgnoreCase))
                {
                    inventaireElements = [doc.Root];
                }

                if (inventaireElements.Any())
                {
                    result.SuccessCount += await ImportInventaireAsync(inventaireElements, errors);
                }

            }

            // Traiter la section templates (directs ou encapsulés dans <templates>)
            if (importTemplates)
            {
                var directTemplates = doc.Root.Elements(AppConstants.XmlElements.Template);
                var nestedTemplates = doc.Root.Element(AppConstants.XmlElements.Templates)?.Elements(AppConstants.XmlElements.Template) ?? Enumerable.Empty<XElement>();
                var templateElements = directTemplates.Concat(nestedTemplates);

                if (templateElements.Any())
                {
                    result.SuccessCount += await ImportTemplatesAsync(templateElements, errors);
                }
            }

            // Traiter la section meilleure escouade
            if (importBestSquad)
            {
                var bestSquadElements = doc.Root.Elements(AppConstants.XmlElements.MeilleurEscouade);
                if (bestSquadElements.Any())
                {
                    result.SuccessCount += await ImportBestSquadAsync(bestSquadElements, errors);
                }
            }

            // Traiter la section historiques
            if (importHistories)
            {
                // Traiter les historiques de classement
                var historiqueClassementElements = doc.Root.Elements(AppConstants.XmlElements.HistoriqueClassement);
                if (historiqueClassementElements.Any())
                {
                    result.SuccessCount += await ImportHistoriquesClassementAsync(historiqueClassementElements, errors);
                }
            }

            // Traiter les historiques de ligue
            if (importLeagueHistory)
            {
                var historiqueLigueElements = doc.Root.Elements(AppConstants.XmlElements.HistoriqueLigue);
                if (historiqueLigueElements.Any())
                {
                    result.SuccessCount += await ImportHistoriquesLigueAsync(historiqueLigueElements, errors);
                }
            }

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
    /// Importe les données depuis la section inventaire
    /// </summary>
    private async Task<int> ImportInventaireAsync(IEnumerable<XElement> inventaireElements, List<string> errors)
    {
        int importedCount = 0;
        var personnages = await _context.Personnages.AsNoTracking().ToListAsync();

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

                var normalizedTemplate = NormalizeUpper(templateName);

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
                            importedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"{AppConstants.Messages.ErrorImportPersonnageTemplate} '{templateName}': {ex.Message}");
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
                errors.Add($"{AppConstants.Messages.ErrorImportTemplate} {ex.Message}");
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
                var mercenairesElements = bestSquad.Elements(AppConstants.XmlElements.Mercenaire);
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
                var commandantElement = bestSquad.Element(AppConstants.XmlElements.Commandant);
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
                var androidesElements = bestSquad.Elements(AppConstants.XmlElements.Androide);
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
                errors.Add($"{AppConstants.Messages.ErrorImportBestSquad} {ex.Message}");
            }
        }

        return Task.FromResult(importedCount);
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

                if (!DateOnly.TryParse(dateStr, out var DateMontee))
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
                var dateStr = historiqueClassementElement.Element(AppConstants.XmlElements.DateEnregistrement)?.Value;
                var ligueStr = historiqueClassementElement.Element(AppConstants.XmlElements.Ligue)?.Value;
                var scoreStr = historiqueClassementElement.Element(AppConstants.XmlElements.Score)?.Value;
                var puissanceTotalStr = historiqueClassementElement.Element(AppConstants.XmlElements.PuissanceTotal)?.Value;
                var puissanceCommandantStr = historiqueClassementElement.Element(AppConstants.XmlElements.PuissanceCommandant)?.Value;
                var puissanceMercenairesStr = historiqueClassementElement.Element(AppConstants.XmlElements.PuissanceMercenaires)?.Value;
                var puissanceLucieStr = historiqueClassementElement.Element(AppConstants.XmlElements.PuissanceLucie)?.Value;

                if (string.IsNullOrWhiteSpace(dateStr))
                {
                    errors.Add("Historique de classement invalide: date manquante");
                    continue;
                }

                if (!DateOnly.TryParse(dateStr, out var dateEnregistrement))
                {
                    errors.Add($"Date d'enregistrement invalide: {dateStr}");
                    continue;
                }

                var historiqueClassement = new HistoriqueClassement
                {
                    DateEnregistrement = dateEnregistrement,
                    Ligue = int.TryParse(ligueStr, out var ligue) ? ligue : 0,
                    Score = int.TryParse(scoreStr, out var score) ? score : 0,
                    PuissanceTotale = int.TryParse(puissanceTotalStr, out var puissanceTotal) ? puissanceTotal : 0,
                    PuissanceCommandant = int.TryParse(puissanceCommandantStr, out var puissanceCommandant) ? puissanceCommandant : 0,
                    PuissanceMercenaires = int.TryParse(puissanceMercenairesStr, out var puissanceMercenaires) ? puissanceMercenaires : 0,
                    PuissanceLucie = int.TryParse(puissanceLucieStr, out var puissanceLucie) ? puissanceLucie : 0
                };

                // Importer les classements
                var classementsElement = historiqueClassementElement.Element(AppConstants.XmlElements.Classements);
                if (classementsElement != null)
                {
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

                // Importer les mercenaires
                var mercenairesElement = historiqueClassementElement.Element(AppConstants.XmlElements.Mercenaires);
                if (mercenairesElement != null)
                {
                    foreach (var personnageElement in mercenairesElement.Elements(AppConstants.XmlElements.Personnage))
                    {
                        var personnage = ParsePersonnageFromXml(personnageElement);
                        if (personnage != null)
                        {
                            var personnageHistorique = new PersonnageHistorique
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
                                Description = personnage.Description,
                                IdOrigine = 0
                            };
                            historiqueClassement.Mercenaires.Add(personnageHistorique);
                        }
                    }
                }

                // Importer le commandant
                var commandantElement = historiqueClassementElement.Element(AppConstants.XmlElements.Commandant);
                if (commandantElement != null)
                {
                    var personnageElement = commandantElement.Element(AppConstants.XmlElements.Personnage) ?? commandantElement;
                    var personnage = ParsePersonnageFromXml(personnageElement);
                    if (personnage != null)
                    {
                        historiqueClassement.Commandant = new PersonnageHistorique
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
                            Description = personnage.Description,
                            IdOrigine = 0
                        };
                    }
                }

                // Importer les androïdes
                var androidesElement = historiqueClassementElement.Element(AppConstants.XmlElements.Androides);
                if (androidesElement != null)
                {
                    foreach (var personnageElement in androidesElement.Elements(AppConstants.XmlElements.Personnage))
                    {
                        var personnage = ParsePersonnageFromXml(personnageElement);
                        if (personnage != null)
                        {
                            var personnageHistorique = new PersonnageHistorique
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
                                Description = personnage.Description,
                                IdOrigine = 0
                            };
                            historiqueClassement.Androides.Add(personnageHistorique);
                        }
                    }
                }

                // Importer les pièces
                var piecesElement = historiqueClassementElement.Element(AppConstants.XmlElements.Pieces);
                if (piecesElement != null)
                {
                    foreach (var pieceElement in piecesElement.Elements(AppConstants.XmlElements.Piece))
                    {
                        var nom = pieceElement.Element(AppConstants.XmlElements.Nom)?.Value;
                        if (!string.IsNullOrWhiteSpace(nom))
                        {
                            var pieceHistorique = new PieceHistorique
                            {
                                Nom = nom,
                                Niveau = int.TryParse(pieceElement.Element(AppConstants.XmlElements.Niveau)?.Value, out var niveau) ? niveau : 1,
                                Selectionnee = ParseBool(pieceElement.Element(AppConstants.XmlElements.Selectionne)?.Value),
                                IdOrigine = 0
                            };

                            if (int.TryParse(pieceElement.Element(AppConstants.XmlElements.PuissanceTactique)?.Value, out var pTact))
                            {
                                pieceHistorique.AspectsTactiques.Puissance = pTact;
                            }

                            if (int.TryParse(pieceElement.Element(AppConstants.XmlElements.PuissanceStrategique)?.Value, out var pStrat))
                            {
                                pieceHistorique.AspectsStrategiques.Puissance = pStrat;
                            }

                            historiqueClassement.Pieces.Add(pieceHistorique);
                        }
                    }
                }

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
            var affectionStr = lucieHouseElement.Element(AppConstants.XmlElements.Affection)?.Value;
            if (!string.IsNullOrWhiteSpace(affectionStr) && int.TryParse(affectionStr, out var affection))
            {
                lucieHouse.Affection = affection;
            }

            var piecesElements = lucieHouseElement.Elements(AppConstants.XmlElements.Piece);

            foreach (var pieceElement in piecesElements)
            {
                try
                {
                    var nom = pieceElement.Element(AppConstants.XmlElements.Nom)?.Value;
                    if (string.IsNullOrWhiteSpace(nom))
                        continue;

                    var piece = new Piece
                    {
                        Nom = nom,
                        Niveau = int.TryParse(pieceElement.Element(AppConstants.XmlElements.Niveau)?.Value, out var niveau) ? niveau : 1,
                        Selectionnee = ParseBool(pieceElement.Element(AppConstants.XmlElements.Selectionne)?.Value)
                    };

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

                    lucieHouse.Pieces.Add(piece);
                    importedCount++;
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

    /// <summary>
    /// Parse un personnage depuis un élément XML
    /// </summary>
    private Personnage? ParsePersonnageFromXml(XElement element)
    {
        var nom = element.Element(AppConstants.XmlElements.Nom)?.Value;
        if (string.IsNullOrWhiteSpace(nom))
            return null;

        var personnage = new Personnage
        {
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
            Description = element.Element(AppConstants.XmlElements.Description)?.Value ?? $"Personnage {nom}",
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
            writer.WriteStartElement(AppConstants.XmlElements.InventairePML);
            writer.WriteAttributeString(AppConstants.XmlElements.Version, "1.0");
            writer.WriteAttributeString(AppConstants.XmlElements.ExportDate, DateTime.UtcNow.ToString(AppConstants.DateTimeFormats.IsoDateTime));

            writer.WriteStartElement(AppConstants.XmlElements.Inventaire);

            foreach (var personnage in personnages)
            {
                writer.WriteStartElement(AppConstants.XmlElements.Personnage);
                writer.WriteElementString(AppConstants.XmlElements.Nom, personnage.Nom);
                writer.WriteElementString(AppConstants.XmlElements.Rarete, personnage.Rarete.ToString());
                writer.WriteElementString(AppConstants.XmlElements.Type, personnage.Type.ToString());
                writer.WriteElementString(AppConstants.XmlElements.Puissance, personnage.Puissance.ToString());
                writer.WriteElementString(AppConstants.XmlElements.PA, personnage.PA.ToString());
                writer.WriteElementString(AppConstants.XmlElements.PV, personnage.PV.ToString());
                writer.WriteElementString(AppConstants.XmlElements.Niveau, personnage.Niveau.ToString());
                writer.WriteElementString(AppConstants.XmlElements.Rang, personnage.Rang.ToString());
                writer.WriteElementString(AppConstants.XmlElements.Role, personnage.Role.ToString());
                writer.WriteElementString(AppConstants.XmlElements.Faction, personnage.Faction.ToString());
                writer.WriteElementString(AppConstants.XmlElements.TypeAttaque, personnage.TypeAttaque.ToString());
                writer.WriteElementString(AppConstants.XmlElements.Selectionne, personnage.Selectionne.ToString());
                writer.WriteElementString(AppConstants.XmlElements.Description, personnage.Description ?? "");
                writer.WriteEndElement();
            }

            // Export Lucie House as part of the inventory payload (no extra checkbox/UI toggle)
            var lucieHouse = _context.LucieHouses.Include(l => l.Pieces).FirstOrDefault();
            if (lucieHouse != null)
            {
                writer.WriteStartElement(AppConstants.XmlElements.LucieHouse);
                writer.WriteElementString(AppConstants.XmlElements.Affection, lucieHouse.Affection.ToString());

                foreach (var piece in lucieHouse.Pieces)
                {
                    writer.WriteStartElement(AppConstants.XmlElements.Piece);
                    writer.WriteElementString(AppConstants.XmlElements.Nom, piece.Nom);
                    writer.WriteElementString(AppConstants.XmlElements.Niveau, piece.Niveau.ToString());
                    writer.WriteElementString(AppConstants.XmlElements.PuissanceTactique, piece.AspectsTactiques.Puissance.ToString());
                    writer.WriteElementString(AppConstants.XmlElements.PuissanceStrategique, piece.AspectsStrategiques.Puissance.ToString());
                    writer.WriteElementString(AppConstants.XmlElements.Selectionne, piece.Selectionnee.ToString());

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

                    writer.WriteEndElement();
                }

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
                    var personnage = _context.Personnages.FirstOrDefault(p => p.Id == personnageId);
                    if (personnage != null)
                    {
                        writer.WriteStartElement(AppConstants.XmlElements.Personnage);
                        writer.WriteElementString(AppConstants.XmlElements.Nom, personnage.Nom);
                        writer.WriteElementString(AppConstants.XmlElements.Rarete, personnage.Rarete.ToString());
                        writer.WriteElementString(AppConstants.XmlElements.Puissance, personnage.Puissance.ToString());
                        writer.WriteElementString(AppConstants.XmlElements.Niveau, personnage.Niveau.ToString());
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
            if (options.IsExporting(PmlExportOptions.EXPORT_TYPE_INVENTORY))
            {
                writer.WriteStartElement(AppConstants.XmlElements.Inventaire);
                var personnages = await _context.Personnages.ToListAsync();
                foreach (var personnage in personnages)
                {
                    writer.WriteStartElement(AppConstants.XmlElements.Personnage);
                    writer.WriteElementString(AppConstants.XmlElements.Nom, personnage.Nom);
                    writer.WriteElementString(AppConstants.XmlElements.Rarete, personnage.Rarete.ToString());
                    writer.WriteElementString(AppConstants.XmlElements.Type, personnage.Type.ToString());
                    writer.WriteElementString(AppConstants.XmlElements.Puissance, personnage.Puissance.ToString());
                    writer.WriteElementString(AppConstants.XmlElements.PA, personnage.PA.ToString());
                    writer.WriteElementString(AppConstants.XmlElements.PV, personnage.PV.ToString());
                    writer.WriteElementString(AppConstants.XmlElements.Niveau, personnage.Niveau.ToString());
                    writer.WriteElementString(AppConstants.XmlElements.Rang, personnage.Rang.ToString());
                    writer.WriteElementString(AppConstants.XmlElements.Role, personnage.Role.ToString());
                    writer.WriteElementString(AppConstants.XmlElements.Faction, personnage.Faction.ToString());
                    writer.WriteElementString(AppConstants.XmlElements.Selectionne, personnage.Selectionne.ToString());
                    writer.WriteElementString(AppConstants.XmlElements.Description, personnage.Description ?? "");
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();

                // Export Lucie House
                var lucieHouse = await _context.LucieHouses.Include(l => l.Pieces).FirstOrDefaultAsync();
                if (lucieHouse != null)
                {
                    writer.WriteStartElement(AppConstants.XmlElements.LucieHouse);
                    writer.WriteElementString(AppConstants.XmlElements.Affection, lucieHouse.Affection.ToString());

                    foreach (var piece in lucieHouse.Pieces)
                    {
                        writer.WriteStartElement(AppConstants.XmlElements.Piece);
                        writer.WriteElementString(AppConstants.XmlElements.Nom, piece.Nom);
                        writer.WriteElementString(AppConstants.XmlElements.Niveau, piece.Niveau.ToString());
                        writer.WriteElementString(AppConstants.XmlElements.PuissanceTactique, piece.AspectsTactiques.Puissance.ToString());
                        writer.WriteElementString(AppConstants.XmlElements.PuissanceStrategique, piece.AspectsStrategiques.Puissance.ToString());
                        writer.WriteElementString(AppConstants.XmlElements.Selectionne, piece.Selectionnee.ToString());

                        // Export des bonus tactiques
                        if (piece.AspectsTactiques.Bonus.Count > 0)
                        {
                            writer.WriteStartElement(AppConstants.XmlElements.BonusTactiques);
                            foreach (var bonus in piece.AspectsTactiques.Bonus)
                            {
                                writer.WriteElementString(AppConstants.XmlElements.Bonus, bonus);
                            }
                            writer.WriteEndElement();
                        }

                        // Export des bonus stratégiques
                        if (piece.AspectsStrategiques.Bonus.Count > 0)
                        {
                            writer.WriteStartElement(AppConstants.XmlElements.BonusStrategiques);
                            foreach (var bonus in piece.AspectsStrategiques.Bonus)
                            {
                                writer.WriteElementString(AppConstants.XmlElements.Bonus, bonus);
                            }
                            writer.WriteEndElement();
                        }

                        writer.WriteEndElement();
                    }

                    writer.WriteEndElement();
                }
            }

            // Export templates
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
                        var personnage = await _context.Personnages.FirstOrDefaultAsync(p => p.Id == personnageId);
                        if (personnage != null)
                        {
                            writer.WriteStartElement(AppConstants.XmlElements.Personnage);
                            writer.WriteElementString(AppConstants.XmlElements.Nom, personnage.Nom);
                            writer.WriteElementString(AppConstants.XmlElements.Rarete, personnage.Rarete.ToString());
                            writer.WriteElementString(AppConstants.XmlElements.Puissance, personnage.Puissance.ToString());
                            writer.WriteElementString(AppConstants.XmlElements.Niveau, personnage.Niveau.ToString());
                            writer.WriteEndElement();
                        }
                    }
                    writer.WriteEndElement();
                }
            }

            // Export meilleure escouade
            if (options.IsExporting(PmlExportOptions.EXPORT_TYPE_BEST_SQUAD))
            {
                writer.WriteStartElement(AppConstants.XmlElements.MeilleurEscouade);

                // Mercenaires (top 10 par puissance)
                var topMercenaires = await _context.Personnages
                    .Where(p => p.Type == TypePersonnage.Mercenaire)
                    .OrderByDescending(p => p.Puissance)
                    .Take(10)
                    .ToListAsync();

                foreach (var merc in topMercenaires)
                {
                    writer.WriteStartElement(AppConstants.XmlElements.Mercenaire);
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
                    writer.WriteStartElement(AppConstants.XmlElements.Commandant);
                    WritePersonnageData(writer, topCommandant);
                    writer.WriteEndElement();
                }

                // Androides (top 5 par puissance)
                var topAndroides = await _context.Personnages
                    .Where(p => p.Type == TypePersonnage.Androide)
                    .OrderByDescending(p => p.Puissance)
                    .Take(5)
                    .ToListAsync();

                foreach (var android in topAndroides)
                {
                    writer.WriteStartElement(AppConstants.XmlElements.Androide);
                    WritePersonnageData(writer, android);
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
            }

            // Export historiques de classement
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
                    writer.WriteStartElement(AppConstants.XmlElements.HistoriqueClassement);
                    writer.WriteElementString(AppConstants.XmlElements.DateEnregistrement, historiqueClassement.DateEnregistrement.ToString("yyyy-MM-dd"));
                    writer.WriteElementString(AppConstants.XmlElements.Ligue, historiqueClassement.Ligue.ToString());
                    writer.WriteElementString(AppConstants.XmlElements.Score, historiqueClassement.Score.ToString());
                    writer.WriteElementString(AppConstants.XmlElements.PuissanceTotal, historiqueClassement.PuissanceTotale.ToString());
                    writer.WriteElementString(AppConstants.XmlElements.PuissanceCommandant, historiqueClassement.PuissanceCommandant.ToString());
                    writer.WriteElementString(AppConstants.XmlElements.PuissanceMercenaires, historiqueClassement.PuissanceMercenaires.ToString());
                    writer.WriteElementString(AppConstants.XmlElements.PuissanceLucie, historiqueClassement.PuissanceLucie.ToString());

                    // Export des classements
                    if (historiqueClassement.Classements.Any())
                    {
                        writer.WriteStartElement(AppConstants.XmlElements.Classements);
                        foreach (var classement in historiqueClassement.Classements)
                        {
                            writer.WriteStartElement(AppConstants.XmlElements.ClassementItem);
                            writer.WriteElementString(AppConstants.XmlElements.Nom, classement.Nom);
                            writer.WriteElementString(AppConstants.XmlElements.TypeClassement, classement.Type.ToString());
                            writer.WriteElementString(AppConstants.XmlElements.Valeur, classement.Valeur.ToString());
                            writer.WriteEndElement();
                        }
                        writer.WriteEndElement();
                    }

                    // Export des mercenaires
                    if (historiqueClassement.Mercenaires.Any())
                    {
                        writer.WriteStartElement(AppConstants.XmlElements.Mercenaires);
                        foreach (var mercenaire in historiqueClassement.Mercenaires)
                        {
                            writer.WriteStartElement(AppConstants.XmlElements.Personnage);
                            WritePersonnageData(writer, mercenaire);
                            writer.WriteEndElement();
                        }
                        writer.WriteEndElement();
                    }

                    // Export du commandant
                    if (historiqueClassement.Commandant != null)
                    {
                        writer.WriteStartElement(AppConstants.XmlElements.Commandant);
                        WritePersonnageData(writer, historiqueClassement.Commandant);
                        writer.WriteEndElement();
                    }

                    // Export des androïdes
                    if (historiqueClassement.Androides.Any())
                    {
                        writer.WriteStartElement(AppConstants.XmlElements.Androides);
                        foreach (var androide in historiqueClassement.Androides)
                        {
                            writer.WriteStartElement(AppConstants.XmlElements.Personnage);
                            WritePersonnageData(writer, androide);
                            writer.WriteEndElement();
                        }
                        writer.WriteEndElement();
                    }

                    // Export des pièces
                    if (historiqueClassement.Pieces.Any())
                    {
                        writer.WriteStartElement(AppConstants.XmlElements.Pieces);
                        foreach (var piece in historiqueClassement.Pieces)
                        {
                            writer.WriteStartElement(AppConstants.XmlElements.Piece);
                            writer.WriteElementString(AppConstants.XmlElements.Nom, piece.Nom);
                            writer.WriteElementString(AppConstants.XmlElements.Niveau, piece.Niveau.ToString());
                            writer.WriteElementString(AppConstants.XmlElements.PuissanceTactique, piece.AspectsTactiques.Puissance.ToString());
                            writer.WriteElementString(AppConstants.XmlElements.PuissanceStrategique, piece.AspectsStrategiques.Puissance.ToString());
                            writer.WriteElementString(AppConstants.XmlElements.Selectionne, piece.Selectionnee.ToString());
                            writer.WriteEndElement();
                        }
                        writer.WriteEndElement();
                    }

                    writer.WriteEndElement();
                }
            }

            // Export historiques de ligue
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

            // Export capacités
            if (options.IsExporting(PmlExportOptions.EXPORT_TYPE_CAPACITES))
            {
                var capacites = await _context.Capacites.ToListAsync();
                if (capacites.Any())
                {
                    writer.WriteStartElement("Capacites");
                    foreach (var capacite in capacites)
                    {
                        writer.WriteStartElement("Capacite");
                        writer.WriteElementString("Nom", capacite.Nom);
                        writer.WriteElementString("Description", capacite.Description ?? "");
                        writer.WriteElementString("Icon", capacite.Icon ?? "");
                        writer.WriteEndElement();
                    }
                    writer.WriteEndElement();
                }
            }

            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        var bytes = memoryStream.ToArray();
        await SaveLastExportDate();
        return bytes;
    }

    /// <summary>
    /// Helper method to write personnage data to XML
    /// </summary>
    private static void WritePersonnageData(System.Xml.XmlWriter writer, Personnage personnage)
    {
        writer.WriteElementString(AppConstants.XmlElements.Nom, personnage.Nom);
        writer.WriteElementString(AppConstants.XmlElements.Rarete, personnage.Rarete.ToString());
        writer.WriteElementString(AppConstants.XmlElements.Type, personnage.Type.ToString());
        writer.WriteElementString(AppConstants.XmlElements.Puissance, personnage.Puissance.ToString());
        writer.WriteElementString(AppConstants.XmlElements.PA, personnage.PA.ToString());
        writer.WriteElementString(AppConstants.XmlElements.PV, personnage.PV.ToString());
        writer.WriteElementString(AppConstants.XmlElements.Niveau, personnage.Niveau.ToString());
        writer.WriteElementString(AppConstants.XmlElements.Rang, personnage.Rang.ToString());
        writer.WriteElementString(AppConstants.XmlElements.Role, personnage.Role.ToString());
        writer.WriteElementString(AppConstants.XmlElements.Faction, personnage.Faction.ToString());
        writer.WriteElementString(AppConstants.XmlElements.TypeAttaque, personnage.TypeAttaque.ToString());
        writer.WriteElementString(AppConstants.XmlElements.Selectionne, personnage.Selectionne.ToString());
        writer.WriteElementString(AppConstants.XmlElements.Description, personnage.Description ?? "");
    }

    private void ImportOrUpdatePersonnage(Personnage nouveauPersonnage)
    {

        var normalizedName = NormalizeUpper(nouveauPersonnage.Nom);

        var existing = _context.Personnages
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
            existing.Description = nouveauPersonnage.Description;

            _context.Personnages.Update(existing);
        }
        else
        {
            // Ajouter le nouveau personnage
            nouveauPersonnage.Nom = normalizedName;
            _context.Personnages.Add(nouveauPersonnage);
        }

        _context.SaveChanges();
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

                    var description = capaciteElement.Element("Description")?.Value ?? "";
                    var icon = capaciteElement.Element("Icon")?.Value ?? "";

                    // Vérifier si la capacité existe déjà
                    var existing = await _context.Capacites.FirstOrDefaultAsync(c => c.Nom == nom);
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
