using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Scoreboard.Web.Modelos.ViewModels
{
    public class JugadoresIndexViewModel
    {
        public IEnumerable<Jugador> Jugadores { get; set; } = Enumerable.Empty<Jugador>();

        public int? EquipoId { get; set; }
        public string? Busqueda { get; set; }

        public IEnumerable<SelectListItem> Equipos { get; set; } = Enumerable.Empty<SelectListItem>();

        public int PaginaActual { get; set; }
        public int TotalPaginas { get; set; }
    }
}
