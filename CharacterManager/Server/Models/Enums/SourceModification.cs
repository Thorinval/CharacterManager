namespace CharacterManager.Server.Models.Enums;

/// <summary>
/// Source/origine d'une modification dans l'historique
/// </summary>
public enum SourceModification
{
    /// <summary>
    /// Source inconnue ou non spécifiée (pour compatibilité avec données existantes)
    /// </summary>
    NonSpecifiee = 0,
    
    /// <summary>
    /// Modification manuelle via l'interface Inventaire (utilisateur a modifié un personnage/pièce)
    /// </summary>
    Inventaire = 1,
    
    /// <summary>
    /// Import PML de l'inventaire (création/mise à jour via fichier PML)
    /// </summary>
    ImportPml = 2,
    
    /// <summary>
    /// Import de classement (modifications générées lors de l'insertion d'un classement)
    /// </summary>
    ImportClassement = 3
}
