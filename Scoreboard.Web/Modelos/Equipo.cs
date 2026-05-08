using System.ComponentModel.DataAnnotations;

namespace Scoreboard.Web.Modelos
{
    /// <summary>
    /// Entidad que representa un equipo de softbol.
    ///
    /// Se conecta con:
    /// - Jugador: cada jugador pertenece a un equipo.
    /// - Partido: un equipo puede jugar como casa o visitante.
    /// - LineupItem: define jugadores disponibles en un partido.
    ///
    /// Flujo simple:
    /// 1. Guarda nombre y ciudad.
    /// 2. Se usa para crear partidos y agrupar jugadores.
    /// 3. Aparece en marcador, estadisticas y reportes.
    ///
    /// Cuidado:
    /// Cambiar o borrar equipos afecta jugadores, partidos e historial.
    /// </summary>
    public class Equipo
    {
        public int Id { get; set; }

        [Required, StringLength(80)]
        public string Nombre { get; set; } = "";

        [StringLength(80)]
        public string? Ciudad { get; set; }

        [StringLength(260)]
        public string? LogoRuta { get; set; }
    }
}
