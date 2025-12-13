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
    public Rareté Rareté { get; set; }
    public int Niveau { get; set; }
    public TypePersonnage Type { get; set; }
    public int Rang { get; set; }
    public int Puissance { get; set; }  
    public bool Selectionné { get; set; }

    // points d'attaque
    public int PA { get; set; }

    // points de vie
    public int PV { get; set; }

    public Role Role { get; set; }

    public Faction Faction { get; set; }

}