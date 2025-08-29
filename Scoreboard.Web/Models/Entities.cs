using System;
using System.ComponentModel.DataAnnotations;

namespace Scoreboard.Web.Models
{
    public class Equipo
    {
        public int Id { get; set; }

        [Required, StringLength(80)]
        public string Nombre { get; set; } = "";
    }

    public class Jugador
    {
        public int Id { get; set; }

        [Required, StringLength(60)]
        public string Nombre { get; set; } = "";

        [Required, StringLength(60)]
        public string Apellido { get; set; } = "";

        [Range(0, 99)]
        public int NumeroUniforme { get; set; }

        public int EquipoId { get; set; }
        public Equipo? Equipo { get; set; }
    }

    public class Partido
    {
        public int Id { get; set; }
        public int EquipoLocalId { get; set; }
        public int EquipoVisitanteId { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;
        public int CarrerasLocal { get; set; }
        public int CarrerasVisitante { get; set; }
    }
}
