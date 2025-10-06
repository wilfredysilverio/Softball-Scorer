namespace Scoreboard.Web.Modelos.ViewModels
{
    public class LineaTemporadaVm
    {
        public string Temporada { get; set; } = "-";
        public int G { get; set; }
        public int AB { get; set; }
        public int R { get; set; }
        public int H { get; set; }
        public int Doubles { get; set; }
        public int Triples { get; set; }
        public int HR { get; set; }
        public int RBI { get; set; }
        public int BB { get; set; }
        public int SO { get; set; }
        public decimal AVG { get; set; }
        public decimal OBP { get; set; }
        public decimal SLG { get; set; }
    }
}
