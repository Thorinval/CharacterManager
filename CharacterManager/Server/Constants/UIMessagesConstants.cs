namespace CharacterManager.Server.Constants;

/// <summary>
/// Constantes des messages d'interface utilisateur communes à toute l'application
/// </summary>
public static class UIMessagesConstants
{
    /// <summary>
    /// Messages d'interface pour les fichiers
    /// </summary>
    public static class FileMessages
    {
        public const string DownloadInitiated = "Téléchargement en cours...";
        public const string FileSelected = "Fichier sélectionné: {0}";
        public const string NoFileSelected = "Aucun fichier sélectionné";
    }

    /// <summary>
    /// Messages de confirmation
    /// </summary>
    public static class ConfirmationMessages
    {
        public const string ConfirmAction = "Êtes-vous sûr ?";
        public const string ConfirmDelete = "Êtes-vous sûr de vouloir supprimer ?";
        public const string ConfirmDestructiveAction = "Cette action est irréversible. Continuer ?";
    }

    /// <summary>
    /// Messages d'état et de traitement
    /// </summary>
    public static class StatusMessages
    {
        public const string Processing = "Traitement en cours...";
        public const string Loading = "Chargement...";
        public const string Saving = "Enregistrement...";
        public const string NoData = "Aucune donnée";
        public const string EmptyResult = "Aucun résultat";
    }

    /// <summary>
    /// Messages de succès génériques
    /// </summary>
    public static class SuccessMessages
    {
        public const string OperationSuccess = "Opération réussie";
        public const string SaveSuccess = "Données enregistrées avec succès";
        public const string UpdateSuccess = "Mise à jour effectuée avec succès";
        public const string DeleteSuccess = "Élément supprimé avec succès";
        public const string RecordAdded = "{0} enregistrement(s) ajouté(s)";
        public const string RecordUpdated = "{0} mis à jour";
    }

    /// <summary>
    /// Messages d'interface génériques
    /// </summary>
    public static class GenericMessages
    {
        public const string Reset = "Réinitialiser";
        public const string Back = "Retour";
        public const string Close = "Fermer";
        public const string Confirm = "Confirmer";
        public const string Cancel = "Annuler";
        public const string Search = "Rechercher";
        public const string Filter = "Filtrer";
        public const string Export = "Exporter";
        public const string Import = "Importer";
    }

    /// <summary>
    /// Toast notification types
    /// </summary>
    public static class ToastTypes
    {
        public const string Success = "success";
        public const string Error = "error";
        public const string Warning = "warning";
        public const string Info = "info";
    }
}
