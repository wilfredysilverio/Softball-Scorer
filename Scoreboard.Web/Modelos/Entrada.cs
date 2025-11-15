using System.ComponentModel.DataAnnotations;

namespace Scoreboard.Web.Modelos
{
    public class Entrada
    {
        public int Id { get; set; }

        [Required]
        public int PartidoId { get; set; }

        // 1..9 (o más si hay extra innings)
        [Range(1, 50)]
        public int NumeroInning { get; set; } = 1;

        // Tablero por entrada
        public int CarrerasCasa { get; set; }
        public int CarrerasVisita { get; set; }

        // Totales por entrada (opcional, tus vistas los usan)
        public int HitsCasa { get; set; }
        public int HitsVisita { get; set; }
        public int ErroresCasa { get; set; }
        public int ErroresVisita { get; set; }

        // Navegación
        public Partido? Partido { get; set; }
    }
}
