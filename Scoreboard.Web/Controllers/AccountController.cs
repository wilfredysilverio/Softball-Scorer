using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Scoreboard.Web.Modelos.Identity;

namespace Scoreboard.Web.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly SignInManager<UsuarioAplicacion> _signIn;
        private readonly UserManager<UsuarioAplicacion> _users;

        public AccountController(SignInManager<UsuarioAplicacion> signIn, UserManager<UsuarioAplicacion> users)
        {
            _signIn = signIn;
            _users = users;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string usuario, string clave, bool recordar = false, string? returnUrl = null)
        {
            var user = await _users.FindByNameAsync(usuario) ?? await _users.FindByEmailAsync(usuario);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Usuario no encontrado");
                return View();
            }
            var r = await _signIn.PasswordSignInAsync(user, clave, recordar, lockoutOnFailure: false);
            if (r.Succeeded) return Redirect(returnUrl ?? Url.Action("Index", "Home")!);
            ModelState.AddModelError(string.Empty, "Credenciales inválidas");
            return View();
        }

        [HttpGet]
        public IActionResult Register(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string usuario, string email, string clave, string? returnUrl = null)
        {
            var exists = await _users.FindByNameAsync(usuario);
            if (exists != null)
            {
                ModelState.AddModelError(string.Empty, "El usuario ya existe");
                return View();
            }
            var u = new UsuarioAplicacion { UserName = usuario, Email = email, EmailConfirmed = true };
            var r = await _users.CreateAsync(u, clave);
            if (r.Succeeded)
            {
                await _signIn.SignInAsync(u, isPersistent: false);
                return Redirect(returnUrl ?? Url.Action("Index", "Home")!);
            }
            foreach (var e in r.Errors) ModelState.AddModelError(string.Empty, e.Description);
            return View();
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
