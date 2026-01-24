using System.Text.Json;
using CharacterManager.Server.Data;
using CharacterManager.Server.Models;
using Microsoft.EntityFrameworkCore;
using CharacterManager.Server.Constants;

namespace CharacterManager.Server.Services;

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
        var maintenant = dateModification ?? DateTime.UtcNow;
        var jourUtc = maintenant.Date;

        // Cherche une modification existante pour le même jour et le même champ
        var modificationJour = await _context.HistoriquesModifications
            .Where(h => h.TypeEntite == typeEntite
                && h.EntiteId == entiteId
                && h.TypeModification == TypeModification.Modification
                && h.ChampModifie == champModifie
                && h.DateModification.Date == jourUtc)
            .OrderByDescending(h => h.DateModification)
            .FirstOrDefaultAsync();

        if (modificationJour != null)
        {
            // Mise à jour de la ligne existante pour le jour
            modificationJour.DateModification = maintenant;
            modificationJour.DateMiseAJour = maintenant;
            // On conserve l'ancienne valeur, on ne met à jour que la nouvelle
            modificationJour.NouvelleValeur = nouvelleValeur != null ? JsonSerializer.Serialize(nouvelleValeur) : null;
            modificationJour.Description = description ?? $"Modification de {champModifie} pour {nomEntite}";
            modificationJour.EstImportation = estImportation;

            _context.HistoriquesModifications.Update(modificationJour);
        }
        else
        {
            // Création d'une nouvelle ligne pour ce jour
            var historique = new HistoriqueModification
            {
                TypeEntite = typeEntite,
                EntiteId = entiteId,
                NomEntite = nomEntite,
                TypeModification = TypeModification.Modification,
                DateModification = maintenant,
                DateInsertion = maintenant,
                DateMiseAJour = maintenant,
                ChampModifie = champModifie,
                AncienneValeur = ancienneValeur != null ? JsonSerializer.Serialize(ancienneValeur) : null,
                NouvelleValeur = nouvelleValeur != null ? JsonSerializer.Serialize(nouvelleValeur) : null,
                Description = description ?? $"Modification de {champModifie} pour {nomEntite}",
                EstImportation = estImportation
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
                    var personnage = await _context.Personnages.FirstOrDefaultAsync(p => p.Nom == modification.NomEntite);

                    if (personnage == null && modification.TypeModification != TypeModification.Suppression)
                    {
                        // Conflit : personnage absent et pas de trace de suppression
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

                        continue;
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

    /// <summary>
    /// Importe l'historique (JSON) avec résolutions de conflits
    /// </summary>
    public async Task<ImportResult> ImportAsync(Stream jsonStream, Dictionary<string, bool> conflictResolutions, List<ImportConflict>? originalConflicts = null)
    {
        var result = new ImportResult();
        var logs = new List<ImportLogEntry>();

        try
        {
            var modifications = await JsonSerializer.DeserializeAsync<List<HistoriqueModification>>(jsonStream, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<HistoriqueModification>();

            // Traiter dans l'ordre chronologique
            foreach (var modification in modifications.OrderBy(m => m.DateModification))
            {
                var date = DateOnly.FromDateTime(modification.DateModification);
                var dataType = modification.ChampModifie ?? modification.TypeModification.ToString();
                var conflictKey = $"{modification.NomEntite}_{dataType}_{date}";

                // Conflit : personnage manquant
                if (modification.TypeEntite == TypeEntite.Personnage)
                {
                    var personnage = await _context.Personnages.FirstOrDefaultAsync(p => p.Nom == modification.NomEntite);

                    if (personnage == null && modification.TypeModification != TypeModification.Suppression)
                    {
                        // Vérifier résolution
                        if (!conflictResolutions.TryGetValue(conflictKey, out var resolved) || !resolved)
                        {
                            logs.Add(new ImportLogEntry
                            {
                                Level = ImportLogLevel.Warning,
                                Category = ImportLogCategory.Historique,
                                DataType = dataType,
                                Message = $"Modification ignorée (non résolue): {modification.NomEntite} ({dataType}) le {date}"
                            });
                            continue;
                        }

                        // Même résolue, sans personnage on ignore l'insertion
                        logs.Add(new ImportLogEntry
                        {
                            Level = ImportLogLevel.Warning,
                            Category = ImportLogCategory.Historique,
                            DataType = dataType,
                            Message = $"Modification ignorée (personnage inexistant): {modification.NomEntite} ({dataType}) le {date}"
                        });
                        continue;
                    }

                    if (personnage != null)
                    {
                        // Appliquer l'entrée en base
                        await ApplyHistoriqueImport(modification, personnage.Id, logs);
                        result.SuccessCount++;
                    }
                }
            }

            result.Logs = logs;
            result.IsSuccess = string.IsNullOrEmpty(result.Error);

            // Rapport des conflits appliqués (ici: ceux marqués comme résolus/ignorés)
            if (originalConflicts != null)
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
        }
        catch (Exception ex)
        {
            result.Error = $"{AppConstants.Messages.ErrorXmlParsing}: {ex.Message}";
            result.IsSuccess = false;
            result.Logs = logs;
        }

        return result;
    }

    private async Task ApplyHistoriqueImport(HistoriqueModification modification, int entiteId, List<ImportLogEntry> logs)
    {
        // Convertir les valeurs JSON en objets pour re-sérialisation
        object? ancienne = null;
        object? nouvelle = null;

        if (!string.IsNullOrWhiteSpace(modification.AncienneValeur))
        {
            try { ancienne = JsonSerializer.Deserialize<object>(modification.AncienneValeur); } catch { }
        }
        if (!string.IsNullOrWhiteSpace(modification.NouvelleValeur))
        {
            try { nouvelle = JsonSerializer.Deserialize<object>(modification.NouvelleValeur); } catch { }
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
