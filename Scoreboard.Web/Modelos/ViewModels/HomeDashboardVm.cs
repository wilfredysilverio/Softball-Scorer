using System;
using System.Collections.Generic;

namespace Scoreboard.Web.Modelos.ViewModels
{
    public class HomeDashboardVm
    {
        public int TotalEquipos { get; set; }
        public int TotalJugadores { get; set; }
        public int TotalPartidos { get; set; }
        public DateTime? ProximoJuego { get; set; }

        public List<JugadorRecienteVm> JugadoresRecientes { get; set; } = new();
        public List<PartidoRecienteVm> PartidosRecientes { get; set; } = new();
    }

    public class JugadorRecienteVm
    {
        public int Id { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
    }

    public class PartidoRecienteVm
    {
        public int Id { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
    }
}
