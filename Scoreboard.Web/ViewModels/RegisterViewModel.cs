using System.ComponentModel.DataAnnotations;

namespace Scoreboard.Web.ViewModels
{
    /// <summary>
    /// ViewModel del formulario de registro.
    ///
    /// Se conecta con:
    /// - AccountController.Register.
    /// - Views/Account/Register.cshtml.
    /// - ASP.NET Core Identity.
    ///
    /// Flujo simple:
    /// 1. Recibe datos del nuevo usuario.
    /// 2. Valida contrasena y confirmacion.
    /// 3. AccountController crea el usuario.
    ///
    /// Cuidado:
    /// Cambiar validaciones puede afectar quien logra registrarse.
    /// </summary>
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Escribe tu nombre.")]
        [Display(Name = "Nombre completo")]
        public string FullName { get; set; } = "";

        [Required(ErrorMessage = "Escribe tu correo.")]
        [EmailAddress(ErrorMessage = "Escribe un correo válido.")]
        [Display(Name = "Correo")]
        public string Email { get; set; } = "";

        [Display(Name = "Teléfono")]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Crea una contraseña.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Usa al menos 8 caracteres.")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Password { get; set; } = "";

        [Required(ErrorMessage = "Repite la contraseña.")]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden.")]
        [Display(Name = "Confirmar contraseña")]
        public string ConfirmPassword { get; set; } = "";
    }
}
