using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Scoreboard.Web.Modelos;
using Scoreboard.Web.ViewModels;

namespace Scoreboard.Web.Controllers
{
    /// <summary>
    /// Controlador MVC para cuentas de usuario: login, registro, logout y acceso denegado.
    ///
    /// Se conecta con:
    /// - ASP.NET Core Identity: para validar usuarios, contrasenas y sesiones.
    /// - LoginViewModel/RegisterViewModel: para recibir datos de formularios.
    /// - Views/Account: para mostrar pantallas de cuenta.
    ///
    /// Flujo simple:
    /// 1. Recibe correo/contrasena o datos de registro.
    /// 2. Usa Identity para autenticar o crear el usuario.
    /// 3. Redirige al home o devuelve errores de validacion.
    ///
    /// Cuidado:
    /// Cambiar rutas, nombres de acciones o modelos puede romper el login.
    /// </summary>
    public class AccountController : Controller
    {
        private readonly SignInManager<UsuarioAplicacion> _signIn;
        private readonly UserManager<UsuarioAplicacion> _users;

        public AccountController(SignInManager<UsuarioAplicacion> signIn, UserManager<UsuarioAplicacion> users)
        {
            _signIn = signIn;
            _users = users;
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> Login(LoginViewModel vm, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            var user = await _users.FindByEmailAsync(vm.Email) ?? await _users.FindByNameAsync(vm.Email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Usuario no encontrado");
                return View(vm);
            }
            var r = await _signIn.PasswordSignInAsync(user, vm.Password, vm.RememberMe, lockoutOnFailure: true);
            if (r.Succeeded) return Redirect(returnUrl ?? Url.Action("Index", "Home")!);
            if (r.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "Cuenta bloqueada temporalmente por varios intentos fallidos. Intenta de nuevo en unos minutos.");
                return View(vm);
            }
            ModelState.AddModelError(string.Empty, "Credenciales inválidas");
            return View(vm);
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Register(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(new RegisterViewModel());
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> Register(RegisterViewModel vm, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            var exists = await _users.FindByEmailAsync(vm.Email);
            if (exists != null)
            {
                ModelState.AddModelError(string.Empty, "El usuario ya existe");
                return View(vm);
            }
            var u = new UsuarioAplicacion
            {
                UserName = vm.Email,
                Email = vm.Email,
                EmailConfirmed = true,
                PhoneNumber = vm.Phone,
                NombreCompleto = vm.FullName.Trim()
            };
            var r = await _users.CreateAsync(u, vm.Password);
            if (r.Succeeded)
            {
                await _signIn.SignInAsync(u, isPersistent: false);
                return Redirect(returnUrl ?? Url.Action("Index", "Home")!);
            }
            foreach (var e in r.Errors) ModelState.AddModelError(string.Empty, e.Description);
            return View(vm);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signIn.SignOutAsync();
            return RedirectToAction("Login");
        }

        public IActionResult AccesoDenegado() => View();
    }
}
