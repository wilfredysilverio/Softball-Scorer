using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scoreboard.Web.Datos;

namespace Scoreboard.Web.Controllers
{
    public class EstadisticasController : Controller
    {
        private readonly ContextoMarcador _db;
        public EstadisticasController(ContextoMarcador db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Estadísticas";

            // Totales generales
            var totalPartidos = await _db.Partidos.CountAsync();
            var totalEquipos = await _db.Equipos.CountAsync();
            var totalJugadores = await _db.Jugadores.CountAsync();
            var totalCarreras = await _db.Partidos.SumAsync(p => p.CarrerasCasa + p.CarrerasVisita);

            ViewData["TotalPartidos"] = totalPartidos;
            ViewData["TotalEquipos"] = totalEquipos;
            ViewData["TotalJugadores"] = totalJugadores;
            ViewData["TotalCarreras"] = totalCarreras;

            // Carreras por equipo (casa y visita)
            var casaList = await _db.Partidos
                .Select(p => new { TeamId = p.EquipoCasaId, Carreras = p.CarrerasCasa })
                .ToListAsync();

            var visitaList = await _db.Partidos
                .Select(p => new { TeamId = p.EquipoVisitaId, Carreras = p.CarrerasVisita })
                .ToListAsync();

            var equiposCarreras = casaList
                .Concat(visitaList)
                .GroupBy(x => x.TeamId)
                .Select(g => new { TeamId = g.Key, Total = g.Sum(x => x.Carreras) })
                .OrderByDescending(x => x.Total)
                .ToList();

            var teamLabels = new List<string>();
            var teamData = new List<int>();

            foreach (var e in equiposCarreras)
            {
                var equipo = await _db.Equipos.FindAsync(e.TeamId);
                teamLabels.Add(equipo?.Nombre ?? ("Equipo " + e.TeamId));
                teamData.Add(e.Total);
            }

            // Promedio general de carreras por partido
            decimal promedioGeneral = 0m;
            if (totalPartidos > 0)
                promedioGeneral = System.Math.Round((decimal)totalCarreras / totalPartidos, 2);

            ViewData["TeamLabels"] = JsonSerializer.Serialize(teamLabels);
            ViewData["TeamData"] = JsonSerializer.Serialize(teamData);
            ViewData["PromedioGeneral"] = promedioGeneral;

            // Equipo con más carreras (texto rápido)
            var top = equiposCarreras.FirstOrDefault();
            if (top != null)
            {
                var equipoTop = await _db.Equipos.FindAsync(top.TeamId);
                ViewData["EquipoMasCarreras"] = equipoTop?.Nombre ?? "-";
                ViewData["EquipoMasCarrerasTotal"] = top.Total;
            }

            return View();
        }
    }
}
