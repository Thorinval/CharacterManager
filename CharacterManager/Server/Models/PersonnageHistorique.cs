namespace CharacterManager.Server.Models;

/// <summary>
/// Représente un personnage historique lié à un enregistrement de classement.
/// </summary>
public class PersonnageHistorique : Personnage
{
    public int HistoriqueClassementId { get; set; }
    public int IdOrigine { get; set; }
}
