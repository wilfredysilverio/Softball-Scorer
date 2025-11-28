using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scoreboard.Web.Datos;
using Scoreboard.Web.Servicios;
using Scoreboard.Web.Helpers;
using System.Globalization;

namespace Scoreboard.Web.Controllers
{
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class ExportacionesController : Controller
    {
        private readonly ContextoMarcador _db;
        private readonly IReportesService _reportes;
        private readonly IExcelExportService _excelExport;

        public ExportacionesController(ContextoMarcador db, IReportesService reportes, IExcelExportService excelExport)
        {
            _db = db;
            _reportes = reportes;
            _excelExport = excelExport;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var partidos = await _db.Partidos
                .Include(p => p.EquipoCasa)
                .Include(p => p.EquipoVisita)
                .OrderByDescending(p => p.Fecha)
                .ToListAsync();

            return View(partidos);
        }

        // Descargar Play-by-Play como Excel (XLSX) con estilo usando servicio
        [HttpGet]
        public async Task<IActionResult> ExportPlayXlsx(int id)
        {
            var (contenido, fileName) = await _excelExport.BuildPlayByPlayXlsxAsync(id);
            const string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            return File(contenido, contentType, fileName);
        }

        // Descargar Play-by-Play como CSV "Excel-friendly"
        [HttpGet]
        public async Task<IActionResult> ExportPlayCsv(int id)
        {
            var partido = await _db.Partidos
                .Include(p => p.EquipoCasa)
                .Include(p => p.EquipoVisita)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (partido == null) return NotFound();

            var logs = await _db.PlayLogs
                .Include(pl => pl.Jugador)
                .Where(pl => pl.PartidoId == id && pl.IsActive)
                .OrderBy(pl => pl.Id)
                .ToListAsync();

            string CsvEscape(string? s)
            {
                if (string.IsNullOrEmpty(s)) return "";
                var needsQuotes = s.Contains(',') || s.Contains('"') || s.Contains('\n') || s.Contains('\r');
                s = s.Replace("\"", "\"\"");
                return needsQuotes ? $"\"{s}\"" : s;
            }

            string BasesText(Scoreboard.Web.Modelos.SnapshotPartido? snap)
            {
                if (snap == null) return "";
                var b1 = snap.B1 ? "1" : "0";
                var b2 = snap.B2 ? "1" : "0";
                var b3 = snap.B3 ? "1" : "0";
                return $"B1:{b1}-B2:{b2}-B3:{b3}";
            }

            var sb = new System.Text.StringBuilder();
            // UTF-8 BOM para Excel
            sb.Append('\uFEFF');

            // Línea de comentario opcional
            var nombreVisita = partido.EquipoVisita?.Nombre ?? "Visita";
            var nombreCasa = partido.EquipoCasa?.Nombre ?? "Casa";
            sb.AppendLine($"# Softball-Scorer - Play-by-Play del partido {nombreVisita} vs {nombreCasa} ({partido.Fecha:yyyy-MM-dd HH:mm})");

            // Cabecera
            sb.AppendLine("Entrada,Mitad,Bateador,Resultado,Bases,Outs,Carreras,Tiempo");

            for (int i = 0; i < logs.Count; i++)
            {
                var log = logs[i];
                var (before, after) = PlayLogSnapshotHelper.ExtractSnapshots(log.SnapshotJson);

                var entrada = (before?.EntradaActual ?? after?.EntradaActual ?? 0).ToString();
                var mitad = (before?.Mitad ?? after?.Mitad ?? "");
                var bateador = log.Jugador == null ? "" : ($"{log.Jugador.Nombre} {log.Jugador.Apellido}").Trim();
                var resultado = log.Resultado?.ToString() ?? "";
                var bases = BasesText(after);
                var outs = (after?.Outs ?? 0).ToString();
                var carreras = log.RunsScored.ToString();
                var tiempo = log.CreadoUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");

                sb.AppendLine(string.Join(',', new[]
                {
                    CsvEscape(entrada),
                    CsvEscape(mitad),
                    CsvEscape(bateador),
                    CsvEscape(resultado),
                    CsvEscape(bases),
                    CsvEscape(outs),
                    CsvEscape(carreras),
                    CsvEscape(tiempo)
                }));
            }

            var nombreArchivo = $"SoftballScorer_PlayByPlay_{id}_{DateTime.Now.ToString("yyyyMMdd_HHmm", CultureInfo.InvariantCulture)}.csv";
            var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv; charset=utf-8", nombreArchivo);
        }

        // Vista imprimible (puedes Guardar como PDF desde el navegador)
        [HttpGet]
        public async Task<IActionResult> PrintPlay(int id)
        {
            var partido = await _db.Partidos
                .Include(p => p.EquipoCasa)
                .Include(p => p.EquipoVisita)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (partido == null) return NotFound();

            var logs = await _db.PlayLogs
                .Include(pl => pl.Jugador)
                .Where(pl => pl.PartidoId == id && pl.IsActive)
                .OrderBy(pl => pl.Id)
                .ToListAsync();

            var vm = new PrintPlayVm
            {
                Partido = partido,
                Logs = logs
            };

            return View(vm);
        }
    }

    // ViewModel sencillo para la vista imprimible
    public class PrintPlayVm
    {
        public Scoreboard.Web.Modelos.Partido? Partido { get; set; }
        public List<Scoreboard.Web.Modelos.PlayLog> Logs { get; set; } = new();
    }
}
