using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Scoreboard.Web.Datos;
using Scoreboard.Web.Modelos.ViewModels;

namespace Scoreboard.Web.Servicios
{
    public class EstadisticasService : IEstadisticasService
    {
        private readonly ContextoMarcador _db;

        public EstadisticasService(ContextoMarcador db)
        {
            _db = db;
        }

        public async Task<EstadisticasJugadorVm?> ObtenerEstadisticasJugadorAsync(int jugadorId)
        {
            var jugador = await _db.Jugadores
                .AsNoTracking()
                .Include(j => j.Equipo)
                .FirstOrDefaultAsync(j => j.Id == jugadorId);

            if (jugador == null) return null;
            // Obtener estadísticas de bateo por jugador
            var stats = await _db.PlayerBattingStats
                .AsNoTracking()
                .Where(s => s.JugadorId == jugadorId)
                .ToListAsync();

            var partidos = await _db.Partidos
                .AsNoTracking()
                .Where(p => p.EquipoCasaId == jugador.EquipoId || p.EquipoVisitaId == jugador.EquipoId)
                .ToListAsync();

            var vm = new EstadisticasJugadorVm
            {
                Jugador = jugador,
                PartidosJugados = partidos.Count,
                AB = stats.Sum(s => s.AB),
                H = stats.Sum(s => s.H),
                R = stats.Sum(s => s.R),
                Doubles = stats.Sum(s => s.Doubles),
                Triples = stats.Sum(s => s.Triples),
                HR = stats.Sum(s => s.HR),
                RBI = stats.Sum(s => s.RBI),
                BB = stats.Sum(s => s.BB),
                SO = stats.Sum(s => s.SO),
                HBP = stats.Sum(s => s.HBP),
                SF = stats.Sum(s => s.SF)
            };

            // Calculos
            vm.AVG = (vm.AB > 0) ? (decimal)vm.H / vm.AB : 0m;
            var obpDen = vm.AB + vm.BB + vm.HBP + vm.SF;
            vm.OBP = (obpDen > 0) ? (decimal)(vm.H + vm.BB + vm.HBP) / obpDen : 0m;
            var totalBases = vm.H + vm.Doubles + (2 * vm.Triples) + (3 * vm.HR); // simplificado: 1*H + extra
            vm.SLG = (vm.AB > 0) ? (decimal)totalBases / vm.AB : 0m;

            // Lineas por temporada (por año de Fecha)
            vm.Lineas = stats
                .GroupBy(s => s.Fecha.Year)
                .OrderByDescending(g => g.Key)
                .Select(g => new LineaTemporadaVm
                {
                    Temporada = g.Key.ToString(),
                    G = g.Select(s => s.PartidoId).Distinct().Count(),
                    AB = g.Sum(s => s.AB),
                    R = g.Sum(s => s.R),
                    H = g.Sum(s => s.H),
                    Doubles = g.Sum(s => s.Doubles),
                    Triples = g.Sum(s => s.Triples),
                    HR = g.Sum(s => s.HR),
                    RBI = g.Sum(s => s.RBI),
                    BB = g.Sum(s => s.BB),
                    SO = g.Sum(s => s.SO),
                    AVG = (g.Sum(s => s.AB) > 0) ? (decimal)g.Sum(s => s.H) / g.Sum(s => s.AB) : 0m,
                    OBP = ((g.Sum(s => s.AB) + g.Sum(s => s.BB) + g.Sum(s => s.HBP) + g.Sum(s => s.SF)) > 0) ?
                        (decimal)(g.Sum(s => s.H) + g.Sum(s => s.BB) + g.Sum(s => s.HBP)) / (g.Sum(s => s.AB) + g.Sum(s => s.BB) + g.Sum(s => s.HBP) + g.Sum(s => s.SF)) : 0m,
                    SLG = (g.Sum(s => s.AB) > 0) ? (decimal)(g.Sum(s => s.H) + g.Sum(s => s.Doubles) + (2 * g.Sum(s => s.Triples)) + (3 * g.Sum(s => s.HR))) / g.Sum(s => s.AB) : 0m
                })
                .ToList();

            return vm;
        }

        public async Task<System.Collections.Generic.List<Scoreboard.Web.Modelos.ViewModels.LineaTemporadaVm>> ObtenerLineasTemporadaAsync(int jugadorId)
        {
            var stats = await _db.PlayerBattingStats
                .AsNoTracking()
                .Where(s => s.JugadorId == jugadorId)
                .ToListAsync();

            var lineas = stats
                .GroupBy(s => s.Fecha.Year)
                .OrderByDescending(g => g.Key)
                .Select(g => new Scoreboard.Web.Modelos.ViewModels.LineaTemporadaVm
                {
                    Temporada = g.Key.ToString(),
                    G = g.Select(s => s.PartidoId).Distinct().Count(),
                    AB = g.Sum(s => s.AB),
                    R = g.Sum(s => s.R),
                    H = g.Sum(s => s.H),
                    Doubles = g.Sum(s => s.Doubles),
                    Triples = g.Sum(s => s.Triples),
                    HR = g.Sum(s => s.HR),
                    RBI = g.Sum(s => s.RBI),
                    BB = g.Sum(s => s.BB),
                    SO = g.Sum(s => s.SO),
                    AVG = (g.Sum(s => s.AB) > 0) ? (decimal)g.Sum(s => s.H) / g.Sum(s => s.AB) : 0m,
                    OBP = ((g.Sum(s => s.AB) + g.Sum(s => s.BB) + g.Sum(s => s.HBP) + g.Sum(s => s.SF)) > 0) ?
                        (decimal)(g.Sum(s => s.H) + g.Sum(s => s.BB) + g.Sum(s => s.HBP)) / (g.Sum(s => s.AB) + g.Sum(s => s.BB) + g.Sum(s => s.HBP) + g.Sum(s => s.SF)) : 0m,
                    SLG = (g.Sum(s => s.AB) > 0) ? (decimal)(g.Sum(s => s.H) + g.Sum(s => s.Doubles) + (2 * g.Sum(s => s.Triples)) + (3 * g.Sum(s => s.HR))) / g.Sum(s => s.AB) : 0m
                })
                .ToList();

            return lineas;
        }

        public async Task<Scoreboard.Web.Modelos.ViewModels.EstadisticasEquipoVm?> ObtenerEstadisticasEquipoAsync(int equipoId)
        {
            var equipo = await _db.Equipos.FindAsync(equipoId);
            if (equipo == null) return null;

            var jugadores = await _db.Jugadores
                .AsNoTracking()
                .Where(j => j.EquipoId == equipoId)
                .ToListAsync();

            var jugadorIds = jugadores.Select(j => j.Id).ToList();

            var stats = await _db.PlayerBattingStats
                .AsNoTracking()
                .Where(s => jugadorIds.Contains(s.JugadorId))
                .ToListAsync();

            var partidos = await _db.Partidos
                .AsNoTracking()
                .Where(p => p.EquipoCasaId == equipoId || p.EquipoVisitaId == equipoId)
                .ToListAsync();

            var vm = new Scoreboard.Web.Modelos.ViewModels.EstadisticasEquipoVm
            {
                Equipo = equipo,
                PartidosJugados = partidos.Count,
                Jugadores = jugadores.Count,
                AB = stats.Sum(s => s.AB),
                H = stats.Sum(s => s.H),
                R = stats.Sum(s => s.R),
                Doubles = stats.Sum(s => s.Doubles),
                Triples = stats.Sum(s => s.Triples),
                HR = stats.Sum(s => s.HR),
                RBI = stats.Sum(s => s.RBI),
                BB = stats.Sum(s => s.BB),
                SO = stats.Sum(s => s.SO),
                HBP = stats.Sum(s => s.HBP),
                SF = stats.Sum(s => s.SF)
            };

            vm.AVG = (vm.AB > 0) ? (decimal)vm.H / vm.AB : 0m;
            var obpDen = vm.AB + vm.BB + vm.HBP + vm.SF;
            vm.OBP = (obpDen > 0) ? (decimal)(vm.H + vm.BB + vm.HBP) / obpDen : 0m;
            var totalBases = vm.H + vm.Doubles + (2 * vm.Triples) + (3 * vm.HR);
            vm.SLG = (vm.AB > 0) ? (decimal)totalBases / vm.AB : 0m;

            vm.Lineas = stats
                .GroupBy(s => s.Fecha.Year)
                .OrderByDescending(g => g.Key)
                .Select(g => new Scoreboard.Web.Modelos.ViewModels.LineaTemporadaVm
                {
                    Temporada = g.Key.ToString(),
                    G = g.Select(s => s.PartidoId).Distinct().Count(),
                    AB = g.Sum(s => s.AB),
                    R = g.Sum(s => s.R),
                    H = g.Sum(s => s.H),
                    Doubles = g.Sum(s => s.Doubles),
                    Triples = g.Sum(s => s.Triples),
                    HR = g.Sum(s => s.HR),
                    RBI = g.Sum(s => s.RBI),
                    BB = g.Sum(s => s.BB),
                    SO = g.Sum(s => s.SO),
                    AVG = (g.Sum(s => s.AB) > 0) ? (decimal)g.Sum(s => s.H) / g.Sum(s => s.AB) : 0m,
                    OBP = ((g.Sum(s => s.AB) + g.Sum(s => s.BB) + g.Sum(s => s.HBP) + g.Sum(s => s.SF)) > 0) ?
                        (decimal)(g.Sum(s => s.H) + g.Sum(s => s.BB) + g.Sum(s => s.HBP)) / (g.Sum(s => s.AB) + g.Sum(s => s.BB) + g.Sum(s => s.HBP) + g.Sum(s => s.SF)) : 0m,
                    SLG = (g.Sum(s => s.AB) > 0) ? (decimal)(g.Sum(s => s.H) + g.Sum(s => s.Doubles) + (2 * g.Sum(s => s.Triples)) + (3 * g.Sum(s => s.HR))) / g.Sum(s => s.AB) : 0m
                })
                .ToList();

            return vm;
        }
    }
}
