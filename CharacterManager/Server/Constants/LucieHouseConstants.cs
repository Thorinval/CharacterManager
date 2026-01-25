namespace CharacterManager.Server.Constants;

/// <summary>
/// Constantes spécifiques à la Maison de Lucie et ses pièces
/// </summary>
public static class LucieHouseConstants
{
    /// <summary>
    /// Éléments XML pour Lucie House
    /// </summary>
    public static class XmlElements
    {
        public const string LucieHouse = "LucieHouse";
        public const string Piece = "Piece";
        public const string Pieces = "Pieces";
        public const string Lucie = "Lucie";
        public const string BonusTactiques = "BonusTactiques";
        public const string BonusStrategiques = "BonusStrategiques";
        public const string PuissanceTactique = "PuissanceTactique";
        public const string PuissanceStrategique = "PuissanceStrategique";
        public const string Affection = "Affection";
        public const string Bonus = "Bonus";
        public const string Niveau = "Niveau";
    }

    /// <summary>
    /// Champs de l'historique des modifications liés à Lucie House
    /// </summary>
    public static class HistoryFields
    {
        public const string Niveau = "Niveau";
        public const string Puissance = "Puissance";
    }

    /// <summary>
    /// Messages et validation pour Lucie House
    /// </summary>
    public static class ErrorMessages
    {
        public const string ErrorImportPieceLucieHouse = "Erreur lors de l'import d'une pièce Lucie House:";
        public const string WarningTooManyLucieHousePieces = "Attention: Plus de {0} pièces sélectionnées dans l'import";
        public const string ErrorImportLucieHouse = "Erreur lors de l'import de Lucie House:";
    }

    /// <summary>
    /// Limites et constantes de gestion
    /// </summary>
    public static class Limits
    {
        public const int MaxPiecesPerImport = 100;
        public const int WarningThresholdPiecesSelected = 50;
    }

    /// <summary>
    /// Localization keys pour Lucie House
    /// </summary>
    public static class Localization
    {
        public const string LucieHouseKey = "lucie.house";
        public const string PiecesKey = "lucie.pieces";
        public const string TacticalBonusKey = "lucie.tactical.bonus";
        public const string StrategicBonusKey = "lucie.strategic.bonus";
    }
}
