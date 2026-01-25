namespace CharacterManager.Server.Constants;

/// <summary>
/// Constantes spécifiques aux classements et historiques de classement
/// </summary>
public static class ClassementConstants
{
    /// <summary>
    /// Types de classement
    /// </summary>
    public static class Types
    {
        public const string Nutaku = "Nutaku";
        public const string Top150 = "Top150";
        public const string Pays = "Pays";
    }

    /// <summary>
    /// Éléments XML pour les classements
    /// </summary>
    public static class XmlElements
    {
        public const string HistoriqueClassement = "HistoriqueClassement";
        public const string Classements = "Classements";
        public const string ClassementItem = "ClassementItem";
        public const string TypeClassement = "TypeClassement";
        public const string Valeur = "Valeur";
        public const string Score = "Score";
        public const string PuissanceCommandant = "PuissanceCommandant";
        public const string PuissanceMercenaires = "PuissanceMercenaires";
        public const string PuissanceLucie = "PuissanceLucie";
        public const string Date = "Date";
        public const string Mercenaires = "Mercenaires";
        public const string DateEnregistrement = "DateEnregistrement";
        public const string Ligue = "Ligue";
        public const string PuissanceTotal = "PuissanceTotal";
        public const string Nom = "Nom";
    }

    /// <summary>
    /// Champs de l'historique des modifications liés aux classements
    /// </summary>
    public static class HistoryFields
    {
        public const string Rang = "Rang";
    }

    /// <summary>
    /// Localisation et labels pour les classements
    /// </summary>
    public static class Localization
    {
        public const string NutakuKey = "classement.nutaku";
        public const string Top150Key = "classement.top150";
        public const string PaysKey = "classement.pays";
        public const string ClassementKey = "classement";
        public const string RankingKey = "ranking";
    }

    /// <summary>
    /// Constantes de sauvegarde et gestion
    /// </summary>
    public static class Management
    {
        public const int MaxHistoryRecordsToExport = 100;
        public const int DefaultHistoryRecordsToFetch = 50;
    }
}
