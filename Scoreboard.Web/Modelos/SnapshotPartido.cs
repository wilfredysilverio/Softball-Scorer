using System.Collections.Generic;

namespace Scoreboard.Web.Modelos
{
    public class SnapshotEntrada
    {
        public int NumeroInning { get; set; }
        public int CarrerasCasa { get; set; }
        public int CarrerasVisita { get; set; }
        public int HitsCasa { get; set; }
        public int HitsVisita { get; set; }
        public int ErroresCasa { get; set; }
        public int ErroresVisita { get; set; }
    }

    public class SnapshotPartido
    {
        public int EntradaActual { get; set; }
        public string Mitad { get; set; } = "Alta";
        public int Outs { get; set; }
        public bool B1 { get; set; }
        public bool B2 { get; set; }
        public bool B3 { get; set; }
        public int CarrerasCasa { get; set; }
        public int CarrerasVisita { get; set; }
        public int HitsCasa { get; set; }
        public int HitsVisita { get; set; }
        public int ErroresCasa { get; set; }
        public int ErroresVisita { get; set; }
        public int Estado { get; set; }
        public int? IndexBateadorCasa { get; set; }
        public int? IndexBateadorVisita { get; set; }
        public List<SnapshotEntrada> Entradas { get; set; } = new();
    }

    // Wrapper para almacenar antes y después en un solo JSON
    public class PlaySnapshotWrapper
    {
        public SnapshotPartido? Before { get; set; }
        public SnapshotPartido? After { get; set; }
    }
}
