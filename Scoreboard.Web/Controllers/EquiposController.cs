using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scoreboard.Web.Datos;
using Scoreboard.Web.Models;

namespace Scoreboard.Web.Controllers
{
    public class EquiposController : Controller
    {
        private readonly ContextoMarcador _db;

        public EquiposController(ContextoMarcador db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var equipos = await _db.Equipos
                                   .AsNoTracking()
                                   .OrderBy(e => e.Nombre)
                                   .ToListAsync();
            return View(equipos);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id is null) return NotFound();

            var equipo = await _db.Equipos.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
            if (equipo is null) return NotFound();

            return View(equipo);
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Equipo modelo)
        {
            if (!ModelState.IsValid) return View(modelo);

            _db.Equipos.Add(modelo);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id is null) return NotFound();

            var equipo = await _db.Equipos.FindAsync(id);
            if (equipo is null) return NotFound();

            return View(equipo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Equipo modelo)
        {
            if (id != modelo.Id) return NotFound();
            if (!ModelState.IsValid) return View(modelo);

            _db.Entry(modelo).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id is null) return NotFound();

            var equipo = await _db.Equipos.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
            if (equipo is null) return NotFound();

            return View(equipo);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var equipo = await _db.Equipos.FindAsync(id);
            if (equipo is not null)
            {
                _db.Equipos.Remove(equipo);
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
