namespace CharacterManager.Server.Models;

using Microsoft.AspNetCore.Identity;
using CharacterManager.Server.Services;

public enum Rarete
{
    R,
    SR,
    SSR,
    Inconnu
}

public enum TypePersonnage
{
    Mercenaire,
    Commandant,
    Androide,
    Inconnu
}

public enum Role
{
    Sentinelle,
    Combattante,
    Androide,
    Commandant,
    Inconnu
}

public enum Faction
{
    Syndicat,
    Pacificateurs,
    HommesLibres,
    Inconnu
}

public enum TypeAttaque
{
    Melee,
    Distance,
    Androide,
    Commandant,
    Inconnu
}

public class Personnage
{
    public int Id { get; set; }
    public string Nom { get; set; } = string.Empty;
    public Rarete Rarete { get; set; }
    public int Niveau { get; set; }
    public TypePersonnage Type { get; set; }
    public int Rang { get; set; }
    public int Puissance { get; set; }
    public bool Selectionne { get; set; }

    // points d'attaque
    public int PA { get; set; }

    // points de vie
    public int PV { get; set; }

    public Role Role { get; set; }
    public Faction Faction { get; set; }

    // Images du personnage - calculées dynamiquement via l'API de ressources (v0.12.1+)
    // Les images sont maintenant servies depuis la DLL CharacterManager.Resources.Personnages
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string ImageUrlDetail => PersonnageImageUrlHelper.GetImageDetailUrl(Nom);
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string ImageUrlPreview => PersonnageImageUrlHelper.GetImageSmallPortraitUrl(Nom);
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string ImageUrlSelected => PersonnageImageUrlHelper.GetImageSmallSelectUrl(Nom);

    // Colonnes stockées pour compatibilité avec l'ancien schéma (remplies automatiquement au save)
    [System.ComponentModel.DataAnnotations.Schema.Column("ImageUrlDetail")]
    public string ImageUrlDetailStored { get; set; } = string.Empty;
    [System.ComponentModel.DataAnnotations.Schema.Column("ImageUrlPreview")]
    public string ImageUrlPreviewStored { get; set; } = string.Empty;
    [System.ComponentModel.DataAnnotations.Schema.Column("ImageUrlSelected")]
    public string ImageUrlSelectedStored { get; set; } = string.Empty;
    public string ImageUrlHeader { get; set; } = string.Empty;        // Pour l'image de fond du header

    /// <summary>
    /// Retourne l'URL de l'image appropriée selon le contexte.
    /// - Inventaire: _small_select.png si sélectionné, sinon _small_portrait.png
    /// - MeilleurEscouade: _small_select.png si sélectionné, sinon _small_portrait.png
    /// </summary>
    public string GetImageUrl(bool useSelectionState = false)
    {
        if (useSelectionState && Selectionne)
        {
            return ImageUrlSelected;
        }
        return ImageUrlPreview;
    }

    // Description et capacités
    public string Description { get; set; } = string.Empty;
    public List<Capacite> Capacites { get; set; } = new();

    // Type d'attaque
    public TypeAttaque TypeAttaque { get; set; }
}