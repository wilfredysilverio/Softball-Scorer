using Microsoft.AspNetCore.Mvc;
using Scoreboard.Web.Modelos;
using Scoreboard.Web.Modelos.ViewModels;


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
