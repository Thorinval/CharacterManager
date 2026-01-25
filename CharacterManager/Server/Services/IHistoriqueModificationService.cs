using CharacterManager.Server.Constants;
using CharacterManager.Server.Models;
using System.Diagnostics.CodeAnalysis;

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
        bool estImportation = false);

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
        bool estImportation = false);

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
        bool estImportation = false);

    /// <summary>
    /// Récupère l'historique des modifications d'une entité
    /// </summary>
    Task<List<HistoriqueModification>> GetHistoriqueEntiteAsync(TypeEntite typeEntite, int entiteId);

    /// <summary>
    /// Récupère l'historique des modifications récentes
    /// </summary>
    Task<List<HistoriqueModification>> GetHistoriqueRecentAsync(int nombre = 50);

    /// <summary>
    /// Vide l'historique des modifications
    /// </summary>
    Task ViderHistoriqueAsync();

    /// <summary>
    /// Exporte l'historique en JSON
    /// </summary>
    Task<string> ExporterAsync(DateTime? dateDebut = null, DateTime? dateFin = null);
}
