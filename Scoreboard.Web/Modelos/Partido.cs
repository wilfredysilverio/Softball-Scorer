using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // <— agrega esto

namespace Scoreboard.Web.Modelos
{
    public class Partido
    {
        public int Id { get; set; }

        [Required] public int EquipoCasaId { get; set; }
        [Required] public int EquipoVisitaId { get; set; }

        public DateTime Fecha { get; set; } = DateTime.Now;

        public int CarrerasCasa { get; set; }
        public int CarrerasVisita { get; set; }

        // Totales adicionales (ignorar por ahora para que no falle EF)
        [NotMapped] public int HitsCasa { get; set; }
        [NotMapped] public int HitsVisita { get; set; }
        [NotMapped] public int ErroresCasa { get; set; }
        [NotMapped] public int ErroresVisita { get; set; }

        public int EntradaActual { get; set; } = 1;
        public MitadEntrada Mitad { get; set; } = MitadEntrada.Alta;
        public int Outs { get; set; } = 0;

        public EstadoPartido Estado { get; set; } = EstadoPartido.NoIniciado;

        public bool B1 { get; set; } = false;
        public bool B2 { get; set; } = false;
        public bool B3 { get; set; } = false;

        public Equipo? EquipoCasa { get; set; }
        public Equipo? EquipoVisita { get; set; }

        public List<Entrada> Entradas { get; set; } = new();
        public List<PlayerBattingStat> PlayerBattingStats { get; set; } = new();
    }
}
