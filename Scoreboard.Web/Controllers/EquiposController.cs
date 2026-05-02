using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scoreboard.Web.Datos;
using Scoreboard.Web.Modelos;
using Scoreboard.Web.Modelos.ViewModels;

namespace Scoreboard.Web.Controllers
{
    /// <summary>
    /// Controlador MVC para administrar equipos.
    ///
    /// Se conecta con:
    /// - ContextoMarcador: para leer y guardar equipos.
    /// - Views/Equipos: para listar, crear, editar, ver detalles y estadisticas.
    /// - EstadisticasService: para datos agregados cuando aplica.
    ///
    /// Flujo simple:
    /// 1. Recibe acciones sobre equipos.
    /// 2. Consulta o modifica la tabla Equipos.
    /// 3. Devuelve una vista o redirecciona.
    ///
    /// Cuidado:
    /// Un equipo puede estar relacionado con jugadores y partidos; borrar o cambiar ids afecta esas relaciones.
    /// </summary>
    public class EquiposController : Controller
    {
        private readonly ContextoMarcador _db;

        public EquiposController(ContextoMarcador db)
        {
            _db = db;
        }

        // GET: /Equipos
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    public async Task<IActionResult> Index()
        {
            var lista = await _db.Equipos
                                 .OrderBy(e => e.Nombre)
                                 .ToListAsync();
            return View(lista);
        }

        // GET: /Equipos/Estadisticas/5
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    public async Task<IActionResult> Estadisticas(int id)
        {
            // Usar el servicio de estadísticas para obtener los datos del equipo
            var service = HttpContext.RequestServices.GetService(typeof(Scoreboard.Web.Servicios.IEstadisticasService)) as Scoreboard.Web.Servicios.IEstadisticasService;
            if (service == null) return StatusCode(500, "Servicio de estadísticas no disponible");

            var vm = await service.ObtenerEstadisticasEquipoAsync(id);
            if (vm == null) return NotFound();
            return View(vm);
        }

        // GET: /Equipos/Details/5
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    public async Task<IActionResult> Details(int id)
        {
            // Consulta de sólo lectura: usar AsNoTracking para evitar seguimiento del contexto
            // y reducir la sobrecarga cuando sólo mostramos detalles.
            var equipo = await _db.Equipos
                                 .AsNoTracking()
                                 .FirstOrDefaultAsync(e => e.Id == id);
            if (equipo == null) return NotFound();
            return View(equipo);
        }

        // GET: /Equipos/Create
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
    public IActionResult Create()
        {
            return View(new Equipo());
        }

        // POST: /Equipos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(Equipo equipo)
        {
            if (!ModelState.IsValid) return View(equipo);

            _db.Add(equipo);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: /Equipos/Edit/5
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id)
        {
            var equipo = await _db.Equipos.FindAsync(id);
            if (equipo == null) return NotFound();
            return View(equipo);
        }

        // POST: /Equipos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
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
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
        {
            var equipo = await _db.Equipos.FindAsync(id);
            if (equipo == null) return NotFound();
            return View(equipo);
        }

        // POST: /Equipos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
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
