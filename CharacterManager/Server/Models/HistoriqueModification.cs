using CharacterManager.Server.Models.Enums;

namespace CharacterManager.Server.Models;

/// <summary>
/// Type d'entité pour l'historique
/// </summary>
public enum TypeEntite
{
    Personnage,
    Piece
}

/// <summary>
/// Type de modification
/// </summary>
public enum TypeModification
{
    Creation,
    Modification,
    Suppression
}

/// <summary>
/// Représente une entrée dans l'historique des modifications
/// </summary>
public class HistoriqueModification
{
    public int Id { get; set; }
    
    /// <summary>
    /// Type d'entité modifiée (Personnage, Piece)
    /// </summary>
    public TypeEntite TypeEntite { get; set; }
    
    /// <summary>
    /// ID de l'entité modifiée
    /// </summary>
    public int EntiteId { get; set; }
    
    /// <summary>
    /// Nom de l'entité (pour affichage)
    /// </summary>
    public string NomEntite { get; set; } = string.Empty;
    
    /// <summary>
    /// Type de modification (Creation, Modification, Suppression)
    /// </summary>
    public TypeModification TypeModification { get; set; }
    
    /// <summary>
    /// Date et heure de la modification
    /// </summary>
    public DateTime DateModification { get; set; }

    /// <summary>
    /// Date/heure d'insertion de l'enregistrement (UTC)
    /// </summary>
    public DateTime DateInsertion { get; set; }

    /// <summary>
    /// Date/heure de dernière mise à jour (UTC)
    /// </summary>
    public DateTime DateMiseAJour { get; set; }
    
    /// <summary>
    /// Nom du champ modifié (null pour création/suppression)
    /// </summary>
    public string? ChampModifie { get; set; }
    
    /// <summary>
    /// Ancienne valeur (JSON)
    /// </summary>
    public string? AncienneValeur { get; set; }
    
    /// <summary>
    /// Nouvelle valeur (JSON)
    /// </summary>
    public string? NouvelleValeur { get; set; }
    
    /// <summary>
    /// Description de la modification
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Indique si cette modification vient d'une importation (pour l'affichage sans heures/minutes)
    /// </summary>
    public bool EstImportation { get; set; } = false;

    /// <summary>
    /// Source/origine de la modification (Inventaire, Import PML, Import Classement, etc.)
    /// </summary>
    public SourceModification Source { get; set; } = SourceModification.NonSpecifiee;
}
