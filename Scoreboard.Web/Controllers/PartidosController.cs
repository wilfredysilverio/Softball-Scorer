using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Scoreboard.Web.Datos;
using Scoreboard.Web.Models;

namespace Scoreboard.Web.Controllers
{
    public class PartidosController : Controller
    {
        private readonly ContextoMarcador _db;
        public PartidosController(ContextoMarcador db) => _db = db;

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
    }
}
