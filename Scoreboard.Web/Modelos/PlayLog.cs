using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Scoreboard.Web.Modelos
{
    public enum PlayLogKind
    {
        BeforePlay = 0, 
        Redo = 1       
    }

    public class PlayLog
    {
        public int Id { get; set; }

        [Required]
        public int PartidoId { get; set; }

        [Required]
        [Column("Fecha")]
        public DateTime CreadoUtc { get; set; } = DateTime.UtcNow;

        [Required]
        public PlayLogKind Kind { get; set; } = PlayLogKind.BeforePlay;

      
        [Required]
        public string SnapshotJson { get; set; } = string.Empty;
        
        // Optional: link to the player and result for redoing/analytics
        public int? JugadorId { get; set; }
        public ResultadoTurno? Resultado { get; set; }
        public int RunsScored { get; set; }

        // JSON con los incrementos de estadísticas ofensivas aplicados en la jugada
        public string? StatDeltaJson { get; set; }

        public Jugador? Jugador { get; set; }

        // Soft-delete / active flag for undo/redo
        public bool IsActive { get; set; } = true;

        // Backwards-compatible Fecha property used by some services/tests
        [NotMapped]
        public DateTime Fecha
        {
            get => CreadoUtc;
            set => CreadoUtc = value;
        }

        [NotMapped]
        public int? EntradaContext { get; set; }

        [NotMapped]
        public string? MitadContext { get; set; }

        [NotMapped]
        public int? OutsAfter { get; set; }

        [NotMapped]
        public string? BasesAfter { get; set; }
    }
}
