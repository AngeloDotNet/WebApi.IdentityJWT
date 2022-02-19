using System;
using System.ComponentModel.DataAnnotations;

namespace BlazorDev.Autentica.Models.Models.InputModels
{
    public class RegisterInputModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email address")]
        public string Email { get; set; } = String.Empty;

        [Required]
        [DataType(DataType.Password)]
        [StringLength(50, ErrorMessage = "La password deve essere lunga tra {2} e {1} caratteri", MinimumLength = 6)]
        public string Password { get; set; } = String.Empty;
    }
}
