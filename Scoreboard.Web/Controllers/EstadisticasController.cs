using Microsoft.AspNetCore.Mvc;

namespace Scoreboard.Web.Controllers
{
    public class EstadisticasController : Controller
    {
        public IActionResult Index()
        {
            // Si quieres pasar un título:
            ViewData["Title"] = "Estadísticas";
            return View();
        }
    }
}
