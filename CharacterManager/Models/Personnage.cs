namespace CharacterManager.Models;

public class Personnage
{
    public int Id { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string Classe { get; set; } = string.Empty;
    public int Niveau { get; set; }
    public string Race { get; set; } = string.Empty;
}