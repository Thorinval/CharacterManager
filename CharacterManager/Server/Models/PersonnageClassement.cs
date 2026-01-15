namespace CharacterManager.Server.Models;

using CharacterManager.Server.Services;

/// <summary>
/// Représente un personnage lié à un enregistrement de classement.
/// Contrairement à PersonnageHistorique, ce modèle ne dérive pas de Personnage 
/// et permet la sélection et édition manuelle lors de la création d'un classement.
/// </summary>
public class PersonnageClassement
{
    public int Id { get; set; }
    
    /// <summary>
    /// ID du personnage d'origine dans l'inventaire
    /// </summary>
    public int IdOrigine { get; set; }
    
    /// <summary>
    /// Nom du personnage (copié depuis l'inventaire)
    /// </summary>
    public string Nom { get; set; } = string.Empty;
    
    /// <summary>
    /// Rareté du personnage
    /// </summary>
    public Rarete Rarete { get; set; }
    
    /// <summary>
    /// Niveau du personnage au moment du classement
    /// </summary>
    public int Niveau { get; set; }
    
    /// <summary>
    /// Type de personnage (Mercenaire, Commandant, Androide)
    /// </summary>
    public TypePersonnage Type { get; set; }
    
    /// <summary>
    /// Rang du personnage (nombre d'étoiles)
    /// </summary>
    public int Rang { get; set; }
    
    /// <summary>
    /// Puissance du personnage au moment du classement
    /// </summary>
    public int Puissance { get; set; }
    
    // Helper methods for UI compatibility
    /// <summary>
    /// Retourne l'URL de l'image appropriée selon le contexte.
    /// </summary>
    public string GetImageUrl(bool useSelectionState = false)
    {
        return PersonnageImageUrlHelper.GetImageSmallPortraitUrl(Nom);
    }
    
    /// <summary>
    /// Crée un PersonnageClassement à partir d'un Personnage de l'inventaire
    /// </summary>
    public static PersonnageClassement FromPersonnage(Personnage p)
    {
        return new PersonnageClassement
        {
            IdOrigine = p.Id,
            Nom = p.Nom,
            Niveau = p.Niveau,
            Rang = p.Rang,
            Rarete = p.Rarete,
            Puissance = p.Puissance,
            Type = p.Type
        };
    }
}
