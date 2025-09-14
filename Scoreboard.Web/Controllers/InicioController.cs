using Microsoft.AspNetCore.Mvc;

namespace Scoreboard.Web.Controllers
{
    public class InicioController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
