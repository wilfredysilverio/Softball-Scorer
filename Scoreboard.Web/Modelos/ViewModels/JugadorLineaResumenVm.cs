namespace Scoreboard.Web.Modelos.ViewModels
{
    public class JugadorLineaResumenVm
    {
        public int JugadorId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public int Numero { get; set; }
        public int AB { get; set; }
        public int H { get; set; }
        public int HR { get; set; }
        public int RBI { get; set; }
        public int BB { get; set; }
        public int SO { get; set; }
        public int PA { get; set; }
        public decimal AVG { get; set; }
        public decimal OBP { get; set; }
        public decimal SLG { get; set; }
        public decimal OPS { get; set; }
    }
}
