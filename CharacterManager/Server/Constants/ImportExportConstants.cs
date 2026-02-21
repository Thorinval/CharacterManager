namespace CharacterManager.Server.Constants;

/// <summary>
/// Constantes spécifiques à l'import/export PML (XML personnalisé)
/// </summary>
public static class ImportExportConstants
{
    /// <summary>
    /// Types de données pour les logs d'import
    /// </summary>
    public static class DataTypes
    {
        public const string Puissance = "Puissance";
        public const string Composition = "Composition";
        public const string Classement = "Classement";
    }

    /// <summary>
    /// Éléments XML/PML spécifiques à l'import/export
    /// </summary>
    public static class XmlElements
    {
        // Racines et conteneurs
        public const string CharacterManagerPML = "CharacterManagerPML";
        public const string InventairePML = "InventairePML";
        public const string TemplatesPML = "TemplatesPML";
        public const string HistoriqueClassements = "HistoriqueClassements";

        // Sections principales
        public const string Inventaire = "inventaire";
        public const string Templates = "templates";
        public const string Template = "template";
        public const string MeilleurEscouade = "meilleurEscouade";
        public const string HistoriqueClassement = "HistoriqueClassement";
        public const string HistoriqueLigue = "HistoriqueLigue";

        // Personnage et dérivés
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
        public const string Androïde = "Androïde";

        // Capacités
        public const string Capacites = "Capacites";
        public const string Capacite = "Capacite";
        public const string Id = "Id";

        // Classements
        public const string Classements = "Classements";
        public const string ClassementItem = "ClassementItem";
        public const string TypeClassement = "TypeClassement";
        public const string Valeur = "Valeur";
        public const string Mercenaires = "Mercenaires";
        public const string Androides = "Androides";
        public const string Pieces = "Pieces";
        public const string Score = "Score";
        public const string PuissanceCommandant = "PuissanceCommandant";
        public const string PuissanceMercenaires = "PuissanceMercenaires";
        public const string PuissanceLucie = "PuissanceLucie";
        public const string Date = "Date";

        // Ligue
        public const string DateMontee = "DateMontee";
        public const string Ligue = "Ligue";
        public const string Notes = "Notes";

        // Lucie House
        public const string LucieHouse = "LucieHouse";
        public const string Piece = "Piece";
        public const string BonusTactiques = "BonusTactiques";
        public const string BonusStrategiques = "BonusStrategiques";
        public const string PuissanceTactique = "PuissanceTactique";
        public const string PuissanceStrategique = "PuissanceStrategique";
        public const string PuissanceLegacy = "Puissance";
        public const string Affection = "Affection";
        public const string Lucie = "Lucie";

        // Attributs XML
        public const string Version = "Version";
        public const string ExportDate = "exportDate";
        public const string TypeAttaque = "TypeAttaque";
        public const string TypeEntite = "TypeEntite";
        public const string Nutaku = "Nutaku";
        public const string Top150 = "Top150";
        public const string Pays = "Pays";
    }

    /// <summary>
    /// Préfixes pour les fichiers d'export
    /// </summary>
    public static class ExportPrefixes
    {
        public const string Inventaire = "inventaire";
        public const string Template = "template";
        public const string HistoriqueClassements = "historique_classements";
        public const string HistoriqueLigues = "historique_ligues";
    }

    /// <summary>
    /// Options d'export PML
    /// </summary>
    public static class ExportOptions
    {
        public const string ExportTypeInventory = "inventory";
        public const string ExportTypeTemplates = "templates";
        public const string ExportTypeBestSquad = "bestSquad";
        public const string ExportTypeLeagueHistory = "leagueHistory";
        public const string ExportTypeCapacities = "capacities";
    }

    /// <summary>
    /// Messages d'erreur spécifiques à l'import/export
    /// </summary>
    public static class ErrorMessages
    {
        // Erreurs de fichier
        public const string ErrorFileEmpty = "Le fichier est vide";
        public const string ErrorFileInvalid = "Le fichier n'est pas valide";
        public const string ErrorNoSectionsFound = "Aucune section valide trouvée dans le fichier";
        public const string ErrorXmlParsing = "Erreur lors de l'analyse du fichier XML";

        // Erreurs d'import spécifiques
        public const string ErrorImportPersonnageInventaire = "Erreur lors de l'import de personnage (inventaire):";
        public const string ErrorImportPersonnageTemplate = "Erreur lors de l'import du personnage au template";
        public const string ErrorImportTemplate = "Erreur lors de l'import du template:";
        public const string ErrorTemplateNoName = "Un template doit avoir un nom";
        
        // Erreurs Lucie House
        public const string WarningTooManyLucieHousePieces = "Attention: Plus de {0} pièces sélectionnées dans l'import";
        
        // Erreurs historique
        public const string ErrorHistoriqueInvalide = "Historique invalide: date ou données manquantes";

        // Messages génériques d'erreur
        public const string ErrorFileNotSelected = "Veuillez sélectionner un fichier PML ou XML.";
        public const string ErrorImportFormat = "Format d'import non supporté";
    }

    /// <summary>
    /// Messages de succès et notifications
    /// </summary>
    public static class SuccessMessages
    {
        public const string ImportSuccess = "Import réussi";
        public const string ExportSuccess = "Export réussi";
        public const string ImportFormatDetected = "{0} enregistrement(s) importé(s) avec succès.";
        public const string NoRecordsImported = "Aucun enregistrement importé.";
        public const string ImportDetails = "Détails (aperçu):";
    }
}
