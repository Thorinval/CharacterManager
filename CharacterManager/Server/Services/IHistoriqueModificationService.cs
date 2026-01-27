using CharacterManager.Server.Constants;
using CharacterManager.Server.Models;
using CharacterManager.Server.Models.Enums;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace CharacterManager.Server.Services;

/// <summary>
/// Interface du service de gestion de l'historique des modifications
/// </summary>
public interface IHistoriqueModificationService
{
    /// <summary>
    /// Enregistre une création d'entité
    /// </summary>
    Task EnregistrerCreationAsync(
        TypeEntite typeEntite,
        int entiteId,
        string nomEntite,
        object? donnees,
        string? description = null,
        DateTime? dateModification = null,
        bool estImportation = false,
        SourceModification source = SourceModification.NonSpecifiee);

    /// <summary>
    /// Enregistre une modification d'entité
    /// </summary>
    Task EnregistrerModificationAsync(ModificationHistoriqueRequest request);

    /// <summary>
    /// Enregistre une modification d'entité (surcharge compatible avec signature existante)
    /// </summary>
    [SuppressMessage("Design", "CA1025:Replace repetitive arguments with params array", Justification = "Backward compatibility with existing code")]
    [SuppressMessage("Major Code Smell", "S107:Methods should not have more than 7 parameters", Justification = "Backward compatibility with existing code - parameters cannot be reduced")]
    Task EnregistrerModificationAsync(
        TypeEntite typeEntite,
        int entiteId,
        string nomEntite,
        string champModifie,
        object? ancienneValeur,
        object? nouvelleValeur,
        string? description = null,
        DateTime? dateModification = null,
        bool estImportation = false,
        SourceModification source = SourceModification.NonSpecifiee);

    /// <summary>
    /// Enregistre une suppression d'entité
    /// </summary>
    Task EnregistrerSuppressionAsync(
        TypeEntite typeEntite,
        int entiteId,
        string nomEntite,
        object? donnees,
        string? description = null,
        DateTime? dateModification = null,
        bool estImportation = false,
        SourceModification source = SourceModification.NonSpecifiee);

    /// <summary>
    /// Récupère l'historique des modifications d'une entité
    /// </summary>
    Task<List<HistoriqueModification>> GetHistoriqueEntiteAsync(TypeEntite typeEntite, int entiteId);

    /// <summary>
    /// Récupère l'historique avec filtres optionnels
    /// </summary>
    Task<List<HistoriqueModification>> GetHistoriqueAsync(
        TypeEntite? typeEntite = null,
        int? entiteId = null,
        DateTime? dateDebut = null,
        DateTime? dateFin = null,
        int? limit = null);

    /// <summary>
    /// Récupère l'historique des modifications récentes
    /// </summary>
    Task<List<HistoriqueModification>> GetHistoriqueRecentAsync(int nombre = 50);

    /// <summary>
    /// Vide l'historique des modifications
    /// </summary>
    Task ViderHistoriqueAsync();

    /// <summary>
    /// Supprime l'historique plus ancien qu'une date donnée
    /// </summary>
    Task<int> SupprimerHistoriqueAvantAsync(DateTime date);

    /// <summary>
    /// Supprime tout l'historique
    /// </summary>
    Task<int> SupprimerToutAsync();

    /// <summary>
    /// Supprime une entrée spécifique de l'historique
    /// </summary>
    Task<bool> SupprimerAsync(int id);

    /// <summary>
    /// Exporte l'historique en JSON
    /// </summary>
    Task<string> ExporterAsync(DateTime? dateDebut = null, DateTime? dateFin = null);

    /// <summary>
    /// Exporte tout l'historique en JSON sans restriction de dates
    /// </summary>
    Task<string> ExporterToutAsync();

    /// <summary>
    /// Détecte et supprime les doublons dans l'historique
    /// </summary>
    Task<int> NettoyerDoublonsAsync();

    /// <summary>
    /// Compte le nombre d'entrées dans l'historique
    /// </summary>
    Task<int> GetCountAsync(TypeEntite? typeEntite = null);

    /// <summary>
    /// Prévisualise un import d'historique (JSON) sans écrire en base
    /// </summary>
    Task<ImportPreviewResult> PreviewImportAsync(Stream jsonStream);

    /// <summary>
    /// Importe l'historique (JSON) avec résolutions de conflits
    /// </summary>
    Task<ImportResult> ImportAsync(Stream jsonStream, Dictionary<string, bool> conflictResolutions, List<ImportConflict>? originalConflicts = null);

    /// <summary>
    /// Enregistre ou met à jour la puissance de Lucie pour le jour courant.
    /// Gère 2 types : puissance sélectionnée (EstPuissanceMax=false) et puissance max (EstPuissanceMax=true).
    /// Si un enregistrement existe déjà pour le jour, on met à jour la nouvelle valeur.
    /// Sinon, on crée un nouvel enregistrement en reprenant la dernière puissance des jours précédents comme ancienne valeur.
    /// </summary>
    Task EnregistrerPuissanceLucieAsync(bool estPuissanceMax, int puissance, DateTime? dateModification = null, bool estImportation = false, int? ancienneValeur = null);}