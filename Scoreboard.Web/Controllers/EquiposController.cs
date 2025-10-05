using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scoreboard.Web.Datos;
using Scoreboard.Web.Modelos;
using Scoreboard.Web.Modelos.ViewModels;

namespace Scoreboard.Web.Controllers
{
    public class EquiposController : Controller
    {
        private readonly ContextoMarcador _db;

        public EquiposController(ContextoMarcador db)
        {
            _db = db;
        }

        // GET: /Equipos
        public async Task<IActionResult> Index()
        {
            var lista = await _db.Equipos
                                 .OrderBy(e => e.Nombre)
                                 .ToListAsync();
            return View(lista);
        }

        // GET: /Equipos/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var equipo = await _db.Equipos.FindAsync(id);
            if (equipo == null) return NotFound();
            return View(equipo);
        }

        // GET: /Equipos/Create
        public IActionResult Create()
        {
            return View(new Equipo());
        }

        // POST: /Equipos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Equipo equipo)
        {
            if (!ModelState.IsValid) return View(equipo);

            _db.Add(equipo);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: /Equipos/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var equipo = await _db.Equipos.FindAsync(id);
            if (equipo == null) return NotFound();
            return View(equipo);
        }

        // POST: /Equipos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Equipo equipo)
        {
            if (id != equipo.Id) return BadRequest();
            if (!ModelState.IsValid) return View(equipo);

            try
            {
                _db.Update(equipo);
                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                var existe = await _db.Equipos.AnyAsync(e => e.Id == id);
                if (!existe) return NotFound();
                throw;
            }
        }

        // GET: /Equipos/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var equipo = await _db.Equipos.FindAsync(id);
            if (equipo == null) return NotFound();
            return View(equipo);
        }

        // POST: /Equipos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var equipo = await _db.Equipos.FindAsync(id);
            if (equipo != null)
            {
                _db.Equipos.Remove(equipo);
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
