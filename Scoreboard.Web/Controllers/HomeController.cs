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
        var vm = new HomeDashboardVm();

        vm.TotalEquipos = await _db.Equipos.CountAsync();
        vm.TotalJugadores = await _db.Jugadores.CountAsync();
        vm.TotalPartidos = await _db.Partidos.CountAsync();

        vm.ProximoJuego = await _db.Partidos
            .Where(p => p.Fecha > DateTime.UtcNow)
            .OrderBy(p => p.Fecha)
            .Select(p => (DateTime?)p.Fecha)
            .FirstOrDefaultAsync();

        // Últimos 5 jugadores (creados/actualizados) — si no hay campos de fecha específicos, usar Id como proxy
        vm.JugadoresRecientes = await _db.Jugadores
            .AsNoTracking()
            .OrderByDescending(j => j.Id)
            .Take(5)
            .Select(j => new JugadorRecienteVm { Id = j.Id, NombreCompleto = j.Nombre + " " + j.Apellido, Fecha = DateTime.UtcNow })
            .ToListAsync();

        // Últimos 5 partidos (por fecha más reciente)
        vm.PartidosRecientes = await _db.Partidos
            .AsNoTracking()
            .OrderByDescending(p => p.Fecha)
            .Take(5)
            .Select(p => new PartidoRecienteVm { Id = p.Id, Fecha = p.Fecha, Descripcion = (p.EquipoCasaId + " vs " + p.EquipoVisitaId) })
            .ToListAsync();

        return View(vm);
    }

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
