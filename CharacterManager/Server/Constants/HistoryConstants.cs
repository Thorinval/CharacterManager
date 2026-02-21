namespace CharacterManager.Server.Constants;

/// <summary>
/// Constantes spécifiques à l'historique des modifications et des ligues
/// </summary>
public static class HistoryConstants
{
    /// <summary>
    /// Éléments XML pour l'historique
    /// </summary>
    public static class XmlElements
    {
        public const string HistoriqueLigue = "HistoriqueLigue";
        public const string DateMontee = "DateMontee";
        public const string Ligue = "Ligue";
        public const string Notes = "Notes";
    }

    /// <summary>
    /// Champs de modification suivi dans l'historique
    /// </summary>
    public static class ModificationFields
    {
        public const string Selectionne = "Selectionne";
        public const string Rang = "Rang";
        public const string Niveau = "Niveau";
        public const string Puissance = "Puissance";
    }

    /// <summary>
    /// Types d'entités trackées
    /// </summary>
    public static class EntityTypes
    {
        public const string Personnage = "Personnage";
        public const string Piece = "Piece";
    }

    /// <summary>
    /// Messages d'erreur pour l'historique
    /// </summary>
    public static class ErrorMessages
    {
        public const string ErrorImportHistory = "Erreur lors de l'import d'historique";
        public const string ErrorExportHistory = "Erreur lors de l'export d'historique";
        public const string ErrorLoadingHistory = "Erreur lors du chargement de l'historique";
        public const string ErrorRecordingDeletion = "Erreur lors de l'ajout des suppressions dans l'historique";
    }

    /// <summary>
    /// Messages d'interface utilisateur pour l'historique
    /// </summary>
    public static class UIMessages
    {
        public const string HistoryExportSuccess = "Export d'historique effectué";
        public const string ConflictResolution = "Résoudre les conflits";
        public const string PreviewReady = "Pré-rapport: {0} entrée(s) prête(s) à être importée(s).";
        public const string DuplicatesIgnored = "{0} doublon(s) ignoré(s)";
    }

    /// <summary>
    /// Limites et constantes de gestion
    /// </summary>
    public static class Management
    {
        public const int MaxLeagueHistoryRecordsToExport = 100;
        public const int DefaultLeagueHistoryRecordsToFetch = 50;
    }

    /// <summary>
    /// Localization keys pour historique
    /// </summary>
    public static class Localization
    {
        public const string HistoryKey = "history";
        public const string LeagueHistoryKey = "league.history";
        public const string ModificationHistoryKey = "modification.history";
    }
}
