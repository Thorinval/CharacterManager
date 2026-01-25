namespace CharacterManager.Server.Constants;

/// <summary>
/// Constantes spécifiques aux templates d'escouade
/// </summary>
public static class TemplateConstants
{
    /// <summary>
    /// Éléments XML pour les templates
    /// </summary>
    public static class XmlElements
    {
        public const string TemplatesPML = "TemplatesPML";
        public const string Template = "template";
        public const string Templates = "templates";
        public const string Nom = "Nom";
        public const string Description = "Description";
        public const string Personnage = "Personnage";
    }

    /// <summary>
    /// Préfixes pour les fichiers d'export de templates
    /// </summary>
    public static class ExportPrefixes
    {
        public const string Template = "template";
    }

    /// <summary>
    /// Messages et validation pour les templates
    /// </summary>
    public static class Validation
    {
        public const string ErrorTemplateNoName = "Un template doit avoir un nom";
        public const string ErrorImportTemplate = "Erreur lors de l'import du template:";
        public const string ErrorImportPersonnageTemplate = "Erreur lors de l'import du personnage au template";
    }

    /// <summary>
    /// Messages d'interface utilisateur pour les templates
    /// </summary>
    public static class UIMessages
    {
        public const string TemplateCreatedSuccess = "Template '{0}' créé avec succès";
        public const string TemplateExportSuccess = "Export de '{0}' effectué";
        public const string TemplateNotFound = "Template introuvable";
        public const string TemplateDeletionFailed = "Suppression échouée";
        public const string TemplateRenameSuccess = "Template renommé";
        public const string TemplateRenameFailed = "Échec du renommage";
        public const string ConfirmDelete = "Supprimer ce template ?";
        public const string EmptyTemplateName = "Le nom ne peut pas être vide";
        public const string ErrorCreatingTemplate = "Erreur lors de la création du template";
        public const string ErrorLoadingTemplate = "Erreur lors du chargement du template";
        public const string ErrorExportingTemplate = "Erreur lors de l'export";
    }

    /// <summary>
    /// Limites et constantes de gestion
    /// </summary>
    public static class Management
    {
        public const int MaxTemplatesPerExport = 100;
    }

    /// <summary>
    /// Localization keys pour templates
    /// </summary>
    public static class Localization
    {
        public const string TemplateKey = "template";
        public const string TemplatesKey = "templates";
    }
}
