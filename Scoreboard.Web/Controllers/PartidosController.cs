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
    /// <summary>
    /// Controlador MVC de partidos y marcador.
    ///
    /// Se conecta con:
    /// - ContextoMarcador: para consultar partidos, equipos, jugadores y jugadas.
    /// - IMarcadorService: para aplicar reglas del juego y actualizar el marcador.
    /// - IReportesService: para generar descargas CSV.
    /// - Views/Partidos: para mostrar crear, listar, definir lineup y anotar partidos.
    ///
    /// Flujo simple:
    /// 1. Recibe acciones del usuario desde las pantallas de partidos.
    /// 2. Valida datos basicos y delega la logica pesada a servicios.
    /// 3. Devuelve una vista, una redireccion, un archivo CSV o JSON para JavaScript.
    ///
    /// Cuidado:
    /// Este controlador conecta la pantalla con el marcador. Cambios en nombres de acciones,
    /// parametros o JSON deben revisarse junto con VerPartido.cshtml y marcador.js.
    /// </summary>
    public class PartidosController : Controller
    {
        private readonly ContextoMarcador _db;
        private readonly IMarcadorService _marcador;
        private readonly IReportesService _reportes;
        public PartidosController(ContextoMarcador db, IMarcadorService marcador, IReportesService reportes)
        {
            _db = db;
            _marcador = marcador;
            _reportes = reportes;
        }

        private void CargarCombos(int? casaId = null, int? visitaId = null)
        {
            var equipos = _db.Equipos.AsNoTracking().OrderBy(e => e.Nombre).ToList();
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

        private IActionResult RequiereConfirmacionFueraTurno(string mensaje, bool esAjax, int partidoId, Jugador bateadorEsperado)
        {
            if (esAjax)
            {
                return Json(new
                {
                    ok = false,
                    requiereConfirmacion = true,
                    message = mensaje,
                    bateadorEsperado = new
                    {
                        bateadorEsperado.Id,
                        bateadorEsperado.Nombre,
                        bateadorEsperado.Apellido,
                        bateadorEsperado.NumeroUniforme
                    }
                });
            }

            TempData["Error"] = mensaje;
            return RedirectToAction(nameof(VerPartido), new { id = partidoId });
        }

        private async Task<(List<Jugador> Lineup, Jugador? Bateador)> ObtenerContextoBateadorAsync(Partido partido)
        {
            var equipoBateaId = partido.Mitad == MitadEntrada.Baja ? partido.EquipoCasaId : partido.EquipoVisitaId;
            var lineupItems = await _db.Lineups.AsNoTracking()
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

        private async Task CrearLineupAutomaticoSiFaltaAsync(int partidoId, int equipoId)
        {
            var yaTieneLineup = await _db.Lineups.AnyAsync(l => l.PartidoId == partidoId && l.EquipoId == equipoId);
            if (yaTieneLineup)
            {
                return;
            }

            var jugadores = await _db.Jugadores.AsNoTracking()
                .Where(j => j.EquipoId == equipoId)
                .OrderBy(j => j.NumeroUniforme)
                .ThenBy(j => j.Nombre)
                .ThenBy(j => j.Apellido)
                .Select(j => j.Id)
                .ToListAsync();

            var orden = 1;
            foreach (var jugadorId in jugadores)
            {
                _db.Lineups.Add(new LineupItem
                {
                    PartidoId = partidoId,
                    EquipoId = equipoId,
                    JugadorId = jugadorId,
                    Orden = orden++
                });
            }
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var lista = await _db.Partidos.AsNoTracking()
                .Include(p => p.EquipoCasa)
                .Include(p => p.EquipoVisita)
                .OrderByDescending(p => p.Fecha)
                .ToListAsync();
            return View(lista);
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
            _db.Partidos.Add(modelo);
            await _db.SaveChangesAsync();
            await CrearLineupAutomaticoSiFaltaAsync(modelo.Id, modelo.EquipoVisitaId);
            await CrearLineupAutomaticoSiFaltaAsync(modelo.Id, modelo.EquipoCasaId);
            await _db.SaveChangesAsync();

            TempData["Ok"] = "Partido creado con el roster completo. Desmarca los jugadores que no fueron hoy.";
            return RedirectToAction(nameof(DefinirLineup), new { id = modelo.Id });
        }

        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id is null) return NotFound();
            var partido = await _db.Partidos.AsNoTracking()
                .Include(p => p.EquipoCasa)
                .Include(p => p.EquipoVisita)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (partido is null) return NotFound();
            return View(partido);
        }

        [AllowAnonymous]
        public async Task<IActionResult> MarcadorPublico(int id)
        {
            var partido = await _db.Partidos.AsNoTracking()
                .Include(p => p.EquipoCasa)
                .Include(p => p.EquipoVisita)
                .Include(p => p.Entradas)
                .Include(p => p.PlayerBattingStats)
                    .ThenInclude(s => s.Jugador)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (partido == null) return NotFound();

            var entradas = EntradaHelper.NormalizarEntradas(partido.Entradas);
            partido.Entradas = entradas;

            var jugadas = await _db.PlayLogs.AsNoTracking()
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
                UltimasJugadas = jugadas,
                Outs = partido.Outs
            };

            return View(vm);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id is null) return NotFound();
            var partido = await _db.Partidos.FindAsync(id);
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
            _db.Entry(modelo).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id is null) return NotFound();
            var partido = await _db.Partidos.AsNoTracking()
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
            var partido = await _db.Partidos.FindAsync(id);
            if (partido is not null)
            {
                _db.Partidos.Remove(partido);
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [AllowAnonymous]
        public async Task<IActionResult> VerPartido(int? id)
        {
            if (id is null) return NotFound();
            var partido = await _db.Partidos.AsNoTracking()
                .Include(p => p.EquipoCasa)
                .Include(p => p.EquipoVisita)
                .Include(p => p.Entradas)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (partido is null) return NotFound();
            var entradasOrdenadas = EntradaHelper.NormalizarEntradas(partido.Entradas);
            partido.Entradas = entradasOrdenadas;
            var maxInnings = Math.Max(9, Math.Max(partido.EntradaActual, entradasOrdenadas.Select(e => e.NumeroInning).DefaultIfEmpty(0).Max()));
            var (lineupJugadores, bateadorEsperado) = await ObtenerContextoBateadorAsync(partido);
            var ultimasJugadas = await _db.PlayLogs.AsNoTracking()
                .Include(pl => pl.Jugador)
                .Where(pl => pl.PartidoId == partido.Id && pl.IsActive && pl.Resultado != null)
                .OrderByDescending(pl => pl.Id)
                .Take(8)
                .ToListAsync();

            foreach (var log in ultimasJugadas)
            {
                var (before, after) = PlayLogSnapshotHelper.ExtractSnapshots(log.SnapshotJson);
                log.EntradaContext = before?.EntradaActual ?? after?.EntradaActual;
                log.MitadContext = before?.Mitad ?? after?.Mitad;
                log.OutsAfter = after?.Outs;
                if (after != null)
                {
                    log.BasesAfter = $"1B:{(after.B1 ? "ocupada" : "libre")} 2B:{(after.B2 ? "ocupada" : "libre")} 3B:{(after.B3 ? "ocupada" : "libre")}";
                }
            }

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
                UltimasJugadas = ultimasJugadas,
                Turno = turnoVm,
                Evento = new RegistrarEventoCorredorVm
                {
                    PartidoId = partido.Id,
                    Evento = EventoCorredor.Ninguno,
                    Base = BaseCorredor.Primera
                },
                MaxInnings = maxInnings,
                MotivoBloqueoTurno = motivoBloqueo,
                Outs = partido.Outs
            };

            return View(vm);
        }

        [Authorize(Roles = "Admin,Anotador")]
        public async Task<IActionResult> DefinirLineup(int id)
        {
            var partido = await _db.Partidos.Include(p => p.EquipoCasa).Include(p => p.EquipoVisita).FirstOrDefaultAsync(p => p.Id == id);
            if (partido == null) return NotFound();
            await CrearLineupAutomaticoSiFaltaAsync(partido.Id, partido.EquipoVisitaId);
            await CrearLineupAutomaticoSiFaltaAsync(partido.Id, partido.EquipoCasaId);
            await _db.SaveChangesAsync();

            var jugadoresCasa = await _db.Jugadores.AsNoTracking().Where(j => j.EquipoId == partido.EquipoCasaId).OrderBy(j => j.NumeroUniforme).ThenBy(j => j.Nombre).ThenBy(j => j.Apellido).ToListAsync();
            var jugadoresVisita = await _db.Jugadores.AsNoTracking().Where(j => j.EquipoId == partido.EquipoVisitaId).OrderBy(j => j.NumeroUniforme).ThenBy(j => j.Nombre).ThenBy(j => j.Apellido).ToListAsync();
            var lineupCasa = await _db.Lineups.AsNoTracking().Where(l => l.PartidoId == id && l.EquipoId == partido.EquipoCasaId).OrderBy(l => l.Orden).ToListAsync();
            var lineupVisita = await _db.Lineups.AsNoTracking().Where(l => l.PartidoId == id && l.EquipoId == partido.EquipoVisitaId).OrderBy(l => l.Orden).ToListAsync();
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
            var partido = await _db.Partidos.FindAsync(id);
            if (partido == null) return NotFound();
            var existentesCasa = await _db.Lineups.Where(l => l.PartidoId == id && l.EquipoId == partido.EquipoCasaId).ToListAsync();
            if (existentesCasa.Any()) _db.Lineups.RemoveRange(existentesCasa);
            int ordenCasa = 1;
            foreach (var j in (casaJugadores ?? Array.Empty<int>()).Where(x => x > 0))
                _db.Lineups.Add(new LineupItem { PartidoId = id, EquipoId = partido.EquipoCasaId, JugadorId = j, Orden = ordenCasa++ });

            var existentesVisita = await _db.Lineups.Where(l => l.PartidoId == id && l.EquipoId == partido.EquipoVisitaId).ToListAsync();
            if (existentesVisita.Any()) _db.Lineups.RemoveRange(existentesVisita);
            int ordenVisita = 1;
            foreach (var j in (visitaJugadores ?? Array.Empty<int>()).Where(x => x > 0))
                _db.Lineups.Add(new LineupItem { PartidoId = id, EquipoId = partido.EquipoVisitaId, JugadorId = j, Orden = ordenVisita++ });

            await _db.SaveChangesAsync();
            TempData["Ok"] = "Jugadores del partido actualizados.";
            return RedirectToAction(nameof(DefinirLineup), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Anotador")]
        public async Task<IActionResult> GuardarLineupVisitante(int id, [FromForm] int[]? visitaJugadores)
        {
            var partido = await _db.Partidos.FindAsync(id);
            if (partido == null) return NotFound();
            var existentes = await _db.Lineups.Where(l => l.PartidoId == id && l.EquipoId == partido.EquipoVisitaId).ToListAsync();
            if (existentes.Any()) _db.Lineups.RemoveRange(existentes);
            int orden = 1;
            foreach (var j in (visitaJugadores ?? Array.Empty<int>()).Where(x => x > 0))
                _db.Lineups.Add(new LineupItem { PartidoId = id, EquipoId = partido.EquipoVisitaId, JugadorId = j, Orden = orden++ });
            await _db.SaveChangesAsync();
            TempData["Ok"] = "Jugadores visitantes actualizados.";
            return RedirectToAction(nameof(DefinirLineup), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Anotador")]
        public async Task<IActionResult> GuardarLineupCasa(int id, [FromForm] int[]? casaJugadores)
        {
            var partido = await _db.Partidos.FindAsync(id);
            if (partido == null) return NotFound();
            var existentes = await _db.Lineups.Where(l => l.PartidoId == id && l.EquipoId == partido.EquipoCasaId).ToListAsync();
            if (existentes.Any()) _db.Lineups.RemoveRange(existentes);
            int orden = 1;
            foreach (var j in (casaJugadores ?? Array.Empty<int>()).Where(x => x > 0))
                _db.Lineups.Add(new LineupItem { PartidoId = id, EquipoId = partido.EquipoCasaId, JugadorId = j, Orden = orden++ });
            await _db.SaveChangesAsync();
            TempData["Ok"] = "Jugadores de casa actualizados.";
            return RedirectToAction(nameof(DefinirLineup), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Anotador")]
        public async Task<IActionResult> UpdateMarcador(int id, List<Entrada> entradas)
        {
            var partido = await _db.Partidos.Include(p => p.Entradas).FirstOrDefaultAsync(p => p.Id == id);
            if (partido is null) return NotFound();
            var existentes = partido.Entradas.ToList();
            if (existentes.Any()) _db.Entradas.RemoveRange(existentes);
            if (entradas != null && entradas.Any())
            {
                foreach (var e in entradas)
                {
                    e.PartidoId = partido.Id;
                    if (e.NumeroInning < 1) e.NumeroInning = 1;
                    _db.Entradas.Add(e);
                }
            }
            partido.CarrerasCasa = entradas?.Sum(x => x.CarrerasCasa) ?? 0;
            partido.CarrerasVisita = entradas?.Sum(x => x.CarrerasVisita) ?? 0;
            partido.HitsCasa = entradas?.Sum(x => x.HitsCasa) ?? 0;
            partido.HitsVisita = entradas?.Sum(x => x.HitsVisita) ?? 0;
            partido.ErroresCasa = entradas?.Sum(x => x.ErroresCasa) ?? 0;
            partido.ErroresVisita = entradas?.Sum(x => x.ErroresVisita) ?? 0;
            await _db.SaveChangesAsync();
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
                return ErrorRegistro("Debe seleccionar un bateador y un resultado vÃ¡lidos.", esAjax, turno.PartidoId);
            }

            var partido = await _db.Partidos.AsNoTracking().FirstOrDefaultAsync(p => p.Id == turno.PartidoId);
            if (partido == null)
            {
                return ErrorRegistro("Partido no encontrado.", esAjax, turno.PartidoId);
            }
            if (partido.Estado != EstadoPartido.EnCurso)
            {
                return ErrorRegistro("Solo puedes registrar jugadas cuando el partido estÃ¡ en curso.", esAjax, turno.PartidoId);
            }

            var (lineupActual, bateadorEsperado) = await ObtenerContextoBateadorAsync(partido);
            if (!lineupActual.Any())
            {
                return ErrorRegistro("Debe definir el lineup antes de registrar jugadas.", esAjax, turno.PartidoId);
            }
            if (bateadorEsperado == null)
            {
                return ErrorRegistro("No se pudo determinar el bateador esperado. Refresca la pÃ¡gina o revisa el lineup.", esAjax, turno.PartidoId);
            }
            if (turno.JugadorId.HasValue && turno.JugadorId.Value != bateadorEsperado.Id && !turno.ConfirmarFueraTurno)
            {
                return RequiereConfirmacionFueraTurno("Este jugador no es el bateador que sigue en el orden. ¿Seguro que deseas anotar esta jugada para él?", esAjax, turno.PartidoId, bateadorEsperado);
            }

            var eventoCorredorSeleccionado = turno.EventoCorredor ?? EventoCorredor.Ninguno;
            var baseEventoSeleccionada = turno.BaseEvento;
            if (eventoCorredorSeleccionado != EventoCorredor.Ninguno && !baseEventoSeleccionada.HasValue)
            {
                return ErrorRegistro("Selecciona el corredor que avanzÃ³ con el evento.", esAjax, turno.PartidoId);
            }
            if (eventoCorredorSeleccionado != EventoCorredor.Ninguno && !(partido.B1 || partido.B2 || partido.B3))
            {
                return ErrorRegistro("No hay corredores en base para aplicar el evento seleccionado.", esAjax, turno.PartidoId);
            }
            if (eventoCorredorSeleccionado != EventoCorredor.Ninguno && baseEventoSeleccionada.HasValue)
            {
                var baseDisponible = baseEventoSeleccionada.Value switch
                {
                    BaseCorredor.Primera => partido.B1,
                    BaseCorredor.Segunda => partido.B2,
                    BaseCorredor.Tercera => partido.B3,
                    _ => false
                };
                if (!baseDisponible)
                {
                    return ErrorRegistro("La base seleccionada no tiene corredor actualmente. Refresca el marcador e intenta nuevamente.", esAjax, turno.PartidoId);
                }
            }

            try
            {
                var baseEvento = baseEventoSeleccionada ?? BaseCorredor.Primera;
                var partidoActualizado = await _marcador.RegistrarTurnoAsync(
                    turno.PartidoId,
                    turno.JugadorId,
                    turno.Resultado,
                    eventoCorredorSeleccionado,
                    baseEvento,
                    turno.ConfirmarFueraTurno);
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
                        equipoBateando = marcador.EquipoBateando,
                        equipoBateandoId = marcador.EquipoBateandoId,
                        lineupBateando = marcador.LineupBateando,
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
        public async Task<IActionResult> RegistrarEventoCorredor([FromForm] RegistrarEventoCorredorVm request)
        {
            var esAjax = EsAjaxRequest();
            if (!ModelState.IsValid || request.Evento == EventoCorredor.Ninguno)
            {
                return ErrorRegistro("Selecciona un evento vÃ¡lido de corredores.", esAjax, request.PartidoId);
            }

            var partido = await _db.Partidos.AsNoTracking().FirstOrDefaultAsync(p => p.Id == request.PartidoId);
            if (partido == null)
            {
                return ErrorRegistro("Partido no encontrado.", esAjax, request.PartidoId);
            }
            if (partido.Estado != EstadoPartido.EnCurso)
            {
                return ErrorRegistro("Solo puedes registrar eventos con el partido en curso.", esAjax, request.PartidoId);
            }

            bool baseDisponible = request.Base switch
            {
                BaseCorredor.Primera => partido.B1,
                BaseCorredor.Segunda => partido.B2,
                BaseCorredor.Tercera => partido.B3,
                _ => false
            };
            if (!baseDisponible)
            {
                return ErrorRegistro("No hay corredor en la base seleccionada.", esAjax, request.PartidoId);
            }

            try
            {
                await _marcador.RegistrarEventoCorredorAsync(request.PartidoId, request.Evento, request.Base);
                if (esAjax)
                {
                    return Json(new { ok = true, message = "Evento registrado correctamente." });
                }
                TempData["Ok"] = "Evento registrado correctamente.";
                return RedirectToAction(nameof(VerPartido), new { id = request.PartidoId });
            }
            catch (Exception ex)
            {
                return ErrorRegistro(ex.Message, esAjax, request.PartidoId);
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
                TempData["Ok"] = "Se deshizo la Ãºltima jugada.";
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
            var partido = await _db.Partidos.AsNoTracking()
                .Include(p => p.Entradas)
                .Include(p => p.EquipoCasa)
                .Include(p => p.EquipoVisita)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (partido == null) return NotFound();
            var entradas = EntradaHelper.NormalizarEntradas(partido.Entradas);
            var maxInnings = Math.Max(9, entradas.Select(e => e.NumeroInning).DefaultIfEmpty(0).Max());
            var sb = new StringBuilder();
            sb.Append("Equipo");
            for (int i = 1; i <= maxInnings; i++) sb.Append($";{i}");
            sb.Append(";C;H;E\r\n");
            sb.Append(partido.EquipoVisita?.Nombre ?? "Visita");
            for (int i = 1; i <= maxInnings; i++)
            {
                var e = entradas.FirstOrDefault(x => x.NumeroInning == i);
                var r = e?.CarrerasVisita ?? 0;
                sb.Append($";{r}");
            }
            sb.Append($";{partido.CarrerasVisita};{partido.HitsVisita};{partido.ErroresVisita}\r\n");
            sb.Append(partido.EquipoCasa?.Nombre ?? "Casa");
            for (int i = 1; i <= maxInnings; i++)
            {
                var e = entradas.FirstOrDefault(x => x.NumeroInning == i);
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

