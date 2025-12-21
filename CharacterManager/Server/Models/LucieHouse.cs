using System.ComponentModel.DataAnnotations.Schema;

namespace CharacterManager.Server.Models;

public class Aspect
{
    public string Nom { get; set; } = string.Empty;
    public int Puissance { get; set; } = 0;
    public List<string> Bonus { get; set; } = new();
}

/// <summary>
/// Représente une pièce de la Lucie House avec ses bonus.
/// </summary>
public class Piece
{
    public int Id { get; set; }
    public string Nom { get; set; } = string.Empty;
    public int Niveau { get; set; }
    public bool Selectionnee { get; set; }

    // Aspects tactiques et stratégiques, sérialisés en JSON en base.
    public Aspect AspectsTactiques { get; set; } = new() { Nom = "Aspects tactiques", Puissance = 0 };
    public Aspect AspectsStrategiques { get; set; } = new() { Nom = "Aspects stratégiques", Puissance = 0 };

    /// <summary>
    /// Puissance calculée: somme des puissances tactiques et stratégiques.
    /// </summary>
    public int Puissance => (AspectsTactiques?.Puissance ?? 0) + (AspectsStrategiques?.Puissance ?? 0);

    /// <summary>
    /// Liste des bonus tactiques (alias pratique pour la sérialisation/affichage).
    /// </summary>
    [NotMapped]
    public List<string> BonusTactiques
    {
        get => AspectsTactiques.Bonus;
        set => AspectsTactiques.Bonus = value ?? new();
    }

    /// <summary>
    /// Liste des bonus stratégiques (alias pratique pour la sérialisation/affichage).
    /// </summary>
    [NotMapped]
    public List<string> BonusStrategiques
    {
        get => AspectsStrategiques.Bonus;
        set => AspectsStrategiques.Bonus = value ?? new();
    }

    /// <summary>
    /// Alias pour compatibilité avec l'ancien modèle.
    /// </summary>
    [NotMapped]
    public int PuissanceTotale => Puissance;
}

/// <summary>
/// Représente la Lucie House qui gère les bonus pour les escouades.
/// Maximum 2 pièces peuvent être sélectionnées simultanément.
/// </summary>
public class LucieHouse
{
    public int Id { get; set; }

    /// <summary>
    /// Liste des pièces disponibles dans la Lucie House.
    /// </summary>
    public List<Piece> Pieces { get; set; } = new();

    /// <summary>
    /// Puissance totale calculée (somme des puissances des pièces sélectionnées).
    /// </summary>
    public int PuissanceTotale => Pieces.Where(p => p.Selectionnee).Sum(p => p.Puissance);

    /// <summary>
    /// Nombre maximum de pièces pouvant être sélectionnées.
    /// </summary>
    public const int MaxPiecesSelectionnees = 2;

    /// <summary>
    /// Obtient le nombre de pièces actuellement sélectionnées.
    /// </summary>
    public int NombrePiecesSelectionnees => Pieces.Count(p => p.Selectionnee);

    /// <summary>
    /// Vérifie si une pièce peut être sélectionnée.
    /// </summary>
    public bool PeutSelectionner() => NombrePiecesSelectionnees < MaxPiecesSelectionnees;

    /// <summary>
    /// Sélectionne une pièce si le maximum n'est pas atteint.
    /// </summary>
    public bool SelectionnerPiece(string nomPiece)
    {
        var piece = Pieces.FirstOrDefault(p => p.Nom.Equals(nomPiece, StringComparison.OrdinalIgnoreCase));
        if (piece == null || piece.Selectionnee)
        {
            return false;
        }

        if (!PeutSelectionner())
        {
            return false;
        }

        piece.Selectionnee = true;
        return true;
    }

    /// <summary>
    /// Désélectionne une pièce.
    /// </summary>
    public bool DeselectionnerPiece(string nomPiece)
    {
        var piece = Pieces.FirstOrDefault(p => p.Nom.Equals(nomPiece, StringComparison.OrdinalIgnoreCase));
        if (piece == null || !piece.Selectionnee)
        {
            return false;
        }

        piece.Selectionnee = false;
        return true;
    }

    /// <summary>
    /// Initialise une Lucie House avec les 5 pièces par défaut.
    /// </summary>
    public static LucieHouse CreerDefaut()
    {
        return new LucieHouse
        {
            Pieces =
            [
                new Piece { Nom = "Salon", Niveau = 1 },
                new Piece { Nom = "Bar", Niveau = 1 },
                new Piece { Nom = "Café", Niveau = 1 },
                new Piece { Nom = "Gymnase", Niveau = 1 },
                new Piece { Nom = "Chambre", Niveau = 1 }
            ]
        };
    }
}
