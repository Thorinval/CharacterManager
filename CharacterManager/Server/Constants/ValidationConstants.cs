namespace CharacterManager.Server.Constants;

/// <summary>
/// Constantes de validation communes à l'application
/// </summary>
public static class ValidationConstants
{
    /// <summary>
    /// Messages de validation de champs
    /// </summary>
    public static class FieldValidation
    {
        public const string FieldRequired = "{0} est requis";
        public const string FieldInvalid = "{0} est invalide";
        public const string FieldEmpty = "{0} ne peut pas être vide";
        public const string FieldTooShort = "{0} est trop court (minimum {1} caractères)";
        public const string FieldTooLong = "{0} est trop long (maximum {1} caractères)";
    }

    /// <summary>
    /// Messages de validation de données
    /// </summary>
    public static class DataValidation
    {
        public const string InvalidEmailFormat = "Format d'email invalide";
        public const string InvalidPhoneFormat = "Format de téléphone invalide";
        public const string InvalidNumberFormat = "Format de nombre invalide";
        public const string InvalidDateFormat = "Format de date invalide";
        public const string NumberOutOfRange = "Nombre en dehors de la plage autorisée ({0} à {1})";
    }

    /// <summary>
    /// Messages de validation de fichiers
    /// </summary>
    public static class FileValidation
    {
        public const string InvalidFileExtension = "Extension de fichier invalide: {0}";
        public const string FileTooLarge = "Le fichier est trop volumineux (maximum {0} MB)";
        public const string FileEmpty = "Le fichier est vide";
        public const string UnsupportedFileType = "Type de fichier non supporté: {0}";
    }

    /// <summary>
    /// Messages de validation métier
    /// </summary>
    public static class BusinessValidation
    {
        public const string DuplicateEntry = "Cette entrée existe déjà";
        public const string ItemNotFound = "{0} non trouvé";
        public const string ItemAlreadyExists = "{0} existe déjà";
        public const string CannotPerformAction = "Impossible de {0}";
        public const string PrerequisiteMissing = "Prérequis manquant: {0}";
    }

    /// <summary>
    /// Limites communes
    /// </summary>
    public static class Limits
    {
        public const int MaxStringLength = 500;
        public const int MaxNameLength = 100;
        public const int MaxDescriptionLength = 1000;
        public const int MaxBulkOperationSize = 100;
        public const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB
    }
}
