using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CharacterManager.Server.Models
{
    public class ClassementFormModel : IValidatableObject
    {
        [Required(ErrorMessage = "La date est obligatoire")]
        public DateOnly DateEnregistrement { get; set; } = DateOnly.FromDateTime(DateTime.Now);

        [Required(ErrorMessage = "Le classement Nutaku est obligatoire")]
        [Range(1, int.MaxValue, ErrorMessage = "La valeur doit être positive")]
        public int? Nutaku { get; set; }

        [Required(ErrorMessage = "Le classement Top 150 est obligatoire")]
        [Range(1, int.MaxValue, ErrorMessage = "La valeur doit être positive")]
        public int? Top150 { get; set; }

        [Required(ErrorMessage = "Le classement France est obligatoire")]
        [Range(1, int.MaxValue, ErrorMessage = "La valeur doit être positive")]
        public int? France { get; set; }

        [Required(ErrorMessage = "La ligue est obligatoire")]
        public int? Ligue { get; set; }

        [Required(ErrorMessage = "Le score est obligatoire")]
        [Range(1, int.MaxValue, ErrorMessage = "Le score doit être positif")]
        public int? Score { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Ligue.HasValue && !(Ligue.Value >= 1 && Ligue.Value <= 25 || Ligue.Value == 50))
            {
                yield return new ValidationResult(
                    "La ligue doit être comprise entre 25 et 1, ou Elite TOP 50.",
                    new[] { nameof(Ligue) });
            }
        }
    }
}