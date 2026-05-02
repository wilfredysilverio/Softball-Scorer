using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Scoreboard.Web.Modelos
{
    /// <summary>
    /// Usuario real de la aplicacion.
    ///
    /// Se conecta con:
    /// - ASP.NET Core Identity: hereda correo, contrasena, telefono y seguridad.
    /// - AccountController: guarda el nombre completo al registrar una cuenta.
    /// - AspNetUsers: tabla donde Identity guarda los usuarios.
    ///
    /// Flujo simple:
    /// 1. El usuario se registra con nombre, correo, telefono y contrasena.
    /// 2. Identity guarda la cuenta en AspNetUsers.
    /// 3. La aplicacion puede usar NombreCompleto para mostrar quien esta usando el sistema.
    ///
    /// Cuidado:
    /// Si se agregan campos nuevos aqui, normalmente hace falta una migracion.
    /// </summary>
    public class UsuarioAplicacion : IdentityUser
    {
        [Required, StringLength(120)]
        public string NombreCompleto { get; set; } = "";
    }
}
