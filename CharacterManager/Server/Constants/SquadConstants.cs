namespace CharacterManager.Server.Constants;

/// <summary>
/// Constantes spécifiques à la gestion des escouades et meilleure escouade
/// </summary>
public static class SquadConstants
{
    /// <summary>
    /// Éléments XML pour les escouades
    /// </summary>
    public static class XmlElements
    {
        public const string MeilleurEscouade = "meilleurEscouade";
        public const string Escouade = "Escouade";
        public const string Androides = "Androides";
        public const string Pieces = "Pieces";
        public const string Mercenaires = "Mercenaires";
        public const string Commandant = "Commandant";
    }

    /// <summary>
    /// Prefix d'export pour les escouades
    /// </summary>
    public static class ExportPrefixes
    {
        public const string BestSquad = "meilleur_escouade";
    }

    /// <summary>
    /// Messages d'erreur pour escouades
    /// </summary>
    public static class ErrorMessages
    {
        public const string ErrorImportBestSquad = "Erreur lors de l'import de la meilleure escouade:";
    }

    /// <summary>
    /// Modes d'affichage des escouades
    /// </summary>
    public static class ViewModes
    {
        public const string Grid = "grid";
        public const string List = "list";
    }

    /// <summary>
    /// Constantes de calcul de puissance
    /// </summary>
    public static class PowerCalculation
    {
        public const int CommandantRankBonus = 20;
    }

    /// <summary>
    /// Localization keys pour escouades
    /// </summary>
    public static class Localization
    {
        public const string SelectedSquadKey = "squad.selected";
        public const string BestSquadKey = "squad.best";
        public const string SquadPowerKey = "squad.power";
    }
}
