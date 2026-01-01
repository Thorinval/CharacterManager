namespace CharacterManager.Server.Models;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Représente un passage dans une ligue à une date donnée
/// </summary>
public class HistoriqueLigue
{
    public int Id { get; set; }

    /// <summary>
    /// Date du passage dans la ligue
    /// </summary>
    [Required]
    public DateOnly DatePassage { get; set; } = DateOnly.FromDateTime(DateTime.Now);

    /// <summary>
    /// Numéro de la ligue (1-25) ou 50 pour Elite Top 50
    /// </summary>
    [Required]
    [Range(1, 50)]
    public int Ligue { get; set; }

    /// <summary>
    /// Notes optionnelles
    /// </summary>
    public string? Notes { get; set; }
}
