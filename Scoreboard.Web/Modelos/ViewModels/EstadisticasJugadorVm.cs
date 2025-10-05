namespace Scoreboard.Web.Modelos.ViewModels
{
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

