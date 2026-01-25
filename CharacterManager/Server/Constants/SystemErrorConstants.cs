namespace CharacterManager.Server.Constants;

/// <summary>
/// Constantes des erreurs système et exceptions communes
/// </summary>
public static class SystemErrorConstants
{
    /// <summary>
    /// Erreurs système générales
    /// </summary>
    public static class GeneralErrors
    {
        public const string UnknownError = "Une erreur inconnue s'est produite";
        public const string ErrorDuringOperation = "Erreur lors de l'opération: {0}";
        public const string InvalidInput = "Entrée invalide";
        public const string InvalidFormat = "Format invalide";
    }

    /// <summary>
    /// Erreurs de base de données
    /// </summary>
    public static class DatabaseErrors
    {
        public const string DatabaseConnectionError = "Erreur de connexion à la base de données";
        public const string ErrorSavingToDatabase = "Erreur lors de l'enregistrement en base de données";
        public const string RecordNotFound = "Enregistrement non trouvé";
        public const string RecordAlreadyExists = "Cet enregistrement existe déjà";
        public const string MigrationError = "Erreur lors de la migration de la base de données";
    }

    /// <summary>
    /// Erreurs de fichier et IO
    /// </summary>
    public static class FileErrors
    {
        public const string FileNotFound = "Fichier non trouvé";
        public const string FileAccessDenied = "Accès au fichier refusé";
        public const string FileReadError = "Erreur lors de la lecture du fichier";
        public const string FileWriteError = "Erreur lors de l'écriture du fichier";
        public const string InvalidFileFormat = "Format de fichier invalide";
    }

    /// <summary>
    /// Erreurs de configuration
    /// </summary>
    public static class ConfigurationErrors
    {
        public const string ConfigLoadError = "Erreur lors du chargement de la configuration";
        public const string ConfigNotFound = "Fichier de configuration non trouvé";
        public const string InvalidConfiguration = "Configuration invalide";
    }

    /// <summary>
    /// Erreurs de ressources
    /// </summary>
    public static class ResourceErrors
    {
        public const string ImageLoadError = "Erreur lors du chargement de l'image";
        public const string ResourceNotFound = "Ressource non trouvée";
        public const string ResourcesListError = "Erreur lors du listage des ressources";
        public const string PersonnageImageLoadError = "Erreur lors de la récupération de l'image du personnage";
    }

    /// <summary>
    /// Erreurs de localisation
    /// </summary>
    public static class LocalizationErrors
    {
        public const string ErrorLoadingLanguage = "Erreur lors du chargement de la langue {0}";
        public const string LanguageNotFound = "Langue non trouvée";
        public const string LocalizationKeyNotFound = "Clé de localisation non trouvée: {0}";
    }

    /// <summary>
    /// Erreurs métier génériques
    /// </summary>
    public static class BusinessErrors
    {
        public const string InvalidOperation = "Opération invalide";
        public const string OperationFailed = "L'opération a échoué";
        public const string OperationNotAllowed = "Cette opération n'est pas autorisée";
        public const string InsufficientPermissions = "Permissions insuffisantes";
    }

    /// <summary>
    /// Messages techniquement orientés pour logging
    /// </summary>
    public static class TechnicalMessages
    {
        public const string ExceptionOccurred = "Exception: {0}";
        public const string StackTrace = "Stack trace: {0}";
        public const string InnerException = "Exception interne: {0}";
        public const string DisposalError = "Erreur lors de la libération des ressources";
        public const string CallbackError = "Erreur dans le callback";
    }
}
