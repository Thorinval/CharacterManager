using System.ComponentModel.DataAnnotations;

namespace CharacterManager.Server.Models
{
    public class ClassementFormModel
    {
        [Required(ErrorMessage = "La date est obligatoire")]
        public DateOnly DateEnregistrement { get; set; } = DateOnly.FromDateTime(DateTime.Now);

        [Range(1, int.MaxValue, ErrorMessage = "Valeur obligatoire")]
        public int? Nutaku { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Valeur obligatoire")]
        public int? Top150 { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Valeur obligatoire")]
        public int? France { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Valeur obligatoire")]
        public int? Ligue { get; set; }
    }
}