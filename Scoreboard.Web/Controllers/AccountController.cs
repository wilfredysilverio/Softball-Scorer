using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Scoreboard.Web.ViewModels;

namespace Scoreboard.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<IdentityUser> _signIn;
        private readonly UserManager<IdentityUser> _users;
        private readonly ILogger<AccountController> _logger;

        public AccountController(SignInManager<IdentityUser> signIn, UserManager<IdentityUser> users, ILogger<AccountController> logger)
        {
            _signIn = signIn;
            _users = users;
            _logger = logger;
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
        public async Task<IActionResult> Login(LoginViewModel vm, string? returnUrl = null)
        {
            try
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
                var r = await _signIn.PasswordSignInAsync(user, vm.Password, vm.RememberMe, lockoutOnFailure: false);
                if (r.Succeeded) return Redirect(returnUrl ?? Url.Action("Index", "Home")!);
                ModelState.AddModelError(string.Empty, "Credenciales inválidas");
                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado en Login");
                TempData["Error"] = "Hay un error en el sistema. Intenta más tarde.";
                return View(vm);
            }
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
        public async Task<IActionResult> Register(RegisterViewModel vm, string? returnUrl = null)
        {
            try
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
                var u = new IdentityUser { UserName = vm.Email, Email = vm.Email, EmailConfirmed = true, PhoneNumber = vm.Phone };
                var r = await _users.CreateAsync(u, vm.Password);
                if (r.Succeeded)
                {
                    // En lugar de iniciar sesión automáticamente, mostrar mensaje de éxito
                    TempData["Success"] = "Cuenta creada correctamente, ahora puedes iniciar sesión.";
                    return RedirectToAction("Login");
                }
                foreach (var e in r.Errors) ModelState.AddModelError(string.Empty, e.Description);
                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado en Register");
                TempData["Error"] = "Hay un error en el sistema. Intenta más tarde.";
                return View(vm);
            }
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            try
            {
                await _signIn.SignOutAsync();
                return RedirectToAction("Login", "Account");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en Logout");
                TempData["Error"] = "Hay un error en el sistema.";
                return RedirectToAction("Login", "Account");
            }
        }

        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
