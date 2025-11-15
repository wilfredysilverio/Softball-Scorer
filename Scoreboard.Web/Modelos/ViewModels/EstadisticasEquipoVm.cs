namespace Scoreboard.Web.Modelos.ViewModels
{
    public class EstadisticasEquipoVm
    {
        public Scoreboard.Web.Modelos.Equipo Equipo { get; set; } = default!;
        public int PartidosJugados { get; set; }
        public int Jugadores { get; set; }
        public int AB { get; set; }
        public int H { get; set; }
        public int R { get; set; }
        public int Doubles { get; set; }
        public int Triples { get; set; }
        public int HR { get; set; }
        public int RBI { get; set; }
        public int BB { get; set; }
        public int SO { get; set; }
        public int HBP { get; set; }
        public int SF { get; set; }
        public decimal AVG { get; set; }
        public decimal OBP { get; set; }
        public decimal SLG { get; set; }
        public List<LineaTemporadaVm> Lineas { get; set; } = new();

        // Compatibilidad con vistas existentes
        public int Turnos { get => AB; set => AB = value; }
        public int Hits { get => H; set => H = value; }
        public int Carreras { get => R; set => R = value; }
        public int HomeRuns { get => HR; set => HR = value; }
        public decimal Promedio { get => AVG; set => AVG = value; }
    }
}
