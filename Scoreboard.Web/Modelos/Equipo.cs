using System.ComponentModel.DataAnnotations;

namespace Scoreboard.Web.Modelos
{
    public class Equipo
    {
        public int Id { get; set; }

        [Required, StringLength(80)]
        public string Nombre { get; set; } = "";

        [StringLength(80)]
        public string? Ciudad { get; set; } 
    }
}
