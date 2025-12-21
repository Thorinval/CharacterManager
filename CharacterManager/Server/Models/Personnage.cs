using Microsoft.AspNetCore.Identity;

namespace CharacterManager.Server.Models;

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
    Androïde,
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
    Mêlée, 
    Distance, 
    Androïde,
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

    // Images du personnage - calculées dynamiquement selon le nom
    public string ImageUrlDetail => $"/images/personnages/{Nom.ToLower().Replace(" ", "_")}.png";
    public string ImageUrlPreview => $"/images/personnages/{Nom.ToLower().Replace(" ", "_")}_small_portrait.png";
    public string ImageUrlSelected => $"/images/personnages/{Nom.ToLower().Replace(" ", "_")}_small_select.png";
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