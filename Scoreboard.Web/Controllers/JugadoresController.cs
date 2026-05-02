using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Scoreboard.Web.Datos;
using Scoreboard.Web.Modelos;
using Scoreboard.Web.Modelos.ViewModels;

namespace Scoreboard.Web.Controllers
{
    /// <summary>
    /// Controlador MVC para administrar jugadores.
    ///
    /// Se conecta con:
    /// - ContextoMarcador: para leer y guardar jugadores.
    /// - IEstadisticasService: para mostrar estadisticas individuales.
    /// - Views/Jugadores: para listar, crear, editar y ver detalles.
    ///
    /// Flujo simple:
    /// 1. Recibe filtros o formularios de jugadores.
    /// 2. Consulta equipos/jugadores y valida datos.
    /// 3. Guarda cambios o muestra estadisticas.
    ///
    /// Cuidado:
    /// Los jugadores se usan en lineups, jugadas e historial; evita borrar datos sin revisar relaciones.
    /// </summary>
    public class JugadoresController : Controller
    {
        private const long FotoPerfilTamanoMaximoBytes = 2 * 1024 * 1024;
        private static readonly HashSet<string> ExtensionesFotoPermitidas = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp"
        };

        private readonly ContextoMarcador _db;
        private readonly Scoreboard.Web.Servicios.IEstadisticasService _estadisticasService;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public JugadoresController(
            ContextoMarcador db,
            Scoreboard.Web.Servicios.IEstadisticasService estadisticasService,
            IWebHostEnvironment webHostEnvironment)
        {
            _db = db;
            _estadisticasService = estadisticasService;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index(string? q, int? equipoId, int page = 1, int pageSize = 15)
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

            page = page <= 0 ? 1 : page;
            pageSize = 15;

            var totalRegistros = await consulta.CountAsync();

            ViewData["Equipos"] = new SelectList(
                await _db.Equipos.OrderBy(e => e.Nombre).ToListAsync(), "Id", "Nombre", equipoId);
            ViewData["q"] = q;

            var jugadores = await consulta
                .OrderBy(j => j.Equipo!.Nombre)
                .ThenBy(j => j.NumeroUniforme)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();

            var totalPaginas = (int)Math.Ceiling(totalRegistros / (double)pageSize);

            ViewBag.PaginaActual = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalRegistros = totalRegistros;
            ViewBag.TotalPaginas = totalPaginas <= 0 ? 1 : totalPaginas;

            return View(jugadores);
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
    public async Task<IActionResult> Create(Jugador jugador, IFormFile? fotoPerfil)
        {
            ValidarFotoPerfil(fotoPerfil);

            if (!ModelState.IsValid)
            {
                await CargarEquiposAsync(jugador.EquipoId);
                return View(jugador);
            }

            jugador.FotoPerfilRuta = await GuardarFotoPerfilAsync(fotoPerfil);

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
    public async Task<IActionResult> Edit(int id, Jugador jugador, IFormFile? fotoPerfil)
        {
            if (id != jugador.Id) return BadRequest();

            ValidarFotoPerfil(fotoPerfil);

            if (!ModelState.IsValid)
            {
                await CargarEquiposAsync(jugador.EquipoId);
                return View(jugador);
            }

            var existente = await _db.Jugadores.FindAsync(id);
            if (existente == null) return NotFound();

            try
            {
                existente.Nombre = jugador.Nombre;
                existente.Apellido = jugador.Apellido;
                existente.NumeroUniforme = jugador.NumeroUniforme;
                existente.Posicion = jugador.Posicion;
                existente.EquipoId = jugador.EquipoId;

                var nuevaFoto = await GuardarFotoPerfilAsync(fotoPerfil);
                if (!string.IsNullOrWhiteSpace(nuevaFoto))
                {
                    existente.FotoPerfilRuta = nuevaFoto;
                }

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

        private void ValidarFotoPerfil(IFormFile? fotoPerfil)
        {
            if (fotoPerfil == null || fotoPerfil.Length == 0)
                return;

            if (fotoPerfil.Length > FotoPerfilTamanoMaximoBytes)
            {
                ModelState.AddModelError("fotoPerfil", "La foto no puede pesar mas de 2 MB.");
            }

            var extension = Path.GetExtension(fotoPerfil.FileName);
            if (!ExtensionesFotoPermitidas.Contains(extension))
            {
                ModelState.AddModelError("fotoPerfil", "La foto debe ser JPG, PNG o WEBP.");
            }
        }

        private async Task<string?> GuardarFotoPerfilAsync(IFormFile? fotoPerfil)
        {
            if (fotoPerfil == null || fotoPerfil.Length == 0)
                return null;

            var extension = Path.GetExtension(fotoPerfil.FileName).ToLowerInvariant();
            var nombreArchivo = $"{Guid.NewGuid():N}{extension}";
            var carpetaRelativa = Path.Combine("uploads", "jugadores");
            var carpetaFisica = Path.Combine(_webHostEnvironment.WebRootPath, carpetaRelativa);
            Directory.CreateDirectory(carpetaFisica);

            var rutaFisica = Path.Combine(carpetaFisica, nombreArchivo);
            await using var stream = new FileStream(rutaFisica, FileMode.CreateNew);
            await fotoPerfil.CopyToAsync(stream);

            return "/" + Path.Combine(carpetaRelativa, nombreArchivo).Replace('\\', '/');
        }
    }
}
