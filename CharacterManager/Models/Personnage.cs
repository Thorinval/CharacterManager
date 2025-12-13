using Microsoft.AspNetCore.Identity;

namespace CharacterManager.Models;

public enum Rareté
{
    R,
    SR,
    SSR
}

public enum TypePersonnage
{
    Mercenaire,
    Commandant,
    Androide
}

public enum Role
{
    Sentinelle,
    Combattante,
    Androide,
    Commandant
}

public enum Faction
{
    Syndicat,
    Pacificateurs,
    HommesLibres
}

public class Personnage
{
    public int Id { get; set; }
    public string Nom { get; set; } = string.Empty;
    public Rareté Rarete { get; set; }
    public int Niveau { get; set; }
    public TypePersonnage Type { get; set; }
    public int Rang { get; set; }
    public int Puissance { get; set; }  
    public bool Selectionne { get; set; }

    // points d'attaque
    public int PA { get; set; }
    public int PAMax { get; set; }

    // points de vie
    public int PV { get; set; }
    public int PVMax { get; set; }

    public Role Role { get; set; }
    public Faction Faction { get; set; }

    // Image du personnage
    public string ImageUrl { get; set; } = string.Empty;

    // Description et capacités
    public string Description { get; set; } = string.Empty;
    public List<Capacite> Capacites { get; set; } = new();

    // Statistiques
    public int Sante { get; set; }
    public int SanteMax { get; set; }
    public string Localisation { get; set; } = string.Empty;
}