<<<<<<< HEAD
﻿using Microsoft.AspNetCore.Mvc;
=======
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scoreboard.Web.Datos;
using Scoreboard.Web.Modelos.ViewModels;
>>>>>>> origin/feature/wilfredy-backend-bd

namespace Scoreboard.Web.Controllers
{
    public class EstadisticasController : Controller
    {
<<<<<<< HEAD
        public IActionResult Index()
        {
            // Si quieres pasar un título:
            ViewData["Title"] = "Estadísticas";
=======
        private readonly ContextoMarcador _db;
        public EstadisticasController(ContextoMarcador db) => _db = db;

        public async Task<IActionResult> Index()
        {
            // Agregados generales basados en partidos y jugadores existentes
            var totalPartidos = await _db.Partidos.CountAsync();
            var totalEquipos = await _db.Equipos.CountAsync();
            var totalJugadores = await _db.Jugadores.CountAsync();

            // Carreras totales en todos los partidos
            var totalCarreras = await _db.Partidos.SumAsync(p => p.CarrerasCasa + p.CarrerasVisita);

            ViewData["TotalPartidos"] = totalPartidos;
            ViewData["TotalEquipos"] = totalEquipos;
            ViewData["TotalJugadores"] = totalJugadores;
            ViewData["TotalCarreras"] = totalCarreras;

            // Equipo con más carreras (agregado por equipo)
            // EF Core cannot translate array initializers inside SelectMany in some providers.
            // Instead build two queries (casa/visita) and concat them.
            var casaList = await _db.Partidos.Select(p => new { TeamId = p.EquipoCasaId, Carreras = p.CarrerasCasa }).ToListAsync();
            var visitaList = await _db.Partidos.Select(p => new { TeamId = p.EquipoVisitaId, Carreras = p.CarrerasVisita }).ToListAsync();

            var equiposCarreras = casaList.Concat(visitaList)
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

            // Promedio general = carreras totales / partidos (si hay partidos)
            decimal promedioGeneral = 0m;
            if (totalPartidos > 0)
                promedioGeneral = Math.Round((decimal)totalCarreras / totalPartidos, 2);

            ViewData["TeamLabels"] = System.Text.Json.JsonSerializer.Serialize(teamLabels);
            ViewData["TeamData"] = System.Text.Json.JsonSerializer.Serialize(teamData);
            ViewData["PromedioGeneral"] = promedioGeneral;

            // Equipo con más carreras (message rápido)
            var top = equiposCarreras.FirstOrDefault();
            if (top != null)
            {
                var equipoTop = await _db.Equipos.FindAsync(top.TeamId);
                ViewData["EquipoMasCarreras"] = equipoTop?.Nombre ?? "-";
                ViewData["EquipoMasCarrerasTotal"] = top.Total;
            }

>>>>>>> origin/feature/wilfredy-backend-bd
            return View();
        }
    }
}
