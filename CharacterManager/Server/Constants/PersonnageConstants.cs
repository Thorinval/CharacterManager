namespace CharacterManager.Server.Constants;

/// <summary>
/// Constantes spécifiques à la gestion des personnages
/// </summary>
public static class PersonnageConstants
{
    /// <summary>
    /// Rarités des personnages
    /// </summary>
    public static class Rarities
    {
        public const string SSR = "SSR";
        public const string SR = "SR";
        public const string R = "R";
    }

    /// <summary>
    /// Types de personnages
    /// </summary>
    public static class Types
    {
        public const string Commandant = "Commandant";
        public const string Mercenaire = "Mercenaire";
        public const string Androide = "Androïde";
    }

    /// <summary>
    /// Rôles des personnages
    /// </summary>
    public static class Roles
    {
        public const string Sentinelle = "Sentinelle";
        public const string Combattante = "Combattante";
    }

    /// <summary>
    /// Factions
    /// </summary>
    public static class Factions
    {
        public const string Syndicat = "Syndicat";
        public const string Pacificateurs = "Pacificateurs";
        public const string HommesLibres = "HommesLibres";
    }

    /// <summary>
    /// Types d'attaque
    /// </summary>
    public static class AttackTypes
    {
        public const string Melee = "Melee";
        public const string MeleeAccent = "Mêlée";
    }

    /// <summary>
    /// Suffixes pour les noms de fichiers images de personnages
    /// </summary>
    public static class ImageSuffixes
    {
        public const string SmallPortrait = "_small_portrait";
        public const string SmallSelect = "_small_select";
        public const string Header = "_header";
    }

    /// <summary>
    /// Éléments XML pour les personnages
    /// </summary>
    public static class XmlElements
    {
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
        public const string TypeAttaque = "TypeAttaque";
        public const string Icon = "Icon";
        public const string HasRelation = "HasRelation";
        public const string NivRelation = "NivRelation";
        public const string Capacites = "Capacites";
        public const string Capacite = "Capacite";
    }

    /// <summary>
    /// Chemins d'images par défaut pour les personnages
    /// </summary>
    public static class DefaultImages
    {
        public const string ImagesPersonnages = "/api/v1/resources/personnages";
        public const string ImagesPersonnagesLegacy = "/images/personnages";
        public const string ImagesAdultes = "/images/personnages/adult";
    }

    /// <summary>
    /// Calculs et constantes de puissance
    /// </summary>
    public static class PowerCalculation
    {
        public const int CommandantRankMultiplier = 20;
        public const int ThumbnailHeightPx = 110;
    }
}
