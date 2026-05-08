using System.ComponentModel.DataAnnotations;

namespace Scoreboard.Web.Modelos
{
    /// <summary>
    /// Entidad que representa un jugador del roster.
    ///
    /// Se conecta con:
    /// - Equipo: indica a que equipo pertenece.
    /// - LineupItem: indica si juega y en que orden batea.
    /// - PlayLog y PlayerBattingStat: guardan jugadas y estadisticas del jugador.
    ///
    /// Flujo simple:
    /// 1. Guarda nombre, apellido, numero y posicion.
    /// 2. Se selecciona en lineups y turnos al bate.
    /// 3. Recibe estadisticas cuando participa en jugadas.
    ///
    /// Cuidado:
    /// Cambiar ids o relaciones puede dejar historial y estadisticas sin jugador.
    /// </summary>
    public class Jugador
    {
        public int Id { get; set; }

        [Required, StringLength(60)]
        public string Nombre { get; set; } = "";

        [Required, StringLength(60)]
        public string Apellido { get; set; } = "";

        [Range(0, 99)]
        public int NumeroUniforme { get; set; }

        public Posicion Posicion { get; set; } = Posicion.Utility;

        [StringLength(260)]
        public string? FotoPerfilRuta { get; set; }

        public int EquipoId { get; set; }
        public Equipo? Equipo { get; set; }
    }
}
