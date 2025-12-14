using System.ComponentModel.DataAnnotations;

namespace CharacterManager.Server.Models;

/// <summary>
/// Représente un template d'escouade sauvegardé
/// </summary>
public class Template
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Nom { get; set; } = string.Empty;

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Puissance totale de l'escouade dans ce template
    /// </summary>
    public int PuissanceTotal { get; set; }

    /// <summary>
    /// Date de création du template
    /// </summary>
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Date de dernière modification
    /// </summary>
    public DateTime DateModification { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Liste des personnages du template (jusqu'à 12 : 1 commandant + 8 mercenaires + 3 androïdes)
    /// Stockée en JSON pour simplifier la persistence
    /// </summary>
    [StringLength(2000)]
    public string PersonnagesJson { get; set; } = string.Empty;

    /// <summary>
    /// Récupère la liste des IDs de personnages du template
    /// </summary>
    public List<int> GetPersonnageIds()
    {
        if (string.IsNullOrEmpty(PersonnagesJson))
            return [];

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<List<int>>(PersonnagesJson) ?? [];
        }
        catch
        {
            return [];
        }
    }

    /// <summary>
    /// Définit la liste des IDs de personnages du template
    /// </summary>
    public void SetPersonnageIds(List<int> ids)
    {
        PersonnagesJson = System.Text.Json.JsonSerializer.Serialize(ids);
    }
}
