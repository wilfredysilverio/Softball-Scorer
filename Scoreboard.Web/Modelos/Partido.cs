using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Scoreboard.Web.Modelos
{
    public class Partido
    {
        public int Id { get; set; }

        [Required]
        public int EquipoCasaId { get; set; }
        [Required]
        public int EquipoVisitaId { get; set; }

        public DateTime Fecha { get; set; } = DateTime.Now;

        public int CarrerasCasa { get; set; }
        public int CarrerasVisita { get; set; }

        // Totales adicionales
        public int HitsCasa { get; set; }
        public int HitsVisita { get; set; }
        public int ErroresCasa { get; set; }
        public int ErroresVisita { get; set; }

        public int EntradaActual { get; set; } = 1;
        public MitadEntrada Mitad { get; set; } = MitadEntrada.Alta;
        public int Outs { get; set; } = 0;

        // Estado del partido
        public EstadoPartido Estado { get; set; } = EstadoPartido.NoIniciado;

        // Estado real de las bases persistido
        public bool B1 { get; set; } = false;
        public bool B2 { get; set; } = false;
        public bool B3 { get; set; } = false;

        // Propiedades de apoyo NO mapeadas (para vistas como Estadio)
        [NotMapped]
        public bool Base1 { get; set; }
        [NotMapped]
        public bool Base2 { get; set; }
        [NotMapped]
        public bool Base3 { get; set; }
        [NotMapped]
        public string? Corredor1B { get; set; }
        [NotMapped]
        public string? Corredor2B { get; set; }
        [NotMapped]
        public string? Corredor3B { get; set; }

        public Equipo? EquipoCasa { get; set; }
        public Equipo? EquipoVisita { get; set; }

        public List<Entrada> Entradas { get; set; } = new();
        public List<PlayerBattingStat> PlayerBattingStats { get; set; } = new();
        public List<LineupItem> LineupCasa { get; set; } = new();
        public List<LineupItem> LineupVisita { get; set; } = new();
        public int? IndexBateadorCasa { get; set; }
        public int? IndexBateadorVisita { get; set; }
    }
}
    