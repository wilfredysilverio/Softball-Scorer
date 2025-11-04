using System.ComponentModel.DataAnnotations;

namespace Scoreboard.Web.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Escribe tu correo.")]
        [EmailAddress(ErrorMessage = "Escribe un correo válido.")]
        [Display(Name = "Correo")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Escribe tu contraseña.")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Password { get; set; } = "";

        [Display(Name = "Mantenerme conectado")]
        public bool RememberMe { get; set; }
    }
}
