using System.ComponentModel.DataAnnotations;

namespace Scoreboard.Web.Modelos
{
    public class Entrada
    {
        public int Id { get; set; }

        [Required]
        public int PartidoId { get; set; }

        // Numero del inning (1..9+)
        public int NumeroInning { get; set; }

        // Carreras anotadas por el equipo de casa y visita en esta entrada
        public int CarrerasCasa { get; set; }
        public int CarrerasVisita { get; set; }

    // Hits y errores por entrada (opcionales)
    public int HitsCasa { get; set; }
    public int HitsVisita { get; set; }
    public int ErroresCasa { get; set; }
    public int ErroresVisita { get; set; }

        public Partido? Partido { get; set; }
    }
}
