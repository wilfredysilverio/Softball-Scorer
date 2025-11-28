using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Text;
using Scoreboard.Web.Datos;
using Scoreboard.Web.Modelos;
using Scoreboard.Web.Servicios.Marcador;
using Scoreboard.Web.Servicios;
using Scoreboard.Web.Modelos.ViewModels;
using Scoreboard.Web.Helpers;

namespace Scoreboard.Web.Controllers
{
    public class PartidosController : Controller
    {
        private readonly ContextoMarcador _context;
        private readonly IMarcadorService _marcador;
        private readonly IReportesService _reportes;
        public PartidosController(ContextoMarcador context, IMarcadorService marcador, IReportesService reportes)
        {
            _context = context;
            _marcador = marcador;
            _reportes = reportes;
        }

        private void CargarCombos(int? casaId = null, int? visitaId = null)
        {
            var equipos = _context.Equipos.AsNoTracking().OrderBy(e => e.Nombre).ToList();
            ViewBag.EquiposCasa = new SelectList(equipos, "Id", "Nombre", casaId);
            ViewBag.EquiposVisita = new SelectList(equipos, "Id", "Nombre", visitaId);
        }

        private bool EsAjaxRequest()
        {
            if (Request?.Headers == null) return false;
            if (Request.Headers.TryGetValue("X-Requested-With", out var xrw) &&
                xrw.Any(v => string.Equals(v, "XMLHttpRequest", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
            var accept = Request.Headers["Accept"].FirstOrDefault();
            return accept != null && accept.Contains("application/json", StringComparison.OrdinalIgnoreCase);
        }

        private IActionResult ErrorRegistro(string mensaje, bool esAjax, int partidoId)
        {
            if (esAjax)
            {
                return BadRequest(new { ok = false, message = mensaje });
            }
            TempData["Error"] = mensaje;
            return RedirectToAction(nameof(VerPartido), new { id = partidoId });
        }

        private async Task<(List<Jugador> Lineup, Jugador? Bateador)> ObtenerContextoBateadorAsync(Partido partido)
        {
            var equipoBateaId = partido.Mitad == MitadEntrada.Baja ? partido.EquipoCasaId : partido.EquipoVisitaId;
            var lineupItems = await _context.Lineups.AsNoTracking()
                .Where(l => l.PartidoId == partido.Id && l.EquipoId == equipoBateaId)
                .OrderBy(l => l.Orden)
                .Include(l => l.Jugador)
                .ToListAsync();

            var jugadores = lineupItems
                .Where(l => l.Jugador != null)
                .Select(l => l.Jugador!)
                .ToList();

            Jugador? bateadorEsperado = null;
            if (jugadores.Any())
            {
                var idx = partido.Mitad == MitadEntrada.Baja ? (partido.IndexBateadorCasa ?? 0) : (partido.IndexBateadorVisita ?? 0);
                idx = Math.Clamp(idx, 0, jugadores.Count - 1);
                bateadorEsperado = jugadores[idx];
            }

            return (jugadores, bateadorEsperado);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var partidos = await _context.Partidos
                .Include(p => p.EquipoCasa)
                .Include(p => p.EquipoVisita)
                .OrderByDescending(p => p.Fecha)
                .ToListAsync();

            return View(partidos);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            CargarCombos();
            return View(new Partido { Fecha = DateTime.Now });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(Partido modelo)
        {
            if (modelo.EquipoCasaId == modelo.EquipoVisitaId)
                ModelState.AddModelError(nameof(Partido.EquipoVisitaId), "El equipo visitante debe ser diferente al equipo de casa.");
            if (!ModelState.IsValid)
            {
                CargarCombos(modelo.EquipoCasaId, modelo.EquipoVisitaId);
                return View(modelo);
            }
            _context.Partidos.Add(modelo);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id is null) return NotFound();
            var partido = await _context.Partidos.AsNoTracking()
                .Include(p => p.EquipoCasa)
                .Include(p => p.EquipoVisita)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (partido is null) return NotFound();
            return View(partido);
        }

        [AllowAnonymous]
        public async Task<IActionResult> MarcadorPublico(int id)
        {
            var partido = await _context.Partidos.AsNoTracking()
                .Include(p => p.EquipoCasa)
                .Include(p => p.EquipoVisita)
                .Include(p => p.Entradas)
                .Include(p => p.PlayerBattingStats)
                    .ThenInclude(s => s.Jugador)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (partido == null) return NotFound();

            var entradas = partido.Entradas?
                .OrderBy(e => e.NumeroInning)
                .ToList() ?? new List<Entrada>();

            var jugadas = await _context.PlayLogs.AsNoTracking()
                .Include(pl => pl.Jugador)
                .Where(pl => pl.PartidoId == id && pl.IsActive)
                .OrderByDescending(pl => pl.Id)
                .Take(5)
                .ToListAsync();

            foreach (var log in jugadas)
            {
                var (before, after) = PlayLogSnapshotHelper.ExtractSnapshots(log.SnapshotJson);
                log.EntradaContext = before?.EntradaActual ?? after?.EntradaActual;
                log.MitadContext = before?.Mitad ?? after?.Mitad;
                log.OutsAfter = after?.Outs;
                if (after != null)
                {
                    var b1 = after.B1 ? "1" : "0";
                    var b2 = after.B2 ? "1" : "0";
                    var b3 = after.B3 ? "1" : "0";
                    log.BasesAfter = $"B1:{b1} B2:{b2} B3:{b3}";
                }
            }

            jugadas = jugadas
                .OrderByDescending(pl => pl.Id)
                .ToList();

            var vm = new MarcadorPublicoVm
            {
                Partido = partido,
                Entradas = entradas,
                UltimasJugadas = jugadas
            };

            return View(vm);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id is null) return NotFound();
            var partido = await _context.Partidos.FindAsync(id);
            if (partido is null) return NotFound();
            CargarCombos(partido.EquipoCasaId, partido.EquipoVisitaId);
            return View(partido);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, Partido modelo)
        {
            if (id != modelo.Id) return NotFound();
            if (modelo.EquipoCasaId == modelo.EquipoVisitaId)
                ModelState.AddModelError(nameof(Partido.EquipoVisitaId), "El equipo visitante debe ser diferente al equipo de casa.");
            if (!ModelState.IsValid)
            {
                CargarCombos(modelo.EquipoCasaId, modelo.EquipoVisitaId);
                return View(modelo);
            }
            _context.Entry(modelo).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id is null) return NotFound();
            var partido = await _context.Partidos.AsNoTracking()
                .Include(p => p.EquipoCasa)
                .Include(p => p.EquipoVisita)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (partido is null) return NotFound();
            return View(partido);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmado(int id)
        {
            var partido = await _context.Partidos.FindAsync(id);
            if (partido is not null)
            {
                _context.Partidos.Remove(partido);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [AllowAnonymous]
        public async Task<IActionResult> VerPartido(int? id)
        {
            if (id is null) return NotFound();
            var partido = await _context.Partidos.AsNoTracking()
                .Include(p => p.EquipoCasa)
                .Include(p => p.EquipoVisita)
                .Include(p => p.Entradas)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (partido is null) return NotFound();
            partido.Entradas = partido.Entradas?
                .OrderBy(e => e.NumeroInning)
                .ToList() ?? new List<Entrada>();
            var maxInnings = Math.Max(9, Math.Max(partido.EntradaActual, partido.Entradas.Select(e => e.NumeroInning).DefaultIfEmpty(0).Max()));
            var (lineupJugadores, bateadorEsperado) = await ObtenerContextoBateadorAsync(partido);
            string? motivoBloqueo = null;
            if (!lineupJugadores.Any())
            {
                motivoBloqueo = "Debes definir el lineup del equipo al bate antes de registrar jugadas.";
            }
            else if (partido.Estado != EstadoPartido.EnCurso)
            {
                motivoBloqueo = "El partido debe estar en curso para registrar jugadas.";
            }
            else if (bateadorEsperado == null)
            {
                motivoBloqueo = "No se pudo determinar el bateador esperado. Revisa el lineup.";
            }

            var turnoVm = new RegistrarTurnoVm
            {
                PartidoId = partido.Id,
                Resultado = ResultadoTurno.Sencillo,
                JugadorId = bateadorEsperado?.Id ?? 0
            };

            var vm = new VerPartidoVm
            {
                Partido = partido,
                Lineup = lineupJugadores,
                BateadorEsperado = bateadorEsperado,
                Turno = turnoVm,
                MaxInnings = maxInnings,
                MotivoBloqueoTurno = motivoBloqueo
            };

            return View(vm);
        }

        [Authorize(Roles = "Admin,Anotador")]
        public async Task<IActionResult> DefinirLineup(int id)
        {
            var partido = await _context.Partidos.Include(p => p.EquipoCasa).Include(p => p.EquipoVisita).FirstOrDefaultAsync(p => p.Id == id);
            if (partido == null) return NotFound();
            var jugadoresCasa = await _context.Jugadores.AsNoTracking().Where(j => j.EquipoId == partido.EquipoCasaId).OrderBy(j => j.Nombre).ThenBy(j => j.Apellido).ToListAsync();
            var jugadoresVisita = await _context.Jugadores.AsNoTracking().Where(j => j.EquipoId == partido.EquipoVisitaId).OrderBy(j => j.Nombre).ThenBy(j => j.Apellido).ToListAsync();
            var lineupCasa = await _context.Lineups.AsNoTracking().Where(l => l.PartidoId == id && l.EquipoId == partido.EquipoCasaId).OrderBy(l => l.Orden).ToListAsync();
            var lineupVisita = await _context.Lineups.AsNoTracking().Where(l => l.PartidoId == id && l.EquipoId == partido.EquipoVisitaId).OrderBy(l => l.Orden).ToListAsync();
            ViewBag.JugadoresCasa = jugadoresCasa;
            ViewBag.JugadoresVisita = jugadoresVisita;
            ViewBag.LineupCasa = lineupCasa;
            ViewBag.LineupVisita = lineupVisita;
            return View(partido);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Anotador")]
        public async Task<IActionResult> DefinirLineup(int id, [FromForm] int[]? casaJugadores, [FromForm] int[]? visitaJugadores)
        {
            var partido = await _context.Partidos.FindAsync(id);
            if (partido == null) return NotFound();
            if (casaJugadores is not null)
            {
                var existentesCasa = await _context.Lineups.Where(l => l.PartidoId == id && l.EquipoId == partido.EquipoCasaId).ToListAsync();
                if (existentesCasa.Any()) _context.Lineups.RemoveRange(existentesCasa);
                int orden = 1;
                foreach (var j in casaJugadores.Where(x => x > 0))
                    _context.Lineups.Add(new LineupItem { PartidoId = id, EquipoId = partido.EquipoCasaId, JugadorId = j, Orden = orden++ });
            }
            if (visitaJugadores is not null)
            {
                var existentesVisita = await _context.Lineups.Where(l => l.PartidoId == id && l.EquipoId == partido.EquipoVisitaId).ToListAsync();
                if (existentesVisita.Any()) _context.Lineups.RemoveRange(existentesVisita);
                int orden = 1;
                foreach (var j in visitaJugadores.Where(x => x > 0))
                    _context.Lineups.Add(new LineupItem { PartidoId = id, EquipoId = partido.EquipoVisitaId, JugadorId = j, Orden = orden++ });
            }
            await _context.SaveChangesAsync();
            TempData["Ok"] = "Lineup actualizado.";
            return RedirectToAction(nameof(DefinirLineup), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Anotador")]
        public async Task<IActionResult> GuardarLineupVisitante(int id, [FromForm] int[] visitaJugadores)
        {
            var partido = await _context.Partidos.FindAsync(id);
            if (partido == null) return NotFound();
            var existentes = await _context.Lineups.Where(l => l.PartidoId == id && l.EquipoId == partido.EquipoVisitaId).ToListAsync();
            if (existentes.Any()) _context.Lineups.RemoveRange(existentes);
            int orden = 1;
            if (visitaJugadores != null)
            {
                foreach (var j in visitaJugadores.Where(x => x > 0))
                    _context.Lineups.Add(new LineupItem { PartidoId = id, EquipoId = partido.EquipoVisitaId, JugadorId = j, Orden = orden++ });
            }
            await _context.SaveChangesAsync();
            TempData["Ok"] = "Lineup visitante guardado.";
            return RedirectToAction(nameof(DefinirLineup), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Anotador")]
        public async Task<IActionResult> GuardarLineupCasa(int id, [FromForm] int[] casaJugadores)
        {
            var partido = await _context.Partidos.FindAsync(id);
            if (partido == null) return NotFound();
            var existentes = await _context.Lineups.Where(l => l.PartidoId == id && l.EquipoId == partido.EquipoCasaId).ToListAsync();
            if (existentes.Any()) _context.Lineups.RemoveRange(existentes);
            int orden = 1;
            if (casaJugadores != null)
            {
                foreach (var j in casaJugadores.Where(x => x > 0))
                    _context.Lineups.Add(new LineupItem { PartidoId = id, EquipoId = partido.EquipoCasaId, JugadorId = j, Orden = orden++ });
            }
            await _context.SaveChangesAsync();
            TempData["Ok"] = "Lineup casa guardado.";
            return RedirectToAction(nameof(DefinirLineup), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Anotador")]
        public async Task<IActionResult> UpdateMarcador(int id, List<Entrada> entradas)
        {
            var partido = await _context.Partidos.Include(p => p.Entradas).FirstOrDefaultAsync(p => p.Id == id);
            if (partido is null) return NotFound();
            var existentes = partido.Entradas.ToList();
            if (existentes.Any()) _context.Entradas.RemoveRange(existentes);
            if (entradas != null && entradas.Any())
            {
                foreach (var e in entradas)
                {
                    e.PartidoId = partido.Id;
                    if (e.NumeroInning < 1) e.NumeroInning = 1;
                    _context.Entradas.Add(e);
                }
            }
            partido.CarrerasCasa = entradas?.Sum(x => x.CarrerasCasa) ?? 0;
            partido.CarrerasVisita = entradas?.Sum(x => x.CarrerasVisita) ?? 0;
            partido.HitsCasa = entradas?.Sum(x => x.HitsCasa) ?? 0;
            partido.HitsVisita = entradas?.Sum(x => x.HitsVisita) ?? 0;
            partido.ErroresCasa = entradas?.Sum(x => x.ErroresCasa) ?? 0;
            partido.ErroresVisita = entradas?.Sum(x => x.ErroresVisita) ?? 0;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(VerPartido), new { id = partido.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Anotador")]
        public async Task<IActionResult> Iniciar(int id)
        {
            try
            {
                await _marcador.IniciarPartidoAsync(id);
                TempData["Ok"] = "Partido iniciado: entrada 1 (alta), outs en 0.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction(nameof(VerPartido), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Anotador")]
        public async Task<IActionResult> Suspender(int id)
        {
            try
            {
                await _marcador.SuspenderPartidoAsync(id);
                TempData["Ok"] = "Partido suspendido.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction(nameof(VerPartido), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Anotador")]
        public async Task<IActionResult> Reanudar(int id)
        {
            try
            {
                await _marcador.ReanudarPartidoAsync(id);
                TempData["Ok"] = "Partido reanudado.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction(nameof(VerPartido), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Anotador")]
        public async Task<IActionResult> Finalizar(int id)
        {
            try
            {
                await _marcador.FinalizarPartidoAsync(id);
                TempData["Ok"] = "Partido finalizado.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction(nameof(VerPartido), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Anotador")]
        public async Task<IActionResult> RegistrarTurno([FromForm] RegistrarTurnoVm turno)
        {
            var esAjax = EsAjaxRequest();
            if (!ModelState.IsValid)
            {
                return ErrorRegistro("Debe seleccionar un bateador y un resultado válidos.", esAjax, turno.PartidoId);
            }

            var partido = await _context.Partidos.AsNoTracking().FirstOrDefaultAsync(p => p.Id == turno.PartidoId);
            if (partido == null)
            {
                return ErrorRegistro("Partido no encontrado.", esAjax, turno.PartidoId);
            }
            if (partido.Estado != EstadoPartido.EnCurso)
            {
                return ErrorRegistro("Solo puedes registrar jugadas cuando el partido está en curso.", esAjax, turno.PartidoId);
            }

            var (lineupActual, bateadorEsperado) = await ObtenerContextoBateadorAsync(partido);
            if (!lineupActual.Any())
            {
                return ErrorRegistro("Debe definir el lineup antes de registrar jugadas.", esAjax, turno.PartidoId);
            }
            if (bateadorEsperado == null)
            {
                return ErrorRegistro("No se pudo determinar el bateador esperado. Refresca la página o revisa el lineup.", esAjax, turno.PartidoId);
            }
            if (turno.JugadorId.HasValue && turno.JugadorId.Value != bateadorEsperado.Id)
            {
                return ErrorRegistro("El formulario está desactualizado. Refresca la página antes de registrar el turno.", esAjax, turno.PartidoId);
            }

            try
            {
                var partidoActualizado = await _marcador.RegistrarTurnoAsync(turno.PartidoId, turno.JugadorId, turno.Resultado);
                var marcador = await _marcador.ObtenerMarcadorAsync(partidoActualizado.Id);
                var proximoBateador = marcador.BateadorEsperado;
                var puedeRegistrar = marcador.PuedeRegistrar;
                var motivoBloqueo = marcador.MotivoBloqueo;

                if (esAjax)
                {
                    return Json(new
                    {
                        ok = true,
                        message = "Turno registrado correctamente.",
                        puedeRegistrar,
                        motivoBloqueo,
                        bateador = proximoBateador == null ? null : new
                        {
                            proximoBateador.Id,
                            proximoBateador.Nombre,
                            proximoBateador.Apellido,
                            proximoBateador.NumeroUniforme
                        },
                        estado = new
                        {
                            marcador.Estado,
                            marcador.Mitad,
                            marcador.EntradaActual,
                            marcador.Outs
                        }
                    });
                }

                TempData["Ok"] = "Turno registrado correctamente.";
                return RedirectToAction(nameof(VerPartido), new { id = turno.PartidoId });
            }
            catch (Exception ex)
            {
                return ErrorRegistro(ex.Message, esAjax, turno.PartidoId);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Anotador")]
        public async Task<IActionResult> Deshacer(int partidoId)
        {
            try
            {
                await _marcador.DeshacerUltimaJugadaAsync(partidoId);
                TempData["Ok"] = "Se deshizo la última jugada.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction(nameof(VerPartido), new { id = partidoId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Anotador")]
        public async Task<IActionResult> Rehacer(int partidoId)
        {
            try
            {
                await _marcador.RehacerUltimaJugadaAsync(partidoId);
                TempData["Ok"] = "Se rehizo la jugada.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction(nameof(VerPartido), new { id = partidoId });
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ExportarBoxScore(int id)
        {
            var partido = await _context.Partidos.AsNoTracking()
                .Include(p => p.Entradas)
                .Include(p => p.EquipoCasa)
                .Include(p => p.EquipoVisita)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (partido == null) return NotFound();
            var maxInnings = Math.Max(9, partido.Entradas?.Select(e => e.NumeroInning).DefaultIfEmpty(0).Max() ?? 9);
            var sb = new StringBuilder();
            sb.Append("Equipo");
            for (int i = 1; i <= maxInnings; i++) sb.Append($";{i}");
            sb.Append(";C;H;E\r\n");
            sb.Append(partido.EquipoVisita?.Nombre ?? "Visita");
            for (int i = 1; i <= maxInnings; i++)
            {
                var e = partido.Entradas?.FirstOrDefault(x => x.NumeroInning == i);
                var r = e?.CarrerasVisita ?? 0;
                sb.Append($";{r}");
            }
            sb.Append($";{partido.CarrerasVisita};{partido.HitsVisita};{partido.ErroresVisita}\r\n");
            sb.Append(partido.EquipoCasa?.Nombre ?? "Casa");
            for (int i = 1; i <= maxInnings; i++)
            {
                var e = partido.Entradas?.FirstOrDefault(x => x.NumeroInning == i);
                var r = e?.CarrerasCasa ?? 0;
                sb.Append($";{r}");
            }
            sb.Append($";{partido.CarrerasCasa};{partido.HitsCasa};{partido.ErroresCasa}\r\n");
            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv; charset=utf-8", $"boxscore-partido-{id}.csv");
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Anotador")]
        public async Task<IActionResult> BoxScoreCsv(int id)
        {
            try
            {
                var bytes = await _reportes.GenerarBoxScoreCsvAsync(id);
                return File(bytes, "text/csv", $"boxscore_partido_{id}.csv");
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Anotador")]
        public async Task<IActionResult> PlayByPlayCsv(int id)
        {
            try
            {
                var bytes = await _reportes.GenerarPlayByPlayCsvAsync(id);
                return File(bytes, "text/csv", $"playbyplay_partido_{id}.csv");
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> MarcadorJson(int id)
        {
            try
            {
                var dto = await _marcador.ObtenerMarcadorAsync(id);
                return Json(dto);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
