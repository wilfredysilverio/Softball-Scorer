using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Scoreboard.Web.Modelos
{
    public class PlayerBattingStat
    {
        public int Id { get; set; }
        public int JugadorId { get; set; }
        public Jugador? Jugador { get; set; }
        public int PartidoId { get; set; }
        public Partido? Partido { get; set; }
        public DateTime Fecha { get; set; }

        // Bateo
        public int AB { get; set; } // At bats
        public int R { get; set; }
        public int H { get; set; }
        public int Doubles { get; set; }
        public int Triples { get; set; }
        public int HR { get; set; }
        public int RBI { get; set; }
        public int BB { get; set; }
        public int SO { get; set; }
        public int HBP { get; set; }
        public int SF { get; set; } // sacrifice flies
    }
}
