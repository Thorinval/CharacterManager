namespace CharacterManager.Server.Constants;

/// <summary>
/// Constantes globales de l'application CharacterManager
/// </summary>
public static class AppConstants
{
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
        public const string ImagesPersonnages = "/images/personnages";
        public const string ImagesAdultes = "/images/personnages/adult";
        public const string ImagesInterface = "/images/interface";
        public const string I18nFolder = "i18n";
        public const string WwwRoot = "wwwroot";
        
        // Images par défaut
        public const string DefaultPortrait = "/images/interface/default_portrait.png";
        public const string GenericCommandantHeader = "/images/interface/empty_header.png";
        public const string HunterHeader = "/images/personnages/adult/hunter_header.png";
        public const string HomeDefaultBackground = "/images/interface/fondheader.png";
    }

    /// <summary>
    /// Suffixes pour les noms de fichiers images
    /// </summary>
    public static class ImageSuffixes
    {
        public const string SmallPortrait = "_small_portrait";
        public const string SmallSelect = "_small_select";
        public const string Header = "_header";
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
    /// Préfixes pour les noms de fichiers d'export
    /// </summary>
    public static class ExportPrefixes
    {
        public const string Inventaire = "inventaire";
        public const string Template = "template";
        public const string HistoriqueClassements = "historique_classements";
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
    /// Éléments XML/PML
    /// </summary>
    public static class XmlElements
    {
        // Éléments racine
        public const string InventairePML = "InventairePML";
        public const string TemplatesPML = "TemplatesPML";
        public const string HistoriqueClassements = "HistoriqueClassements";
        
        // Sections
        public const string Inventaire = "inventaire";
        public const string Templates = "templates";
        public const string Template = "template";
        
        // Éléments de personnage
        public const string Personnage = "Personnage";
        public const string Nom = "Nom";
        public const string Rarete = "Rarete";
        public const string Type = "Type";
        public const string Puissance = "Puissance";
        public const string PA = "PA";
        public const string PV = "PV";
        public const string Niveau = "Niveau";
        public const string Rang = "Rang";
        public const string Role = "Role";
        public const string Faction = "Faction";
        public const string Selectionne = "Selectionne";
        public const string Description = "Description";
        
        // Attributs
        public const string Version = "version";
        public const string ExportDate = "exportDate";
    }

    /// <summary>
    /// Messages et libellés de l'application
    /// </summary>
    public static class Messages
    {
        // Messages d'erreur
        public const string ErrorFileEmpty = "Le fichier est vide";
        public const string ErrorFileInvalid = "Le fichier n'est pas valide";
        public const string ErrorXmlParsing = "Erreur lors de l'analyse du fichier XML";
        public const string ErrorNoSectionsFound = "Aucune section reconnue trouvée dans le fichier";
        
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
    }

    /// <summary>
    /// Limites de l'application
    /// </summary>
    public static class Limits
    {
        public const int MaxCommandants = 1;
        public const int MaxMercenaires = 8;
        public const int MaxAndroides = 3;
        public const int MaxEscouadeTotal = 12;
        public const int MaxTemplateNameLength = 100;
        public const int MaxDescriptionLength = 500;
        public const int MaxPersonnagesJsonLength = 2000;
    }

    /// <summary>
    /// Clés de routage
    /// </summary>
    public static class Routes
    {
        public const string Home = "/";
        public const string Inventaire = "/inventaire";
        public const string Templates = "/templates";
        public const string Historique = "/historique";
        public const string ImportPml = "/import-pml";
        public const string Escouade = "/escouade";
        public const string MeilleurEscouade = "/meilleur-escouade";
        public const string Login = "/login";
        public const string Logout = "/api/logout";
        public const string ChangePassword = "/change-password";
    }

    /// <summary>
    /// Encodages
    /// </summary>
    public static class Encodings
    {
        public const string Utf8 = "UTF-8";
    }

    /// <summary>
    /// Types MIME
    /// </summary>
    public static class MimeTypes
    {
        public const string ApplicationXml = "application/xml";
        public const string ApplicationJson = "application/json";
        public const string ApplicationOctetStream = "application/octet-stream";
    }
}
