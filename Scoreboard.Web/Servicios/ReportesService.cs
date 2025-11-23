using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Scoreboard.Web.Datos;
using Scoreboard.Web.Helpers;
using Scoreboard.Web.Modelos;

namespace Scoreboard.Web.Servicios
{
    public class ReportesService : IReportesService
    {
        private readonly ContextoMarcador _db;

        public ReportesService(ContextoMarcador db)
        {
            _db = db;
        }

        public async Task<byte[]> GenerarBoxScoreCsvAsync(int partidoId)
        {
            var partido = await _db.Partidos
                .Include(p => p.EquipoCasa)
                .Include(p => p.EquipoVisita)
                .Include(p => p.Entradas)
                .FirstOrDefaultAsync(p => p.Id == partidoId)
                ?? throw new KeyNotFoundException("Partido no encontrado");

            var stats = await _db.PlayerBattingStats
                .Include(s => s.Jugador)
                .Where(s => s.PartidoId == partidoId)
                .ToListAsync();

            var lineups = await _db.Lineups.AsNoTracking()
                .Where(l => l.PartidoId == partidoId)
                .Include(l => l.Jugador)
                .ToListAsync();

            var jugadoresPorEquipo = stats
                .GroupBy(s => s.JugadorId)
                .Select(g => BuildJugadorLinea(g.Key, g.ToList()))
                .ToList();

            var totalesVisita = BuildTotalesEquipo(jugadoresPorEquipo, partido.EquipoVisitaId);
            var totalesCasa = BuildTotalesEquipo(jugadoresPorEquipo, partido.EquipoCasaId);

            var ordenVisita = GetOrden(lineups, partido.EquipoVisitaId);
            var ordenCasa = GetOrden(lineups, partido.EquipoCasaId);

            var listaVisita = jugadoresPorEquipo
                .Where(j => j.EquipoId == partido.EquipoVisitaId)
                .OrderBy(j => ordenVisita.TryGetValue(j.JugadorId, out var idx) ? idx : int.MaxValue)
                .ThenBy(j => j.Nombre)
                .ToList();

            var listaCasa = jugadoresPorEquipo
                .Where(j => j.EquipoId == partido.EquipoCasaId)
                .OrderBy(j => ordenCasa.TryGetValue(j.JugadorId, out var idx) ? idx : int.MaxValue)
                .ThenBy(j => j.Nombre)
                .ToList();

            var maxInnings = Math.Max(9, Math.Max(partido.EntradaActual, partido.Entradas?.Select(e => e.NumeroInning).DefaultIfEmpty(0).Max() ?? 0));

            var sb = new StringBuilder();
            sb.AppendLine($"Equipos,{partido.EquipoVisita?.Nombre} vs {partido.EquipoCasa?.Nombre}");
            sb.AppendLine($"Fecha,{partido.Fecha:yyyy-MM-dd}");
            sb.AppendLine("Arbitro,");
            sb.AppendLine($"Resultado,{partido.EquipoVisita?.Nombre} {partido.CarrerasVisita} - {partido.CarrerasCasa} {partido.EquipoCasa?.Nombre}");
            sb.AppendLine();

            sb.Append("Equipo");
            for (int i = 1; i <= maxInnings; i++)
                sb.Append($",{i}");
            sb.Append(",C,H,E");
            sb.AppendLine();

            sb.AppendLine(BuildLineaEntradas(partido.EquipoVisita?.Nombre ?? "Visita", partido.Entradas, maxInnings, true, partido));
            sb.AppendLine(BuildLineaEntradas(partido.EquipoCasa?.Nombre ?? "Casa", partido.Entradas, maxInnings, false, partido));

            sb.AppendLine();

            sb.AppendLine(partido.EquipoVisita?.Nombre ?? "Visita");
            AppendBoxHeader(sb);
            foreach (var jugador in listaVisita)
                AppendJugadorLinea(sb, jugador);
            AppendTotalesLinea(sb, totalesVisita, "Totales");

            sb.AppendLine();

            sb.AppendLine(partido.EquipoCasa?.Nombre ?? "Casa");
            AppendBoxHeader(sb);
            foreach (var jugador in listaCasa)
                AppendJugadorLinea(sb, jugador);
            AppendTotalesLinea(sb, totalesCasa, "Totales");

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        public async Task<byte[]> GenerarPlayByPlayCsvAsync(int partidoId)
        {
            _ = await _db.Partidos.AsNoTracking().FirstOrDefaultAsync(p => p.Id == partidoId)
                ?? throw new KeyNotFoundException("Partido no encontrado");

            var logs = await _db.PlayLogs
                .Include(pl => pl.Jugador)
                .Where(pl => pl.PartidoId == partidoId && pl.IsActive)
                .OrderBy(pl => pl.Id)
                .ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("Entrada,Mitad,Bateador,Resultado,Nuevas bases,Outs,Carreras,Tiempo");
            foreach (var log in logs)
            {
                var (before, after) = PlayLogSnapshotHelper.ExtractSnapshots(log.SnapshotJson);
                var entrada = before?.EntradaActual ?? after?.EntradaActual ?? 0;
                var mitad = before?.Mitad ?? after?.Mitad ?? string.Empty;
                var bases = after == null ? string.Empty : FormatBases(after);
                var outs = after?.Outs ?? 0;
                var bateador = log.Jugador == null ? string.Empty : $"{log.Jugador.Nombre} {log.Jugador.Apellido}".Trim();
                var resultado = log.Resultado?.ToString() ?? string.Empty;
                var tiempo = log.CreadoUtc.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                sb.AppendLine($"{entrada},{mitad},{bateador},{resultado},{bases},{outs},{log.RunsScored},{tiempo}");
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        private sealed class JugadorLinea
        {
            public int JugadorId { get; init; }
            public int EquipoId { get; init; }
            public string Nombre { get; init; } = string.Empty;
            public string Posicion { get; init; } = string.Empty;
            public int AB { get; init; }
            public int H { get; init; }
            public int R { get; init; }
            public int RBI { get; init; }
            public int BB { get; init; }
            public int SO { get; init; }
            public int HBP { get; init; }
            public int SF { get; init; }
            public int Doubles { get; init; }
            public int Triples { get; init; }
            public int HR { get; init; }
            public decimal OBP { get; init; }
            public decimal SLG { get; init; }
            public decimal AVG { get; init; }
        }

        private sealed class TotalesEquipo
        {
            public int AB { get; set; }
            public int H { get; set; }
            public int R { get; set; }
            public int RBI { get; set; }
            public int BB { get; set; }
            public int SO { get; set; }
            public int HBP { get; set; }
            public int SF { get; set; }
            public int Doubles { get; set; }
            public int Triples { get; set; }
            public int HR { get; set; }
            public decimal OBP { get; set; }
            public decimal SLG { get; set; }
            public decimal AVG { get; set; }
        }

        private static JugadorLinea BuildJugadorLinea(int jugadorId, List<PlayerBattingStat> stats)
        {
            var jugador = stats.First().Jugador;
            var nombre = jugador == null ? "" : $"{jugador.Nombre} {jugador.Apellido}".Trim();
            var ab = stats.Sum(s => s.AB);
            var h = stats.Sum(s => s.H);
            var r = stats.Sum(s => s.R);
            var rbi = stats.Sum(s => s.RBI);
            var bb = stats.Sum(s => s.BB);
            var so = stats.Sum(s => s.SO);
            var hbp = stats.Sum(s => s.HBP);
            var sf = stats.Sum(s => s.SF);
            var doubles = stats.Sum(s => s.Doubles);
            var triples = stats.Sum(s => s.Triples);
            var hr = stats.Sum(s => s.HR);

            var obp = SafeDiv(h + bb + hbp, ab + bb + hbp + sf);
            var totalBases = (h - doubles - triples - hr) + (2 * doubles) + (3 * triples) + (4 * hr);
            var slg = SafeDiv(totalBases, ab);
            var avg = SafeDiv(h, ab);

            return new JugadorLinea
            {
                JugadorId = jugadorId,
                EquipoId = stats.First().EquipoId,
                Nombre = nombre,
                Posicion = jugador?.Posicion.ToString() ?? string.Empty,
                AB = ab,
                H = h,
                R = r,
                RBI = rbi,
                BB = bb,
                SO = so,
                HBP = hbp,
                SF = sf,
                Doubles = doubles,
                Triples = triples,
                HR = hr,
                OBP = obp,
                SLG = slg,
                AVG = avg
            };
        }

        private static TotalesEquipo BuildTotalesEquipo(IEnumerable<JugadorLinea> jugadores, int equipoId)
        {
            var subset = jugadores.Where(j => j.EquipoId == equipoId).ToList();
            if (!subset.Any()) return new TotalesEquipo();

            var totals = new TotalesEquipo
            {
                AB = subset.Sum(j => j.AB),
                H = subset.Sum(j => j.H),
                R = subset.Sum(j => j.R),
                RBI = subset.Sum(j => j.RBI),
                BB = subset.Sum(j => j.BB),
                SO = subset.Sum(j => j.SO),
                HBP = subset.Sum(j => j.HBP),
                SF = subset.Sum(j => j.SF),
                Doubles = subset.Sum(j => j.Doubles),
                Triples = subset.Sum(j => j.Triples),
                HR = subset.Sum(j => j.HR)
            };

            totals.OBP = SafeDiv(totals.H + totals.BB + totals.HBP, totals.AB + totals.BB + totals.HBP + totals.SF);
            var totalBases = (totals.H - totals.Doubles - totals.Triples - totals.HR) + (2 * totals.Doubles) + (3 * totals.Triples) + (4 * totals.HR);
            totals.SLG = SafeDiv(totalBases, totals.AB);
            totals.AVG = SafeDiv(totals.H, totals.AB);
            return totals;
        }

        private static Dictionary<int, int> GetOrden(IEnumerable<LineupItem> lineups, int equipoId)
        {
            return lineups
                .Where(l => l.EquipoId == equipoId)
                .OrderBy(l => l.Orden)
                .Select((item, idx) => new { item.JugadorId, idx })
                .ToDictionary(x => x.JugadorId, x => x.idx);
        }

        private static string BuildLineaEntradas(string equipoNombre, IEnumerable<Entrada>? entradas, int maxInnings, bool esVisita, Partido partido)
        {
            var sb = new StringBuilder();
            sb.Append(equipoNombre);
            for (int inning = 1; inning <= maxInnings; inning++)
            {
                var entrada = entradas?.FirstOrDefault(e => e.NumeroInning == inning);
                var runs = esVisita ? entrada?.CarrerasVisita ?? 0 : entrada?.CarrerasCasa ?? 0;
                sb.Append($",{runs}");
            }
            var carreras = esVisita ? partido.CarrerasVisita : partido.CarrerasCasa;
            var hits = esVisita ? partido.HitsVisita : partido.HitsCasa;
            var errores = esVisita ? partido.ErroresVisita : partido.ErroresCasa;
            sb.Append($",{carreras},{hits},{errores}");
            return sb.ToString();
        }

        private static void AppendBoxHeader(StringBuilder sb)
        {
            sb.AppendLine("Jugador,Posicion,AB,H,R,RBI,BB,SO,OBP,SLG,AVG");
        }

        private static void AppendJugadorLinea(StringBuilder sb, JugadorLinea linea)
        {
            sb.Append(linea.Nombre).Append(',')
              .Append(linea.Posicion).Append(',')
              .Append(linea.AB).Append(',')
              .Append(linea.H).Append(',')
              .Append(linea.R).Append(',')
              .Append(linea.RBI).Append(',')
              .Append(linea.BB).Append(',')
              .Append(linea.SO).Append(',')
              .Append(linea.OBP.ToString("0.000", CultureInfo.InvariantCulture)).Append(',')
              .Append(linea.SLG.ToString("0.000", CultureInfo.InvariantCulture)).Append(',')
              .Append(linea.AVG.ToString("0.000", CultureInfo.InvariantCulture))
              .AppendLine();
        }

        private static void AppendTotalesLinea(StringBuilder sb, TotalesEquipo totales, string etiqueta)
        {
            sb.Append(etiqueta).Append(',')
              .Append('-').Append(',')
              .Append(totales.AB).Append(',')
              .Append(totales.H).Append(',')
              .Append(totales.R).Append(',')
              .Append(totales.RBI).Append(',')
              .Append(totales.BB).Append(',')
              .Append(totales.SO).Append(',')
              .Append(totales.OBP.ToString("0.000", CultureInfo.InvariantCulture)).Append(',')
              .Append(totales.SLG.ToString("0.000", CultureInfo.InvariantCulture)).Append(',')
              .Append(totales.AVG.ToString("0.000", CultureInfo.InvariantCulture))
              .AppendLine();
        }

        private static decimal SafeDiv(int numerator, int denominator)
        {
            if (denominator == 0) return 0m;
            return Math.Round((decimal)numerator / denominator, 3, MidpointRounding.AwayFromZero);
        }

        private static string FormatBases(SnapshotPartido snapshot)
        {
            var b1 = snapshot.B1 ? "1" : "0";
            var b2 = snapshot.B2 ? "1" : "0";
            var b3 = snapshot.B3 ? "1" : "0";
            return $"B1:{b1}-B2:{b2}-B3:{b3}";
        }
    }
}
