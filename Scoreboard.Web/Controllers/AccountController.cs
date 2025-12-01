using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Scoreboard.Web.ViewModels;

namespace Scoreboard.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<IdentityUser> _signIn;
        private readonly UserManager<IdentityUser> _users;

        public AccountController(SignInManager<IdentityUser> signIn, UserManager<IdentityUser> users)
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
            var r = await _signIn.PasswordSignInAsync(user, vm.Password, vm.RememberMe, lockoutOnFailure: false);
            if (r.Succeeded) return Redirect(returnUrl ?? Url.Action("Index", "Home")!);
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
            var u = new IdentityUser { UserName = vm.Email, Email = vm.Email, EmailConfirmed = true, PhoneNumber = vm.Phone };
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
