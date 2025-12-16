using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CharacterManager.Server.Models;

/// <summary>
/// Représente un enregistrement historique de l'escouade à une date donnée
/// </summary>
public class HistoriqueEscouade
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Date de l'enregistrement
    /// </summary>
    [Required]
    public DateTime DateEnregistrement { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Puissance totale de l'escouade
    /// </summary>
    public int PuissanceTotal { get; set; }

    /// <summary>
    /// Classement de l'escouade (si applicable)
    /// </summary>
    public int? Classement { get; set; }

    /// <summary>
    /// JSON contenant les données des personnages de l'escouade
    /// Format: { mercenaires: [...], commandant: {...}, androides: [...] }
    /// </summary>
    [Required]
    public string DonneesEscouadeJson { get; set; } = "{}";
}

/// <summary>
/// Données de personnel pour l'historique
/// </summary>
public class PersonnelHistorique
{
    public int Id { get; set; }
    public string Nom { get; set; } = string.Empty;
    public int Niveau { get; set; }
    public int Rang { get; set; }
    public string Rarete { get; set; } = string.Empty;
    public int Puissance { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
}

/// <summary>
/// Structure des données sérialisées de l'escouade
/// </summary>
public class DonneesEscouadeSerialisees
{
    [JsonPropertyName("mercenaires")]
    public List<PersonnelHistorique> Mercenaires { get; set; } = new();

    [JsonPropertyName("commandant")]
    public PersonnelHistorique? Commandant { get; set; }

    [JsonPropertyName("androides")]
    public List<PersonnelHistorique> Androides { get; set; } = new();

    [JsonPropertyName("luciePuissance")]
    public int LuciePuissance { get; set; } = 0;

    [JsonPropertyName("ligue")]
    public int Ligue { get; set; } = 0;

    [JsonPropertyName("nutaku")]
    public int Nutaku { get; set; } = 0;

    [JsonPropertyName("top150")]
    public int Top150 { get; set; } = 0;

    [JsonPropertyName("pays")]
    public int Pays { get; set; } = 0;

    [JsonPropertyName("score")]
    public int Score { get; set; } = 0;
}
