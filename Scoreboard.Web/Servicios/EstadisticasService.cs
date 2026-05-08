using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Scoreboard.Web.Datos;
using Scoreboard.Web.Modelos;
using Scoreboard.Web.Modelos.ViewModels;

namespace Scoreboard.Web.Servicios
{
    /// <summary>
    /// Servicio que calcula estadisticas de jugadores y equipos.
    ///
    /// Se conecta con:
    /// - ContextoMarcador: para leer jugadores, partidos y PlayerBattingStats.
    /// - ViewModels de estadisticas: para devolver datos listos para pantallas.
    ///
    /// Flujo simple:
    /// 1. Busca datos crudos en la base.
    /// 2. Suma apariciones, hits, jonrones y otros indicadores.
    /// 3. Devuelve un ViewModel para mostrar en la vista.
    ///
    /// Cuidado:
    /// Las estadisticas dependen de lo que guarda MarcadorService en cada jugada.
    /// </summary>
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
            var stats = await _db.PlayerBattingStats
                .AsNoTracking()
                .Where(s => s.JugadorId == jugadorId)
                .ToListAsync();

            var partidosEquipo = await _db.Partidos
                .AsNoTracking()
                .Where(p => p.EquipoCasaId == jugador.EquipoId || p.EquipoVisitaId == jugador.EquipoId)
                .CountAsync();

            var totales = BuildTotals(stats);

            var vm = new EstadisticasJugadorVm
            {
                Jugador = jugador,
                PartidosJugados = totales.Partidos > 0 ? totales.Partidos : partidosEquipo,
                AB = totales.AB,
                H = totales.H,
                R = totales.R,
                Doubles = totales.Doubles,
                Triples = totales.Triples,
                HR = totales.HR,
                RBI = totales.RBI,
                BB = totales.BB,
                SO = totales.SO,
                HBP = totales.HBP,
                SF = totales.SF,
                SH = totales.SH,
                PA = totales.PA,
                AVG = totales.AVG,
                OBP = totales.OBP,
                SLG = totales.SLG,
                OPS = totales.OPS
            };

            vm.Lineas = BuildSeasonLines(stats);

            return vm;
        }

        public async Task<System.Collections.Generic.List<Scoreboard.Web.Modelos.ViewModels.LineaTemporadaVm>> ObtenerLineasTemporadaAsync(int jugadorId)
        {
            var stats = await _db.PlayerBattingStats
                .AsNoTracking()
                .Where(s => s.JugadorId == jugadorId)
                .ToListAsync();

            return BuildSeasonLines(stats);
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

            var totales = BuildTotals(stats);
            var jugadoresPorId = jugadores.ToDictionary(j => j.Id);
            var jugadoresDetalle = stats
                .GroupBy(s => s.JugadorId)
                .Select(g =>
                {
                    var totalsJugador = BuildTotals(g);
                    jugadoresPorId.TryGetValue(g.Key, out var jugadorInfo);
                    return new JugadorLineaResumenVm
                    {
                        JugadorId = g.Key,
                        Nombre = jugadorInfo != null ? $"{jugadorInfo.Nombre} {jugadorInfo.Apellido}" : $"Jugador #{g.Key}",
                        Numero = jugadorInfo?.NumeroUniforme ?? 0,
                        AB = totalsJugador.AB,
                        H = totalsJugador.H,
                        HR = totalsJugador.HR,
                        RBI = totalsJugador.RBI,
                        BB = totalsJugador.BB,
                        SO = totalsJugador.SO,
                        PA = totalsJugador.PA,
                        AVG = totalsJugador.AVG,
                        OBP = totalsJugador.OBP,
                        SLG = totalsJugador.SLG,
                        OPS = totalsJugador.OPS
                    };
                })
                .OrderByDescending(j => j.OPS)
                .ThenByDescending(j => j.AVG)
                .ThenBy(j => j.Nombre)
                .ToList();

            var vm = new Scoreboard.Web.Modelos.ViewModels.EstadisticasEquipoVm
            {
                Equipo = equipo,
                PartidosJugados = Math.Max(partidos.Count, totales.Partidos),
                Jugadores = jugadores.Count,
                AB = totales.AB,
                H = totales.H,
                R = totales.R,
                Doubles = totales.Doubles,
                Triples = totales.Triples,
                HR = totales.HR,
                RBI = totales.RBI,
                BB = totales.BB,
                SO = totales.SO,
                HBP = totales.HBP,
                SF = totales.SF,
                SH = totales.SH,
                PA = totales.PA,
                AVG = totales.AVG,
                OBP = totales.OBP,
                SLG = totales.SLG,
                OPS = totales.OPS,
                Lineas = BuildSeasonLines(stats),
                JugadoresDetalle = jugadoresDetalle
            };

            return vm;
        }

        private static BattingTotals BuildTotals(IEnumerable<PlayerBattingStat> source)
        {
            var list = source as IList<PlayerBattingStat> ?? source.ToList();
            var totals = new BattingTotals
            {
                Partidos = list.Sum(s => s.PartidosJugados),
                AB = list.Sum(s => s.AB),
                H = list.Sum(s => s.H),
                Doubles = list.Sum(s => s.Doubles),
                Triples = list.Sum(s => s.Triples),
                HR = list.Sum(s => s.HR),
                RBI = list.Sum(s => s.RBI),
                R = list.Sum(s => s.R),
                BB = list.Sum(s => s.BB),
                SO = list.Sum(s => s.SO),
                HBP = list.Sum(s => s.HBP),
                SF = list.Sum(s => s.SF),
                SH = list.Sum(s => s.SH),
                PA = list.Sum(s => s.PA)
            };
            totals.ComputeRates();
            return totals;
        }

        private static List<LineaTemporadaVm> BuildSeasonLines(IEnumerable<PlayerBattingStat> stats)
        {
            return stats
                .GroupBy(s => s.Temporada != 0 ? s.Temporada : s.Fecha.Year)
                .OrderByDescending(g => g.Key)
                .Select(g =>
                {
                    var totals = BuildTotals(g);
                    return new LineaTemporadaVm
                    {
                        Temporada = g.Key.ToString(),
                        G = g.Select(s => s.PartidoId).Distinct().Count(),
                        AB = totals.AB,
                        R = totals.R,
                        H = totals.H,
                        Doubles = totals.Doubles,
                        Triples = totals.Triples,
                        HR = totals.HR,
                        RBI = totals.RBI,
                        BB = totals.BB,
                        SO = totals.SO,
                        HBP = totals.HBP,
                        SF = totals.SF,
                        SH = totals.SH,
                        PA = totals.PA,
                        AVG = totals.AVG,
                        OBP = totals.OBP,
                        SLG = totals.SLG,
                        OPS = totals.OPS
                    };
                })
                .ToList();
        }

        private static decimal SafeDivide(int numerator, int denominator)
            => denominator > 0 ? Math.Round((decimal)numerator / denominator, 3, MidpointRounding.AwayFromZero) : 0m;

        private sealed class BattingTotals
        {
            public int Partidos { get; set; }
            public int AB { get; set; }
            public int H { get; set; }
            public int Doubles { get; set; }
            public int Triples { get; set; }
            public int HR { get; set; }
            public int RBI { get; set; }
            public int R { get; set; }
            public int BB { get; set; }
            public int SO { get; set; }
            public int HBP { get; set; }
            public int SF { get; set; }
            public int SH { get; set; }
            public int PA { get; set; }
            public decimal AVG { get; private set; }
            public decimal OBP { get; private set; }
            public decimal SLG { get; private set; }
            public decimal OPS { get; private set; }

            public void ComputeRates()
            {
                AVG = SafeDivide(H, AB);
                var obpDen = AB + BB + HBP + SF;
                OBP = SafeDivide(H + BB + HBP, obpDen);
                var singles = H - Doubles - Triples - HR;
                if (singles < 0) singles = 0;
                var totalBases = singles + (2 * Doubles) + (3 * Triples) + (4 * HR);
                SLG = SafeDivide(totalBases, AB);
                OPS = Math.Round(OBP + SLG, 3, MidpointRounding.AwayFromZero);
            }
        }
    }
}
