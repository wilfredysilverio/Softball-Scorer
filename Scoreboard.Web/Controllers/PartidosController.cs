using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Scoreboard.Web.Datos;
using Scoreboard.Web.Modelos;

namespace Scoreboard.Web.Controllers
{
    public class PartidosController : Controller
    {
        private readonly ContextoMarcador _db;

        public PartidosController(ContextoMarcador db)
        {
            _db = db;
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
            try
            {
                // Proyección “ligera”: solo campos seguros que ya existen.
                var lista = await _db.Partidos
                    .AsNoTracking()
                    .OrderByDescending(p => p.Fecha)
                    .Select(p => new Partido
                    {
                        Id = p.Id,
                        Fecha = p.Fecha,
                        EquipoCasaId = p.EquipoCasaId,
                        EquipoVisitaId = p.EquipoVisitaId,
                        CarrerasCasa = p.CarrerasCasa,
                        CarrerasVisita = p.CarrerasVisita
                        // OJO: no tocamos navegaciones para evitar joins que fallen
                        // EquipoCasa / EquipoVisita quedarán null => la vista usa ?.Nombre
                    })
                    .ToListAsync();

                return View(lista);
            }
            catch (Exception ex)
            {
                // Entrar “sí o sí”
                TempData["Error"] = $"No se pudo cargar Partidos ({ex.GetType().Name}). Se muestra vacío.";
                return View(new List<Partido>());
            }
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

            // Jugadores del equipo que batea
            int equipoBateaId = partido.Mitad == MitadEntrada.Baja ? partido.EquipoCasaId : partido.EquipoVisitaId;
            var bateadores = await _db.Jugadores
                .AsNoTracking()
                .Where(j => j.EquipoId == equipoBateaId)
                .OrderBy(j => j.Nombre).ThenBy(j => j.Apellido)
                .ToListAsync();
            ViewBag.Bateadores = bateadores;
            return View(partido);
        }

        // POST: /Partidos/UpdateMarcador/5  (sin servicio Marcador)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateMarcador(int id, List<Entrada> entradas)
        {
            var partido = await _db.Partidos
                .Include(p => p.Entradas)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (partido is null) return NotFound();

            // Reemplazar entradas
            var existentes = partido.Entradas.ToList();
            if (existentes.Any())
            {
                _db.Set<Entrada>().RemoveRange(existentes);
            }

            if (entradas != null && entradas.Any())
            {
                foreach (var e in entradas)
                {
                    e.PartidoId = partido.Id;
                    if (e.NumeroInning < 1) e.NumeroInning = 1;
                    _db.Set<Entrada>().Add(e);
                }
            }

            // Recalcular totales
            var totCasa = entradas?.Sum(x => x.CarrerasCasa) ?? 0;
            var totVisita = entradas?.Sum(x => x.CarrerasVisita) ?? 0;
            var hitsCasa = entradas?.Sum(x => x.HitsCasa) ?? 0;
            var hitsVis = entradas?.Sum(x => x.HitsVisita) ?? 0;
            var errCasa = entradas?.Sum(x => x.ErroresCasa) ?? 0;
            var errVis = entradas?.Sum(x => x.ErroresVisita) ?? 0;

            partido.CarrerasCasa = totCasa;
            partido.CarrerasVisita = totVisita;
            partido.HitsCasa = hitsCasa;
            partido.HitsVisita = hitsVis;
            partido.ErroresCasa = errCasa;
            partido.ErroresVisita = errVis;

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(VerPartido), new { id = partido.Id });
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

            var entradas = (partido.Entradas ?? new List<Entrada>())
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
