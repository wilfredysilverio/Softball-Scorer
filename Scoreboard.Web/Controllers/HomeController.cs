using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Scoreboard.Web.Modelos.ViewModels;
using Scoreboard.Web.Modelos;
using Scoreboard.Web.Datos;
using Microsoft.EntityFrameworkCore;

namespace Scoreboard.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ContextoMarcador _db;

    public HomeController(ILogger<HomeController> logger, ContextoMarcador db)
    {
        _logger = logger;
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        // Redirigir a Login si el usuario no está autenticado
        if (!User.Identity?.IsAuthenticated ?? true)
        {
            return RedirectToAction("Login", "Account");
        }

        // Totales
        ViewBag.TotalEquipos = await _db.Equipos.CountAsync();
        ViewBag.TotalJugadores = await _db.Jugadores.CountAsync();
        ViewBag.TotalPartidos = await _db.Partidos.CountAsync();

        // Próximo juego
        var proximoJuego = await _db.Partidos
            .Where(p => p.Fecha > DateTime.UtcNow)
            .OrderBy(p => p.Fecha)
            .Select(p => (DateTime?)p.Fecha)
            .FirstOrDefaultAsync();

        ViewBag.ProximoJuego = proximoJuego.HasValue 
            ? proximoJuego.Value.ToString("dd/MM/yyyy HH:mm") 
            : null;

        // Últimos 5 jugadores
        var jugadoresRecientes = await _db.Jugadores
            .AsNoTracking()
            .Include(j => j.Equipo)
            .OrderByDescending(j => j.Id)
            .Take(5)
            .ToListAsync();

        ViewBag.UltimosJugadores = jugadoresRecientes.Select(j => new
        {
            j.Id,
            j.Nombre,
            j.Apellido,
            FechaTexto = "Reciente"
        }).ToList();

        // Últimos 5 partidos
        var partidosRecientes = await _db.Partidos
            .AsNoTracking()
            .Include(p => p.EquipoCasa)
            .Include(p => p.EquipoVisita)
            .OrderByDescending(p => p.Fecha)
            .Take(5)
            .ToListAsync();

        ViewBag.UltimosPartidos = partidosRecientes.Select(p => new
        {
            p.Id,
            Titulo = $"{p.EquipoCasa?.Nombre ?? "Casa"} vs {p.EquipoVisita?.Nombre ?? "Visita"}",
            FechaTexto = p.Fecha.ToString("dd/MM/yyyy HH:mm")
        }).ToList();

        return View();
    }

    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SeedDevData()
    {
        // Sólo en Development
        var env = HttpContext.RequestServices.GetService<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
        if (env == null || !env.IsDevelopment()) return Forbid();

        await Scoreboard.Web.Datos.SeedData.EnsureSeedDataAsync(HttpContext.RequestServices);
        return RedirectToAction(nameof(Index));
    }
}
