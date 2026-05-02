using System.ComponentModel.DataAnnotations;

namespace Scoreboard.Web.Modelos
{
    /// <summary>
    /// Entidad que define un jugador dentro del lineup de un partido.
    ///
    /// Se conecta con:
    /// - Partido: indica en que juego participa.
    /// - Equipo: separa lineup de casa y visitante.
    /// - Jugador: indica quien batea en ese turno.
    ///
    /// Flujo simple:
    /// 1. Guarda partido, equipo, jugador y orden.
    /// 2. MarcadorService usa el orden para saber quien batea.
    /// 3. Al terminar un turno, el indice avanza al siguiente jugador.
    ///
    /// Cuidado:
    /// Si el orden o jugador son incorrectos, el marcador puede anotar al bateador equivocado.
    /// </summary>
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
