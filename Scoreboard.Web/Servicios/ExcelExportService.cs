using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Scoreboard.Web.Datos;
using Scoreboard.Web.Helpers;
using Scoreboard.Web.Modelos;

namespace Scoreboard.Web.Servicios
{
    public class ExcelExportService : IExcelExportService
    {
        private readonly ContextoMarcador _db;

        public ExcelExportService(ContextoMarcador db)
        {
            _db = db;
        }

        public async Task<(byte[] contenido, string fileName)> BuildPlayByPlayXlsxAsync(int partidoId)
        {
            var partido = await _db.Partidos
                .Include(p => p.EquipoCasa)
                .Include(p => p.EquipoVisita)
                .FirstOrDefaultAsync(p => p.Id == partidoId);

            if (partido == null)
                throw new KeyNotFoundException($"Partido {partidoId} no encontrado");

            var logs = await _db.PlayLogs
                .AsNoTracking()
                .Where(l => l.PartidoId == partidoId && l.IsActive)
                .Include(l => l.Jugador)
                .OrderBy(l => l.CreadoUtc)
                .ToListAsync();

            var nombreVisita = partido.EquipoVisita?.Nombre ?? "Visita";
            var nombreCasa = partido.EquipoCasa?.Nombre ?? "Casa";

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add($"Play_{partidoId}");

            // Estilo global
            ws.Style.Font.FontName = "Calibri";
            ws.Style.Font.FontSize = 13;
            ws.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            var headers = new[] { "Entrada", "Mitad", "Bateador", "Resultado", "Bases", "Outs", "Carreras", "Tiempo" };
            int colCount = headers.Length;

            // Offset visual (dos filas y dos columnas en blanco)
            int rowOffset = 2; // filas vacías al inicio
            int colOffset = 2; // columnas vacías al inicio
            int firstCol = 1 + colOffset;
            int lastCol = firstCol + colCount - 1;

            // Título y subtítulos
            int titleRow = rowOffset + 1;
            ws.Cell(titleRow, firstCol).Value = "Softball-Scorer — Play-by-Play";
            ws.Range(titleRow, firstCol, titleRow, lastCol).Merge();
            ws.Cell(titleRow, firstCol).Style.Font.Bold = true;

            int subtitleTeamsRow = rowOffset + 2;
            ws.Cell(subtitleTeamsRow, firstCol).Value = $"{nombreVisita} vs {nombreCasa}";
            ws.Range(subtitleTeamsRow, firstCol, subtitleTeamsRow, lastCol).Merge();
            ws.Cell(subtitleTeamsRow, firstCol).Style.Font.FontColor = XLColor.FromHtml("#555555");

            int subtitleDateRow = rowOffset + 3;
            ws.Cell(subtitleDateRow, firstCol).Value = partido.Fecha.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
            ws.Range(subtitleDateRow, firstCol, subtitleDateRow, lastCol).Merge();
            ws.Cell(subtitleDateRow, firstCol).Style.Font.FontColor = XLColor.FromHtml("#777777");

            // Encabezados
            int headerRow = rowOffset + 5;
            for (int c = 0; c < colCount; c++)
            {
                var cell = ws.Cell(headerRow, firstCol + c);
                cell.Value = headers[c];
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#EAEAEA");
                cell.Style.Font.Bold = true;
            }

            // Datos
            int row = headerRow + 1;
            foreach (var log in logs)
            {
                var (before, after) = PlayLogSnapshotHelper.ExtractSnapshots(log.SnapshotJson);
                int entrada = before?.EntradaActual ?? after?.EntradaActual ?? 0;
                string mitad = before?.Mitad ?? after?.Mitad ?? string.Empty;
                string bateador = log.Jugador == null ? string.Empty : ($"{log.Jugador.Nombre} {log.Jugador.Apellido}").Trim();
                string resultado = log.Resultado?.ToString() ?? string.Empty;
                string bases = BasesText(after);
                int outs = after?.Outs ?? 0;
                int carreras = log.RunsScored;
                var tiempo = log.CreadoUtc.ToLocalTime();

                ws.Cell(row, firstCol + 0).Value = entrada;
                ws.Cell(row, firstCol + 1).Value = mitad;
                ws.Cell(row, firstCol + 2).Value = bateador;
                ws.Cell(row, firstCol + 3).Value = resultado;
                ws.Cell(row, firstCol + 4).Value = bases;
                ws.Cell(row, firstCol + 5).Value = outs;
                ws.Cell(row, firstCol + 6).Value = carreras;
                ws.Cell(row, firstCol + 7).Value = tiempo;
                ws.Cell(row, firstCol + 7).Style.DateFormat.Format = "yyyy-mm-dd hh:mm:ss";

                row++;
            }

            int dataLastRow = row - 1;
            if (dataLastRow >= headerRow + 1)
            {
                // Formato de tabla y bordes
                var tableRange = ws.Range(headerRow, firstCol, dataLastRow, lastCol);
                tableRange.CreateTable();
                tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
                tableRange.Style.Border.OutsideBorderColor = XLColor.Black;
                tableRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            }

            // Ajustes finales: autofit con ancho mínimo
            ws.Columns(firstCol, lastCol).AdjustToContents();
            for (int c = firstCol; c <= lastCol; c++)
            {
                if (ws.Column(c).Width < 15)
                    ws.Column(c).Width = 15;
            }

            // Altura mínima de filas 22 para las filas utilizadas
            int lastUsedRow = Math.Max(dataLastRow, headerRow);
            for (int r = 1; r <= lastUsedRow; r++)
            {
                if (ws.Row(r).Height < 22)
                    ws.Row(r).Height = 22;
            }

            // Congelar hasta el header
            ws.SheetView.FreezeRows(headerRow);

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            var fileName = $"SoftballScorer_PlayByPlay_{partidoId}.xlsx";
            return (ms.ToArray(), fileName);
        }

        private static string BasesText(SnapshotPartido? snap)
        {
            if (snap == null)
                return string.Empty;

            var parts = new List<string>();
            if (snap.B1) parts.Add("1B");
            if (snap.B2) parts.Add("2B");
            if (snap.B3) parts.Add("3B");

            return parts.Count == 0 ? "-" : string.Join(", ", parts);
        }
    }
}
