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
        private readonly Scoreboard.Web.Servicios.IEstadisticasService _estadisticasService;

        public JugadoresController(ContextoMarcador db, Scoreboard.Web.Servicios.IEstadisticasService estadisticasService)
        {
            _db = db;
            _estadisticasService = estadisticasService;
        }

        public async Task<IActionResult> Index(int? equipoId, string? q, int pagina = 1)
        {
            const int pageSize = 10;

            var query = _db.Jugadores
                .Include(j => j.Equipo)
                .AsQueryable();

            // Filtro por equipo
            if (equipoId.HasValue)
                query = query.Where(j => j.EquipoId == equipoId.Value);

            // Filtro por búsqueda (nombre, apellido o número de uniforme)
            if (!string.IsNullOrWhiteSpace(q))
            {
                var texto = q.Trim();
                query = query.Where(j =>
                    j.Nombre.Contains(texto) ||
                    j.Apellido.Contains(texto) ||
                    j.NumeroUniforme.ToString().Contains(texto));
            }

            // Calcular paginación
            var totalRegistros = await query.CountAsync();
            var totalPaginas = (int)Math.Ceiling(totalRegistros / (double)pageSize);

            // Validar página actual
            if (pagina < 1) pagina = 1;
            if (pagina > totalPaginas && totalPaginas > 0) pagina = totalPaginas;

            // Obtener jugadores paginados
            var jugadores = await query
                .OrderBy(j => j.Equipo!.Nombre)
                .ThenBy(j => j.NumeroUniforme)
                .Skip((pagina - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Cargar equipos para dropdown
            var equipos = await _db.Equipos
                .OrderBy(e => e.Nombre)
                .ToListAsync();

            ViewBag.Equipos = new SelectList(equipos, "Id", "Nombre", equipoId);
            ViewBag.q = q;
            ViewBag.PaginaActual = pagina;
            ViewBag.TotalPaginas = totalPaginas;

            return View(jugadores);
        }

        [HttpGet]
        public async Task<IActionResult> Buscar(string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return Json(new List<object>());

            var texto = q.Trim();
            var jugadores = await _db.Jugadores
                .Where(j => j.Nombre.Contains(texto) || j.Apellido.Contains(texto))
                .OrderBy(j => j.Nombre)
                .ThenBy(j => j.Apellido)
                .Take(10)
                .Select(j => new
                {
                    nombre = j.Nombre,
                    apellido = j.Apellido,
                    nombreCompleto = j.Nombre + " " + j.Apellido
                })
                .ToListAsync();

            return Json(jugadores);
        }

        public async Task<IActionResult> Details(int id)
        {
            var jugador = await _db.Jugadores
                                   .Include(j => j.Equipo)
                                   .FirstOrDefaultAsync(j => j.Id == id);
            if (jugador == null) return NotFound();
            return View(jugador);
        }

    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create()
        {
            await CargarEquiposAsync();
            return View(new Jugador());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
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

    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id)
        {
            var jugador = await _db.Jugadores.FindAsync(id);
            if (jugador == null) return NotFound();
            await CargarEquiposAsync(jugador.EquipoId);
            return View(jugador);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
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

    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
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
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
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

        [HttpGet]
        public async Task<IActionResult> Estadisticas(int id)
        {
            var datos = await _estadisticasService.ObtenerEstadisticasJugadorAsync(id);
            if (datos == null) return NotFound();
            return View(datos);
        }

        private async Task CargarEquiposAsync(int? seleccionado = null)
        {
            ViewData["EquipoId"] = new SelectList(
                await _db.Equipos.OrderBy(e => e.Nombre).ToListAsync(),
                "Id", "Nombre", seleccionado);
        }
    }
}
