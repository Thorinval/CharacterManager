namespace CharacterManager.Server.Constants;

/// <summary>
/// Constantes spécifiques au module Statistiques
/// </summary>
public static class StatisticsConstants
{
    /// <summary>
    /// Champs de l'historique des modifications utilisés en statistiques
    /// </summary>
    public static class HistoryFields
    {
        public const string Niveau = "Niveau";
        public const string Selectionne = "Selectionne";
        public const string Puissance = "Puissance";
        public const string Rang = "Rang";
        public const string PuissanceLucieSelectionnee = "PuissanceLucieSelectionnee";
        public const string PuissanceLucieMax = "PuissanceLucieMax";
    }

    /// <summary>
    /// Valeurs booléennes en string pour l'historique
    /// </summary>
    public static class BooleanValues
    {
        public const string True = "true";
        public const string False = "false";
    }

    /// <summary>
    /// Noms des mois pour le formatage des dates
    /// </summary>
    public static class MonthNames
    {
        public static readonly string[] Values = { "JAN", "FEV", "MAR", "AVR", "MAI", "JUN", "JUL", "AOU", "SEP", "OCT", "NOV", "DEC" };
    }

    /// <summary>
    /// Clés de localisation pour le module statistiques
    /// </summary>
    public static class LocalizationKeys
    {
        public const string SelectedTeamPower = "statistics.selectedTeamPower";
        public const string BestTeamPower = "statistics.bestTeamPower";
        public const string ChartTitleTeamPowerEvolution = "statistics.chartTitleTeamPowerEvolution";
        public const string ChartTitleClassementEvolution = "statistics.chartTitleClassementEvolution";
        public const string ClassementNutaku = "statistics.classementNutaku";
        public const string ClassementTop150 = "statistics.classementTop150";
        public const string ClassementFrance = "statistics.classementFrance";
    }

    /// <summary>
    /// Couleurs utilisées pour les graphiques
    /// </summary>
    public static class Colors
    {
        public const string PrimaryPurple = "#667eea";
        public const string SecondaryPurple = "#764ba2";
        public const string AccentPink = "#f093fb";
        public const string Melee = "#FF6384";
        public const string Distance = "#36A2EB";
        public const string Androide = "#FFCE56";
        public const string Commandant = "#4BC0C0";
        public const string Syndicat = "#9966FF";
        public const string Pacificateurs = "#FF9F40";
        public const string HommesLibres = "#4BC0C0";
    }

    /// <summary>
    /// Paramètres de formatage des graphiques
    /// </summary>
    public static class ChartFormatting
    {
        public const double AlphaFill = 0.1;
        public const int BorderWidth = 2;
        public const int PointRadius = 3;
        public const int PointHoverRadius = 5;
        public const double Tension = 0.3;
    }

    /// <summary>
    /// Messages d'erreur pour le module statistiques
    /// </summary>
    public static class ErrorMessages
    {
        public const string ErrorCreatingCharts = "Erreur lors de la création des graphiques";
        public const string ErrorCreatingLevelEvolutionChart = "Erreur lors de la création du graphique d'évolution";
        public const string ErrorCreatingPowerEvolutionChart = "Erreur lors de la création du graphique de puissance";
        public const string ErrorCreatingClassementEvolutionChart = "Erreur lors de la création du graphique de classement";
    }
}
