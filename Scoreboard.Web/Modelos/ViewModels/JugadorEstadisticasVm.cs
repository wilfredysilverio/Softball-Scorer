using System.Collections.Generic;

namespace Scoreboard.Web.Modelos.ViewModels
{
    public class JugadorEstadisticasVm
    {
        public Scoreboard.Web.Modelos.Jugador Jugador { get; set; } = default!;
        public string? FotoUrl { get; set; }
        public int Partidos { get; set; }
        public int? Turnos { get; set; }
        public int? Hits { get; set; }
        public int? Carreras { get; set; }
        public int? HomeRuns { get; set; }
        public decimal? Promedio { get; set; }
        public List<LineaTemporadaVm> Lineas { get; set; } = new();
    }
}
