using System.ComponentModel.DataAnnotations;

namespace Scoreboard.Web.ViewModels
{
    /// <summary>
    /// ViewModel del formulario de login.
    ///
    /// Se conecta con:
    /// - AccountController.Login.
    /// - Views/Account/Login.cshtml.
    /// - ASP.NET Core Identity.
    ///
    /// Flujo simple:
    /// 1. Recibe correo, contrasena y RememberMe.
    /// 2. AccountController valida contra Identity.
    /// 3. Si es correcto, inicia sesion.
    ///
    /// Cuidado:
    /// Cambiar nombres de propiedades requiere revisar el formulario de login.
    /// </summary>
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
