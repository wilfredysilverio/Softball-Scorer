using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Scoreboard.Web.Datos;
using Scoreboard.Web.Modelos;
using Scoreboard.Web.Modelos.ViewModels;

namespace Scoreboard.Web.Controllers
{
    public class JugadoresController : Controller
    {
        private readonly ContextoMarcador _db;

        public JugadoresController(ContextoMarcador db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index(string? q, int? equipoId)
        {
            var consulta = _db.Jugadores
                              .Include(j => j.Equipo)
                              .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var texto = q.Trim();
                consulta = consulta.Where(j =>
                    j.Nombre.Contains(texto) ||
                    j.Apellido.Contains(texto) ||
                    j.NumeroUniforme.ToString().Contains(texto));
            }

            if (equipoId.HasValue)
                consulta = consulta.Where(j => j.EquipoId == equipoId.Value);

            ViewData["Equipos"] = new SelectList(await _db.Equipos.OrderBy(e => e.Nombre).ToListAsync(), "Id", "Nombre", equipoId);
            ViewData["q"] = q;

            var lista = await consulta
                .OrderBy(j => j.Equipo!.Nombre)
                .ThenBy(j => j.NumeroUniforme)
                .ToListAsync();

            return View(lista);
        }

        public async Task<IActionResult> Details(int id)
        {
            var jugador = await _db.Jugadores
                                   .Include(j => j.Equipo)
                                   .FirstOrDefaultAsync(j => j.Id == id);
            if (jugador == null) return NotFound();
            return View(jugador);
        }

        public async Task<IActionResult> Create()
        {
            await CargarEquiposAsync();
            return View(new Jugador());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Jugador jugador)
        {
            if (!ModelState.IsValid)
            {
                await CargarEquiposAsync(jugador.EquipoId);
                return View(jugador);
            }

            _db.Add(jugador);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var jugador = await _db.Jugadores.FindAsync(id);
            if (jugador == null) return NotFound();
            await CargarEquiposAsync(jugador.EquipoId);
            return View(jugador);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Jugador jugador)
        {
            if (id != jugador.Id) return BadRequest();

            if (!ModelState.IsValid)
            {
                await CargarEquiposAsync(jugador.EquipoId);
                return View(jugador);
            }

            try
            {
                _db.Update(jugador);
                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _db.Jugadores.AnyAsync(j => j.Id == id))
                    return NotFound();
                throw;
            }
        }

        public async Task<IActionResult> Delete(int id)
        {
            var jugador = await _db.Jugadores
                                   .Include(j => j.Equipo)
                                   .FirstOrDefaultAsync(j => j.Id == id);
            if (jugador == null) return NotFound();
            return View(jugador);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmado(int id)
        {
            var jugador = await _db.Jugadores.FindAsync(id);
            if (jugador != null)
            {
                _db.Jugadores.Remove(jugador);
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }


        private async Task CargarEquiposAsync(int? seleccionado = null)
        {
            ViewData["EquipoId"] = new SelectList(
                await _db.Equipos.OrderBy(e => e.Nombre).ToListAsync(),
                "Id", "Nombre", seleccionado);
        }
    }

    // ViewModel simple para estadísticas
    public class EstadisticasJugadorVm
    {
        public Jugador Jugador { get; set; } = default!;
        public int PartidosJugados { get; set; }
        public int Turnos { get; set; }
        public int Hits { get; set; }
        public int Carreras { get; set; }
        public int HomeRuns { get; set; }
        public decimal Promedio { get; set; } // AVG = Hits / Turnos
    }
}
