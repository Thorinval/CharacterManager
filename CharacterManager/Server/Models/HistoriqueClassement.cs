
namespace CharacterManager.Server.Models
{
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
        /// Classement Ligue
        /// </summary>
        public int Ligue { get; set; }

        /// <summary>
        /// Classement global
        /// </summary>
        public List<Classement> Classements { get; set; } = [];

        public List<Personnage> Mercenaires { get; set; } = [];
        public int? CommandantId { get; set; }
        public Personnage? Commandant { get; set; }
        public List<Personnage> Androides { get; set; } = [];

        public int PuissanceTotal { get; set; }

        public List<Piece> Pieces { get; set; } = [];
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
}