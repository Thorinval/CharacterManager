
namespace CharacterManager.Server.Models;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Représente un enregistrement historique du classement à une date donnée
/// </summary>
public class HistoriqueClassement
{
    public int Id { get; set; }

    /// <summary>
    /// Date de l'enregistrement
    /// </summary>
    public DateOnly DateEnregistrement { get; set; } = DateOnly.FromDateTime(DateTime.Now);

    /// <summary>
    /// Score classement
    /// </summary>
    public int Score { get; set; }

    /// <summary>
    /// Classement Ligue
    /// </summary>
    public int Ligue { get; set; }

    /// <summary>
    /// Puissance du commandant au moment de l'enregistrement
    /// </summary>
    public int PuissanceCommandant { get; set; }

    /// <summary>
    /// Puissance des mercenaires et androïdes au moment de l'enregistrement
    /// </summary>
    public int PuissanceMercenaires { get; set; }

    /// <summary>
    /// Puissance des pièces Lucie au moment de l'enregistrement
    /// </summary>
    public int PuissanceLucie { get; set; }

    /// <summary>
    /// Classement global
    /// </summary>
    public List<Classement> Classements { get; set; } = [];

    public List<PersonnageHistorique> Mercenaires { get; set; } = [];
    public int? CommandantId { get; set; }
    public PersonnageHistorique? Commandant { get; set; }
    public List<PersonnageHistorique> Androides { get; set; } = [];

    public int PuissanceTotale { get; set; }

    public List<PieceHistorique> Pieces { get; set; } = [];
}

public enum TypeClassement
{
    Nutaku,
    Top150,
    France
}

public class Classement
{
    [Key]
    public int Id { get; set; }
    public string Nom { get; set; } = string.Empty;
    public TypeClassement Type { get; set; }
    public int Valeur { get; set; } = 0;
}
