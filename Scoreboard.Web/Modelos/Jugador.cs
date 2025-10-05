using System.ComponentModel.DataAnnotations;

namespace Scoreboard.Web.Modelos
{
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

        public int EquipoId { get; set; }
        public Equipo? Equipo { get; set; }
    }
}
