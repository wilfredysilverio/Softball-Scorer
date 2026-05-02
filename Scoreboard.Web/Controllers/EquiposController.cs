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
        private const long LogoTamanoMaximoBytes = 2 * 1024 * 1024;
        private static readonly HashSet<string> ExtensionesLogoPermitidas = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp",
            ".svg"
        };

        private readonly ContextoMarcador _db;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public EquiposController(ContextoMarcador db, IWebHostEnvironment webHostEnvironment)
        {
            _db = db;
            _webHostEnvironment = webHostEnvironment;
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
    public async Task<IActionResult> Create(Equipo equipo, IFormFile? logoEquipo)
        {
            ValidarLogoEquipo(logoEquipo);

            if (!ModelState.IsValid) return View(equipo);

            equipo.LogoRuta = await GuardarLogoEquipoAsync(logoEquipo);

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
    public async Task<IActionResult> Edit(int id, Equipo equipo, IFormFile? logoEquipo)
        {
            if (id != equipo.Id) return BadRequest();

            ValidarLogoEquipo(logoEquipo);

            if (!ModelState.IsValid) return View(equipo);

            var existente = await _db.Equipos.FindAsync(id);
            if (existente == null) return NotFound();

            try
            {
                existente.Nombre = equipo.Nombre;
                existente.Ciudad = equipo.Ciudad;

                var nuevoLogo = await GuardarLogoEquipoAsync(logoEquipo);
                if (!string.IsNullOrWhiteSpace(nuevoLogo))
                {
                    existente.LogoRuta = nuevoLogo;
                }

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

        private void ValidarLogoEquipo(IFormFile? logoEquipo)
        {
            if (logoEquipo == null || logoEquipo.Length == 0)
                return;

            if (logoEquipo.Length > LogoTamanoMaximoBytes)
            {
                ModelState.AddModelError("logoEquipo", "El logo no puede pesar mas de 2 MB.");
            }

            var extension = Path.GetExtension(logoEquipo.FileName);
            if (!ExtensionesLogoPermitidas.Contains(extension))
            {
                ModelState.AddModelError("logoEquipo", "El logo debe ser JPG, PNG, WEBP o SVG.");
            }
        }

        private async Task<string?> GuardarLogoEquipoAsync(IFormFile? logoEquipo)
        {
            if (logoEquipo == null || logoEquipo.Length == 0)
                return null;

            var extension = Path.GetExtension(logoEquipo.FileName).ToLowerInvariant();
            var nombreArchivo = $"{Guid.NewGuid():N}{extension}";
            var carpetaRelativa = Path.Combine("uploads", "equipos");
            var carpetaFisica = Path.Combine(_webHostEnvironment.WebRootPath, carpetaRelativa);
            Directory.CreateDirectory(carpetaFisica);

            var rutaFisica = Path.Combine(carpetaFisica, nombreArchivo);
            await using var stream = new FileStream(rutaFisica, FileMode.CreateNew);
            await logoEquipo.CopyToAsync(stream);

            return "/" + Path.Combine(carpetaRelativa, nombreArchivo).Replace('\\', '/');
        }
    }
}
