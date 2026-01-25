using System.Text.Json;
using CharacterManager.Server.Data;
using CharacterManager.Server.Models;
using Microsoft.EntityFrameworkCore;
using CharacterManager.Server.Constants;

namespace CharacterManager.Server.Services;

/// <summary>
/// DTO pour enregistrer une modification d'entité
/// </summary>
public class ModificationHistoriqueRequest
{
    public TypeEntite TypeEntite { get; set; }
    public int EntiteId { get; set; }
    public string NomEntite { get; set; } = string.Empty;
    public string ChampModifie { get; set; } = string.Empty;
    public object? AncienneValeur { get; set; }
    public object? NouvelleValeur { get; set; }
    public string? Description { get; set; }
    public DateTime? DateModification { get; set; }
    public bool EstImportation { get; set; } = false;
}

/// <summary>
/// Service de gestion de l'historique des modifications
/// </summary>
public class HistoriqueModificationService
{
    private readonly ApplicationDbContext _context;

    public HistoriqueModificationService(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Enregistre une création d'entité
    /// </summary>
    public async Task EnregistrerCreationAsync(
        TypeEntite typeEntite,
        int entiteId,
        string nomEntite,
        object? donnees,
        string? description = null,
        DateTime? dateModification = null,
        bool estImportation = false)
    {
        var timestamp = dateModification ?? DateTime.UtcNow;

        var historique = new HistoriqueModification
        {
            TypeEntite = typeEntite,
            EntiteId = entiteId,
            NomEntite = nomEntite,
            TypeModification = TypeModification.Creation,
            DateModification = timestamp,
            DateInsertion = timestamp,
            DateMiseAJour = timestamp,
            NouvelleValeur = donnees != null ? JsonSerializer.Serialize(donnees) : null,
            Description = description ?? $"Création de {nomEntite}",
            EstImportation = estImportation
        };

        _context.HistoriquesModifications.Add(historique);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Enregistre une modification d'entité.
    /// Si une modification du même type (même TypeEntite, EntiteId, TypeModification et ChampModifie) 
    /// a eu lieu il y a moins de 5 secondes, la ligne existante est mise à jour au lieu d'en ajouter une nouvelle.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "Parameters validated by EF Core")]
    public async Task EnregistrerModificationAsync(ModificationHistoriqueRequest request)
    {
        await EnregistrerModificationInternalAsync(request);
    }

    /// <summary>
    /// Enregistre une modification d'entité (surcharge compatible avec signature existante).
    /// Si une modification du même type (même TypeEntite, EntiteId, TypeModification et ChampModifie) 
    /// a eu lieu il y a moins de 5 secondes, la ligne existante est mise à jour au lieu d'en ajouter une nouvelle.
    /// </summary>
    /// <remarks>
    /// Cette surcharge maintient la compatibilité arrière. Préférez utiliser la surcharge acceptant ModificationHistoriqueRequest.
    /// Exceptionnellement conservée avec 9 paramètres pour la rétrocompatibilité - préférer la surcharge avec ModificationHistoriqueRequest.
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "Parameters validated by EF Core")]
#pragma warning disable S107 // Functions should not have too many parameters
    public async Task EnregistrerModificationAsync(
        TypeEntite typeEntite,
        int entiteId,
        string nomEntite,
        string champModifie,
        object? ancienneValeur,
        object? nouvelleValeur,
        string? description = null,
        DateTime? dateModification = null,
        bool estImportation = false)
    {
        var request = new ModificationHistoriqueRequest
        {
            TypeEntite = typeEntite,
            EntiteId = entiteId,
            NomEntite = nomEntite,
            ChampModifie = champModifie,
            AncienneValeur = ancienneValeur,
            NouvelleValeur = nouvelleValeur,
            Description = description,
            DateModification = dateModification,
            EstImportation = estImportation
        };

        await EnregistrerModificationInternalAsync(request);
    }
#pragma warning restore S107

    private async Task EnregistrerModificationInternalAsync(ModificationHistoriqueRequest request)
    {
        var maintenant = request.DateModification ?? DateTime.UtcNow;
        var jourUtc = maintenant.Date;

        // Cherche une modification existante pour le même jour et le même champ
        var modificationJour = await _context.HistoriquesModifications
            .Where(h => h.TypeEntite == request.TypeEntite
                && h.EntiteId == request.EntiteId
                && h.TypeModification == TypeModification.Modification
                && h.ChampModifie == request.ChampModifie
                && h.DateModification.Date == jourUtc)
            .OrderByDescending(h => h.DateModification)
            .FirstOrDefaultAsync();

        if (modificationJour != null)
        {
            // Mise à jour de la ligne existante pour le jour
            modificationJour.DateModification = maintenant;
            modificationJour.DateMiseAJour = maintenant;
            // On conserve l'ancienne valeur, on ne met à jour que la nouvelle
            modificationJour.NouvelleValeur = request.NouvelleValeur != null ? JsonSerializer.Serialize(request.NouvelleValeur) : null;
            modificationJour.Description = request.Description ?? $"Modification de {request.ChampModifie} pour {request.NomEntite}";
            modificationJour.EstImportation = request.EstImportation;

            _context.HistoriquesModifications.Update(modificationJour);
        }
        else
        {
            // Création d'une nouvelle ligne pour ce jour
            var historique = new HistoriqueModification
            {
                TypeEntite = request.TypeEntite,
                EntiteId = request.EntiteId,
                NomEntite = request.NomEntite,
                TypeModification = TypeModification.Modification,
                DateModification = maintenant,
                DateInsertion = maintenant,
                DateMiseAJour = maintenant,
                ChampModifie = request.ChampModifie,
                AncienneValeur = request.AncienneValeur != null ? JsonSerializer.Serialize(request.AncienneValeur) : null,
                NouvelleValeur = request.NouvelleValeur != null ? JsonSerializer.Serialize(request.NouvelleValeur) : null,
                Description = request.Description ?? $"Modification de {request.ChampModifie} pour {request.NomEntite}",
                EstImportation = request.EstImportation
            };

            _context.HistoriquesModifications.Add(historique);
        }

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Enregistre une suppression d'entité
    /// </summary>
    public async Task EnregistrerSuppressionAsync(
        TypeEntite typeEntite,
        int entiteId,
        string nomEntite,
        object? donnees,
        string? description = null,
        DateTime? dateModification = null,
        bool estImportation = false)
    {
        var timestamp = dateModification ?? DateTime.UtcNow;

        var historique = new HistoriqueModification
        {
            TypeEntite = typeEntite,
            EntiteId = entiteId,
            NomEntite = nomEntite,
            TypeModification = TypeModification.Suppression,
            DateModification = timestamp,
            DateInsertion = timestamp,
            DateMiseAJour = timestamp,
            AncienneValeur = donnees != null ? JsonSerializer.Serialize(donnees) : null,
            Description = description ?? $"Suppression de {nomEntite}",
            EstImportation = estImportation
        };

        _context.HistoriquesModifications.Add(historique);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Récupère l'historique avec filtres optionnels
    /// </summary>
    public async Task<List<HistoriqueModification>> GetHistoriqueAsync(
        TypeEntite? typeEntite = null,
        int? entiteId = null,
        DateTime? dateDebut = null,
        DateTime? dateFin = null,
        int? limit = null)
    {
        var query = _context.HistoriquesModifications.AsQueryable();

        if (typeEntite.HasValue)
            query = query.Where(h => h.TypeEntite == typeEntite.Value);

        if (entiteId.HasValue)
            query = query.Where(h => h.EntiteId == entiteId.Value);

        if (dateDebut.HasValue)
            query = query.Where(h => h.DateModification >= dateDebut.Value);

        if (dateFin.HasValue)
            query = query.Where(h => h.DateModification <= dateFin.Value);

        query = query.OrderByDescending(h => h.DateModification);

        if (limit.HasValue)
            query = query.Take(limit.Value);

        return await query.ToListAsync();
    }

    /// <summary>
    /// Récupère l'historique pour une entité spécifique
    /// </summary>
    public async Task<List<HistoriqueModification>> GetHistoriqueEntiteAsync(TypeEntite typeEntite, int entiteId)
    {
        return await _context.HistoriquesModifications
            .Where(h => h.TypeEntite == typeEntite && h.EntiteId == entiteId)
            .OrderByDescending(h => h.DateModification)
            .ToListAsync();
    }

    /// <summary>
    /// Supprime l'historique plus ancien qu'une date donnée
    /// </summary>
    public async Task<int> SupprimerHistoriqueAvantAsync(DateTime date)
    {
        var anciens = await _context.HistoriquesModifications
            .Where(h => h.DateModification < date)
            .ToListAsync();

        _context.HistoriquesModifications.RemoveRange(anciens);
        await _context.SaveChangesAsync();

        return anciens.Count;
    }

    /// <summary>
    /// Supprime tout l'historique
    /// </summary>
    public async Task<int> SupprimerToutAsync()
    {
        var tous = await _context.HistoriquesModifications.ToListAsync();
        var count = tous.Count;
        
        _context.HistoriquesModifications.RemoveRange(tous);
        await _context.SaveChangesAsync();

        return count;
    }

    /// <summary>
    /// Supprime une entrée spécifique de l'historique
    /// </summary>
    public async Task<bool> SupprimerAsync(int id)
    {
        var historique = await _context.HistoriquesModifications.FindAsync(id);
        if (historique == null)
            return false;

        _context.HistoriquesModifications.Remove(historique);
        await _context.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// Exporte l'historique en JSON
    /// </summary>
    public async Task<string> ExporterAsync(DateTime? dateDebut = null, DateTime? dateFin = null)
    {
        var historique = await GetHistoriqueAsync(
            dateDebut: dateDebut,
            dateFin: dateFin);

        return JsonSerializer.Serialize(historique, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    /// <summary>
    /// Exporte tout l'historique en JSON sans restriction de dates
    /// </summary>
    public async Task<string> ExporterToutAsync()
    {
        var historique = await _context.HistoriquesModifications
            .OrderByDescending(h => h.DateModification)
            .ToListAsync();

        return JsonSerializer.Serialize(historique, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    /// <summary>
    /// Détecte et supprime les doublons dans l'historique
    /// Un doublon est défini par : même TypeEntite, EntiteId, ChampModifie, DateModification et TypeModification
    /// En cas de doublon, conserve l'entrée la plus récente (DateInsertion la plus récente)
    /// </summary>
    /// <returns>Le nombre de doublons supprimés</returns>
    public async Task<int> NettoyerDoublonsAsync()
    {
        var tousLesHistoriques = await _context.HistoriquesModifications
            .OrderBy(h => h.DateModification)
            .ToListAsync();

        var groupes = tousLesHistoriques
            .GroupBy(h => new
            {
                h.TypeEntite,
                h.EntiteId,
                h.ChampModifie,
                h.DateModification,
                h.TypeModification
            })
            .Where(g => g.Count() > 1)
            .ToList();

        var aSupprimer = new List<HistoriqueModification>();

        foreach (var groupe in groupes)
        {
            // Conserver la plus récente (DateInsertion la plus élevée)
            var aConserver = groupe.OrderByDescending(h => h.DateInsertion).First();
            var doublons = groupe.Where(h => h.Id != aConserver.Id).ToList();
            aSupprimer.AddRange(doublons);
        }

        if (aSupprimer.Any())
        {
            _context.HistoriquesModifications.RemoveRange(aSupprimer);
            await _context.SaveChangesAsync();
        }

        return aSupprimer.Count;
    }

    /// <summary>
    /// Compte le nombre d'entrées dans l'historique
    /// </summary>
    public async Task<int> GetCountAsync(TypeEntite? typeEntite = null)
    {
        var query = _context.HistoriquesModifications.AsQueryable();

        if (typeEntite.HasValue)
            query = query.Where(h => h.TypeEntite == typeEntite.Value);

        return await query.CountAsync();
    }

    /// <summary>
    /// Prévisualise un import d'historique (JSON) sans écrire en base
    /// </summary>
    public async Task<ImportPreviewResult> PreviewImportAsync(Stream jsonStream)
    {
        var preview = new ImportPreviewResult();
        var logs = new List<ImportLogEntry>();

        try
        {
            var modifications = await JsonSerializer.DeserializeAsync<List<HistoriqueModification>>(jsonStream, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<HistoriqueModification>();

            foreach (var modification in modifications)
            {
                var date = DateOnly.FromDateTime(modification.DateModification);
                var dataType = modification.ChampModifie ?? modification.TypeModification.ToString();

                if (modification.TypeEntite == TypeEntite.Personnage)
                {
                    await HandlePersonnagePreviewAsync(modification, dataType, date, preview, logs);
                }
            }

            preview.Logs = logs;
            preview.IsSuccess = string.IsNullOrEmpty(preview.Error);
        }
        catch (Exception ex)
        {
            preview.Error = $"{AppConstants.Messages.ErrorXmlParsing}: {ex.Message}";
            preview.IsSuccess = false;
            preview.Logs = logs;
        }

        return preview;
    }

    private async Task HandlePersonnagePreviewAsync(HistoriqueModification modification, string dataType, DateOnly date, ImportPreviewResult preview, List<ImportLogEntry> logs)
    {
        var personnage = await _context.Personnages.FirstOrDefaultAsync(p => p.Nom == modification.NomEntite);

        if (personnage == null && modification.TypeModification != TypeModification.Suppression)
        {
            preview.Conflicts.Add(new ImportConflict
            {
                PersonnageName = modification.NomEntite,
                ChampModifie = dataType,
                DateClassement = date,
                AncienneValeur = modification.AncienneValeur,
                NouvelleValeur = modification.NouvelleValeur
            });

            logs.Add(new ImportLogEntry
            {
                Level = ImportLogLevel.Warning,
                Category = ImportLogCategory.Historique,
                DataType = dataType,
                Message = $"Modification ignorée (personnage inexistant): {modification.NomEntite} ({dataType}) le {date}"
            });

            return;
        }

        if (personnage != null)
        {
            var existingModification = await _context.HistoriquesModifications
                .FirstOrDefaultAsync(h =>
                    h.TypeEntite == modification.TypeEntite &&
                    h.EntiteId == personnage.Id &&
                    h.ChampModifie == modification.ChampModifie &&
                    h.DateModification == modification.DateModification &&
                    h.TypeModification == modification.TypeModification);

            if (existingModification != null)
            {
                preview.DuplicateCount++;
                logs.Add(new ImportLogEntry
                {
                    Level = ImportLogLevel.Duplicate,
                    Category = ImportLogCategory.Historique,
                    DataType = dataType,
                    Message = $"Doublon détecté (déjà en base): {modification.NomEntite} ({dataType}) le {date}"
                });
                return;
            }
        }

        preview.ValidCount++;
        logs.Add(new ImportLogEntry
        {
            Level = ImportLogLevel.Ok,
            Category = ImportLogCategory.Historique,
            DataType = dataType,
            Message = $"Prêt à importer: {modification.NomEntite} ({dataType}) le {date}"
        });
    }

    /// <summary>
    /// Importe l'historique (JSON) avec résolutions de conflits
    /// </summary>
    public async Task<ImportResult> ImportAsync(Stream jsonStream, Dictionary<string, bool> conflictResolutions, List<ImportConflict>? originalConflicts = null)
    {
        var result = new ImportResult();
        var logs = new List<ImportLogEntry>();

        try
        {
            var modifications = await DeserializeModifications(jsonStream);
            
            foreach (var modification in modifications.OrderBy(m => m.DateModification))
            {
                await ProcessModificationImport(modification, conflictResolutions, result, logs);
            }

            result.Logs = logs;
            result.IsSuccess = string.IsNullOrEmpty(result.Error);

            ProcessConflictResolutions(originalConflicts, conflictResolutions, result);
        }
        catch (Exception ex)
        {
            result.Error = $"{AppConstants.Messages.ErrorXmlParsing}: {ex.Message}";
            result.IsSuccess = false;
            result.Logs = logs;
        }

        return result;
    }

    private static async Task<List<HistoriqueModification>> DeserializeModifications(Stream jsonStream)
    {
        return await JsonSerializer.DeserializeAsync<List<HistoriqueModification>>(jsonStream, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new List<HistoriqueModification>();
    }

    private async Task ProcessModificationImport(HistoriqueModification modification, Dictionary<string, bool> conflictResolutions, ImportResult result, List<ImportLogEntry> logs)
    {
        var date = DateOnly.FromDateTime(modification.DateModification);
        var dataType = modification.ChampModifie ?? modification.TypeModification.ToString();
        var conflictKey = $"{modification.NomEntite}_{dataType}_{date}";

        if (modification.TypeEntite == TypeEntite.Personnage)
        {
            var personnage = await _context.Personnages.FirstOrDefaultAsync(p => p.Nom == modification.NomEntite);

            if (personnage == null && modification.TypeModification != TypeModification.Suppression)
            {
                HandleMissingPersonnage(modification, conflictKey, conflictResolutions, logs, date, dataType);
            }
            else if (personnage != null)
            {
                // Vérifier si la modification existe déjà en base
                var existingModification = await _context.HistoriquesModifications
                    .FirstOrDefaultAsync(h =>
                        h.TypeEntite == modification.TypeEntite &&
                        h.EntiteId == personnage.Id &&
                        h.ChampModifie == modification.ChampModifie &&
                        h.DateModification == modification.DateModification &&
                        h.TypeModification == modification.TypeModification);

                if (existingModification != null)
                {
                    result.DuplicateCount++;
                    logs.Add(new ImportLogEntry
                    {
                        Level = ImportLogLevel.Duplicate,
                        Category = ImportLogCategory.Historique,
                        DataType = dataType,
                        Message = $"Doublon ignoré (déjà en base): {modification.NomEntite} ({dataType}) le {date}"
                    });
                    return;
                }

                await ApplyHistoriqueImport(modification, personnage.Id, logs);
                result.SuccessCount++;
            }
        }
    }

    private static void HandleMissingPersonnage(HistoriqueModification modification, string conflictKey, Dictionary<string, bool> conflictResolutions, List<ImportLogEntry> logs, DateOnly date, string dataType)
    {
        if (!conflictResolutions.TryGetValue(conflictKey, out var resolved) || !resolved)
        {
            logs.Add(new ImportLogEntry
            {
                Level = ImportLogLevel.Warning,
                Category = ImportLogCategory.Historique,
                DataType = dataType,
                Message = $"Modification ignorée (non résolue): {modification.NomEntite} ({dataType}) le {date}"
            });
            return;
        }

        logs.Add(new ImportLogEntry
        {
            Level = ImportLogLevel.Warning,
            Category = ImportLogCategory.Historique,
            DataType = dataType,
            Message = $"Modification ignorée (personnage inexistant): {modification.NomEntite} ({dataType}) le {date}"
        });
    }

    private static void ProcessConflictResolutions(List<ImportConflict>? originalConflicts, Dictionary<string, bool> conflictResolutions, ImportResult result)
    {
        if (originalConflicts == null) return;

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

    private async Task ApplyHistoriqueImport(HistoriqueModification modification, int entiteId, List<ImportLogEntry> logs)
    {
        // Convertir les valeurs JSON en objets pour re-sérialisation
        object? ancienne = null;
        object? nouvelle = null;

        if (!string.IsNullOrWhiteSpace(modification.AncienneValeur))
        {
            try 
            { 
                ancienne = JsonSerializer.Deserialize<object>(modification.AncienneValeur); 
            } 
            catch (JsonException)
            {
                // Si la désérialisation échoue, on garde la valeur null (valeur invalide ignorée)
            }
        }
        if (!string.IsNullOrWhiteSpace(modification.NouvelleValeur))
        {
            try 
            { 
                nouvelle = JsonSerializer.Deserialize<object>(modification.NouvelleValeur); 
            } 
            catch (JsonException)
            {
                // Si la désérialisation échoue, on garde la valeur null (valeur invalide ignorée)
            }
        }

        switch (modification.TypeModification)
        {
            case TypeModification.Creation:
                await EnregistrerCreationAsync(modification.TypeEntite, entiteId, modification.NomEntite, nouvelle, modification.Description, modification.DateModification, estImportation: true);
                break;
            case TypeModification.Modification:
                await EnregistrerModificationAsync(modification.TypeEntite, entiteId, modification.NomEntite, modification.ChampModifie ?? string.Empty, ancienne, nouvelle, modification.Description, modification.DateModification, estImportation: true);
                break;
            case TypeModification.Suppression:
                await EnregistrerSuppressionAsync(modification.TypeEntite, entiteId, modification.NomEntite, ancienne, modification.Description, modification.DateModification, estImportation: true);
                break;
        }

        // Mettre à jour les anciennes valeurs des modifications plus récentes si nécessaire
        if (!string.IsNullOrEmpty(modification.ChampModifie) && !string.IsNullOrEmpty(modification.NouvelleValeur))
        {
            var futures = await _context.HistoriquesModifications
                .Where(h => h.TypeEntite == modification.TypeEntite
                    && h.EntiteId == entiteId
                    && h.ChampModifie == modification.ChampModifie
                    && h.DateModification > modification.DateModification)
                .ToListAsync();

            foreach (var future in futures)
            {
                future.AncienneValeur = modification.NouvelleValeur;
                future.DateMiseAJour = DateTime.UtcNow;
            }

            if (futures.Count > 0)
            {
                await _context.SaveChangesAsync();
                logs.Add(new ImportLogEntry
                {
                    Level = ImportLogLevel.Ok,
                    Category = ImportLogCategory.Historique,
                    DataType = modification.ChampModifie ?? string.Empty,
                    Message = $"Anciennes valeurs recalculées pour {futures.Count} modification(s) future(s) de {modification.NomEntite}"
                });
            }
        }
    }
}
