using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scoreboard.Web.Datos;
using Scoreboard.Web.Modelos;
using Scoreboard.Web.Modelos.ViewModels;

namespace Scoreboard.Web.Controllers;

/// <summary>
/// Controlador MVC del inicio operativo del sistema.
///
/// Se conecta con:
/// - ContextoMarcador: para leer totales, partidos recientes y lideres.
/// - HomeDashboardVm: para enviar datos preparados a la vista.
/// - Views/Home/Index.cshtml: para mostrar el resumen principal.
///
/// Flujo simple:
/// 1. Consulta datos generales de la base.
/// 2. Arma un dashboard con partidos, jugadores y estadisticas.
/// 3. Devuelve la pantalla de inicio.
///
/// Cuidado:
/// Si se agregan metricas nuevas, evita meter reglas pesadas aqui; usa servicios.
/// </summary>
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
        var vm = new HomeDashboardVm
        {
            TotalEquipos = await _db.Equipos.CountAsync(),
            TotalJugadores = await _db.Jugadores.CountAsync(),
            TotalPartidos = await _db.Partidos.CountAsync(),
            PartidosEnCurso = await _db.Partidos.CountAsync(p => p.Estado == EstadoPartido.EnCurso),
            CarrerasAnotadas = await _db.Partidos.Select(p => (int?)(p.CarrerasCasa + p.CarrerasVisita)).SumAsync() ?? 0,
            Jonrones = await _db.PlayerBattingStats.Select(s => (int?)s.HR).SumAsync() ?? 0
        };

        vm.ProximoJuego = await _db.Partidos
            .Where(p => p.Fecha > DateTime.Now && p.Estado != EstadoPartido.Finalizado)
            .OrderBy(p => p.Fecha)
            .Select(p => (DateTime?)p.Fecha)
            .FirstOrDefaultAsync();

        vm.PartidoPrincipal = await _db.Partidos
            .AsNoTracking()
            .Include(p => p.EquipoCasa)
            .Include(p => p.EquipoVisita)
            .OrderByDescending(p => p.Estado == EstadoPartido.EnCurso)
            .ThenBy(p => p.Fecha)
            .ThenByDescending(p => p.Fecha)
            .Select(p => new PartidoPanelVm
            {
                Id = p.Id,
                EquipoCasa = p.EquipoCasa != null ? p.EquipoCasa.Nombre : "Casa",
                EquipoVisita = p.EquipoVisita != null ? p.EquipoVisita.Nombre : "Visita",
                CarrerasCasa = p.CarrerasCasa,
                CarrerasVisita = p.CarrerasVisita,
                EntradaActual = p.EntradaActual,
                Mitad = p.Mitad.ToString(),
                Outs = p.Outs,
                Estado = p.Estado.ToString(),
                Fecha = p.Fecha
            })
            .FirstOrDefaultAsync();

        vm.JugadoresRecientes = await _db.Jugadores
            .AsNoTracking()
            .OrderByDescending(j => j.Id)
            .Take(5)
            .Select(j => new JugadorRecienteVm
            {
                Id = j.Id,
                NombreCompleto = j.Nombre + " " + j.Apellido,
                Fecha = DateTime.Now
            })
            .ToListAsync();

        vm.PartidosRecientes = await _db.Partidos
            .AsNoTracking()
            .Include(p => p.EquipoCasa)
            .Include(p => p.EquipoVisita)
            .OrderByDescending(p => p.Fecha)
            .Take(5)
            .Select(p => new PartidoRecienteVm
            {
                Id = p.Id,
                Fecha = p.Fecha,
                Descripcion = (p.EquipoVisita != null ? p.EquipoVisita.Nombre : "Visita") + " vs " + (p.EquipoCasa != null ? p.EquipoCasa.Nombre : "Casa"),
                Estado = p.Estado.ToString(),
                CarrerasCasa = p.CarrerasCasa,
                CarrerasVisita = p.CarrerasVisita
            })
            .ToListAsync();

        vm.MejoresJugadores = await ObtenerMejoresJugadoresAsync();

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
        var env = HttpContext.RequestServices.GetService<IWebHostEnvironment>();
        if (env == null || !env.IsDevelopment()) return Forbid();

        await SeedData.EnsureSeedDataAsync(HttpContext.RequestServices);
        return RedirectToAction(nameof(Index));
    }

    private async Task<List<JugadorTopVm>> ObtenerMejoresJugadoresAsync()
    {
        var stats = await _db.PlayerBattingStats
            .AsNoTracking()
            .Include(s => s.Jugador)
            .ThenInclude(j => j!.Equipo)
            .ToListAsync();

        return stats
            .Where(s => s.Jugador != null)
            .GroupBy(s => s.Jugador!)
            .Select(g =>
            {
                var ab = g.Sum(s => s.AB);
                var h = g.Sum(s => s.H);
                var bb = g.Sum(s => s.BB);
                var hbp = g.Sum(s => s.HBP);
                var sf = g.Sum(s => s.SF);
                var doubles = g.Sum(s => s.Doubles);
                var triples = g.Sum(s => s.Triples);
                var hr = g.Sum(s => s.HR);
                var totalBases = (h - doubles - triples - hr) + (2 * doubles) + (3 * triples) + (4 * hr);
                var avg = ab == 0 ? 0m : Math.Round((decimal)h / ab, 3, MidpointRounding.AwayFromZero);
                var obpDen = ab + bb + hbp + sf;
                var obp = obpDen == 0 ? 0m : Math.Round((decimal)(h + bb + hbp) / obpDen, 3, MidpointRounding.AwayFromZero);
                var slg = ab == 0 ? 0m : Math.Round((decimal)totalBases / ab, 3, MidpointRounding.AwayFromZero);

                return new JugadorTopVm
                {
                    Id = g.Key.Id,
                    NombreCompleto = $"{g.Key.Nombre} {g.Key.Apellido}".Trim(),
                    Equipo = g.Key.Equipo?.Nombre ?? "Sin equipo",
                    AB = ab,
                    H = h,
                    HR = hr,
                    RBI = g.Sum(s => s.RBI),
                    AVG = avg,
                    OPS = obp + slg
                };
            })
            .Where(j => j.AB > 0)
            .OrderByDescending(j => j.OPS)
            .ThenByDescending(j => j.AVG)
            .ThenByDescending(j => j.HR)
            .Take(5)
            .ToList();
    }
}
