namespace CharacterManager.Server.Constants;
/// <summary>
/// Constantes globales de l'application CharacterManager
/// </summary>
public static class AppConstants
{
    /// <summary>
    /// Chemins de routage de l'application
    /// </summary>
    public static class Routes
    {
        public const string Home = "/";
        public const string Inventaire = "/inventaire";
        public const string Templates = "/templates";
        public const string Historique = "/classements";
        public const string ImportPml = "/import-pml";
        public const string Escouade = "/escouade";
        public const string MeilleurEscouade = "/meilleur-escouade";
        public const string Login = "/login";
        public const string Logout = "/api/logout";
        public const string ChangePassword = "/change-password";
    }
    /// <summary>
    /// Extensions de fichiers supportées
    /// </summary>
    public static class FileExtensions
    {
        public const string Pml = ".pml";
        public const string Xml = ".xml";
        public const string Json = ".json";
        public const string Png = ".png";
        public const string Jpg = ".jpg";
    }

    /// <summary>
    /// Chemins et répertoires de l'application
    /// </summary>
    public static class Paths
    {
        // Images de personnages désormais servies via la DLL de ressources (v0.12.1+)
        public const string ImagesPersonnages = "/api/v1/resources/personnages";
        public const string ImagesPersonnagesLegacy = "/images/personnages"; // Pour compatibilité v0.12.0
        public const string ImagesAdultes = "/images/personnages/adult";
        // Images d'interface désormais servies via la DLL de ressources
        public const string ImagesInterface = "/api/v1/resources/interface";
        public const string I18nFolder = "i18n";
        public const string WwwRoot = "wwwroot";

        // Images par défaut
        public const string DefaultPortrait = "/api/v1/resources/interface/default_portrait.png";
        public const string GenericCommandantHeader = "/api/v1/resources/interface/fondheader.png";
        public const string HomeDefaultBackground = "/api/v1/resources/interface/fondheader.png";
    }

    /// <summary>
    /// Noms de fichiers de configuration
    /// </summary>
    public static class ConfigFiles
    {
        // NOTE: PersonnagesConfig.json removed - now using filesystem-based detection
        // Images in /adult/ subdirectory are automatically treated as adult content
        public const string Database = "charactermanager.db";
    }

    /// <summary>
    /// Formats de date/heure
    /// </summary>
    public static class DateTimeFormats
    {
        public const string FileNameDateTime = "yyyyMMdd_HHmmss";
        public const string IsoDateTime = "yyyy-MM-ddTHH:mm:ssZ";
    }

    /// <summary>
    /// Messages et libellés de l'application
    /// </summary>
    public static class Messages
    {
        // Messages de succès
        public const string SuccessImport = "Import réussi";
        public const string SuccessExport = "Export réussi";

        // Messages d'information
        public const string InfoProcessing = "Traitement en cours...";
    }

    /// <summary>
    /// Valeurs par défaut de l'application
    /// </summary>
    public static class Defaults
    {
        public const string AppVersion = "Unknown";
        public const string DefaultLanguage = "fr";
        public const string DefaultRole = "utilisateur";
        public const int ThumbnailHeightPx = 110;
        public const bool IsAdultModeEnabled = true;

        public const string ViewModeGrid = "grid";
        public const string ViewModeList = "list";
    }

    /// <summary>
    /// Valeurs booléennes communes en string (pour historique, API, etc.)
    /// </summary>
    public static class BooleanStrings
    {
        public const string True = "true";
        public const string False = "false";
    }
}
