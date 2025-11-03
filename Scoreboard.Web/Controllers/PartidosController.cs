using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Scoreboard.Web.Datos;
using Scoreboard.Web.Modelos;
using Scoreboard.Web.Modelos.ViewModels;
using Scoreboard.Web.Servicios;



namespace Scoreboard.Web.Controllers
{
    public class PartidosController : Controller
    {
        private readonly ContextoMarcador _db;
        private readonly Scoreboard.Web.Servicios.Marcador.IMarcadorService _marcador;
        public PartidosController(ContextoMarcador db, Scoreboard.Web.Servicios.Marcador.IMarcadorService marcador)
        {
            _db = db;
            _marcador = marcador;
        }

        // Combos de equipos
        private void CargarCombos(int? casaId = null, int? visitaId = null)
        {
            var equipos = _db.Equipos.AsNoTracking().OrderBy(e => e.Nombre).ToList();
            ViewBag.EquiposCasa = new SelectList(equipos, "Id", "Nombre", casaId);
            ViewBag.EquiposVisita = new SelectList(equipos, "Id", "Nombre", visitaId);
        }

        // LISTADO
        public async Task<IActionResult> Index()
        {
            var lista = await _db.Partidos
                .AsNoTracking()
                .Include(p => p.EquipoCasa)
                .Include(p => p.EquipoVisita)
                .OrderByDescending(p => p.Fecha)
                .ToListAsync();
            return View(lista);
        }

        public IActionResult Create()
        {
            CargarCombos();
            return View(new Partido { Fecha = DateTime.Now });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
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
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id is null) return NotFound();

            var partido = await _db.Partidos
                .AsNoTracking()
                .Include(p => p.EquipoCasa)
                .Include(p => p.EquipoVisita)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (partido is null) return NotFound();
            return View(partido);
        }

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

        public async Task<IActionResult> Delete(int? id)
        {
            if (id is null) return NotFound();

            var partido = await _db.Partidos
                .AsNoTracking()
                .Include(p => p.EquipoCasa)
                .Include(p => p.EquipoVisita)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (partido is null) return NotFound();
            return View(partido);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
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

        // GET: /Partidos/VerPartido/5
        public async Task<IActionResult> VerPartido(int? id)
        {
            if (id is null) return NotFound();

            var partido = await _db.Partidos
                .AsNoTracking()
                .Include(p => p.EquipoCasa)
                .Include(p => p.EquipoVisita)
                .Include(p => p.Entradas)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (partido is null) return NotFound();
            // Cargar lista de jugadores del equipo que está bateando
            int equipoBateaId = partido.Mitad == MitadEntrada.Baja ? partido.EquipoCasaId : partido.EquipoVisitaId;
            var bateadores = await _db.Jugadores
                .AsNoTracking()
                .Where(j => j.EquipoId == equipoBateaId)
                .OrderBy(j => j.Nombre).ThenBy(j => j.Apellido)
                .ToListAsync();
            ViewBag.Bateadores = bateadores;
            return View(partido);
        }

        // POST: /Partidos/UpdateMarcador/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateMarcador(int id, List<Scoreboard.Web.Modelos.Entrada> entradas)
        {
            var partido = await _db.Partidos
                .Include(p => p.Entradas)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (partido is null) return NotFound();

            // Replace existing entradas with submitted ones
            var existentes = partido.Entradas.ToList();
            if (existentes.Any())
            {
                _db.Entradas.RemoveRange(existentes);
            }

            if (entradas != null && entradas.Any())
            {
                // Normalize and assign PartidoId
                foreach (var e in entradas)
                {
                    e.PartidoId = partido.Id;
                    // Ensure inning number >=1
                    if (e.NumeroInning < 1) e.NumeroInning = 1;
                    _db.Entradas.Add(e);
                }
            }

            // Recalculate totals: carreras, hits y errores
            var totCasa = entradas?.Sum(x => x.CarrerasCasa) ?? 0;
            var totVisita = entradas?.Sum(x => x.CarrerasVisita) ?? 0;
            var hitsCasa = entradas?.Sum(x => x.HitsCasa) ?? 0;
            var hitsVisita = entradas?.Sum(x => x.HitsVisita) ?? 0;
            var errCasa = entradas?.Sum(x => x.ErroresCasa) ?? 0;
            var errVisita = entradas?.Sum(x => x.ErroresVisita) ?? 0;

            partido.CarrerasCasa = totCasa;
            partido.CarrerasVisita = totVisita;
            partido.HitsCasa = hitsCasa;
            partido.HitsVisita = hitsVisita;
            partido.ErroresCasa = errCasa;
            partido.ErroresVisita = errVisita;

            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(VerPartido), new { id = partido.Id });
        }

        // POST: /Partidos/Iniciar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public async Task<IActionResult> Iniciar(int id)
        {
            await _marcador.IniciarPartidoAsync(id);
            return RedirectToAction(nameof(VerPartido), new { id });
        }

        // POST: /Partidos/Suspender/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public async Task<IActionResult> Suspender(int id)
        {
            await _marcador.SuspenderPartidoAsync(id);
            return RedirectToAction(nameof(VerPartido), new { id });
        }

        // POST: /Partidos/Reanudar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public async Task<IActionResult> Reanudar(int id)
        {
            await _marcador.ReanudarPartidoAsync(id);
            return RedirectToAction(nameof(VerPartido), new { id });
        }

        // POST: /Partidos/Finalizar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public async Task<IActionResult> Finalizar(int id)
        {
            await _marcador.FinalizarPartidoAsync(id);
            return RedirectToAction(nameof(VerPartido), new { id });
        }

        // POST: /Partidos/RegistrarTurno
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public async Task<IActionResult> RegistrarTurno(int partidoId, int jugadorId, Scoreboard.Web.Modelos.ResultadoTurno resultado)
        {
            await _marcador.RegistrarTurnoAsync(partidoId, jugadorId, resultado);
            return RedirectToAction(nameof(VerPartido), new { id = partidoId });
        }

        // POST: /Partidos/Deshacer
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public async Task<IActionResult> Deshacer(int partidoId)
        {
            await _marcador.DeshacerUltimaJugadaAsync(partidoId);
            return RedirectToAction(nameof(VerPartido), new { id = partidoId });
        }

        // POST: /Partidos/Rehacer
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public async Task<IActionResult> Rehacer(int partidoId)
        {
            await _marcador.RehacerUltimaJugadaAsync(partidoId);
            return RedirectToAction(nameof(VerPartido), new { id = partidoId });
        }

        // GET: /Partidos/MarcadorJson/5
        [HttpGet]
        public async Task<IActionResult> MarcadorJson(int id)
        {
            var partido = await _db.Partidos
                .AsNoTracking()
                .Include(p => p.Entradas)
                .Include(p => p.EquipoCasa)
                .Include(p => p.EquipoVisita)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (partido is null) return NotFound();

            var entradas = partido.Entradas
                .OrderBy(e => e.NumeroInning)
                .Select(e => new
                {
                    e.NumeroInning,
                    e.CarrerasCasa,
                    e.CarrerasVisita,
                    e.HitsCasa,
                    e.HitsVisita,
                    e.ErroresCasa,
                    e.ErroresVisita
                })
                .ToList();

            return Json(new
            {
                entradas,
                partido.CarrerasCasa,
                partido.CarrerasVisita,
                partido.HitsCasa,
                partido.HitsVisita,
                partido.ErroresCasa,
                partido.ErroresVisita,
                partido.EntradaActual,
                Mitad = partido.Mitad.ToString(),
                partido.Outs
            });
        }
    }
}
