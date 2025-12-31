namespace CharacterManager.Server.Models;

/// <summary>
/// Représente une pièce historique liée à un enregistrement de classement.
/// </summary>
public class PieceHistorique : Piece
{
    public int HistoriqueClassementId { get; set; }

    public int IdOrigine { get; set; }
}
