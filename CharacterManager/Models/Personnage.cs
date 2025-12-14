using Microsoft.AspNetCore.Identity;

namespace CharacterManager.Models;

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

    // Images du personnage
    public string ImageUrlDetail { get; set; } = string.Empty;        // Pour la vue détail (grande image)
    public string ImageUrlPreview { get; set; } = string.Empty;       // Pour les aperçus (escouade, inventaire) - _small_portrait.png
    public string ImageUrlSelected { get; set; } = string.Empty;      // Quand le personnage est sélectionné - _small_select.png

    // Description et capacités
    public string Description { get; set; } = string.Empty;
    public List<Capacite> Capacites { get; set; } = new();

    // Type d'attaque
    public TypeAttaque TypeAttaque { get; set; }
}