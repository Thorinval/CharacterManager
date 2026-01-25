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
