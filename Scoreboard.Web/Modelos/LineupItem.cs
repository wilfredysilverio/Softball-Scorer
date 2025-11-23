using System.ComponentModel.DataAnnotations;

namespace Scoreboard.Web.Modelos
{
    public class LineupItem
    {
        public int Id { get; set; }

        [Required]
        public int PartidoId { get; set; }

        [Required]
        public int EquipoId { get; set; }

        [Required]
        public int JugadorId { get; set; }

        // Orden en el lineup (1..9 típico)
        [Range(1, 12)]
        public int Orden { get; set; }

        // Navegación opcional
        public Partido? Partido { get; set; }
        public Equipo? Equipo { get; set; }
        public Jugador? Jugador { get; set; }
    }
}
