using System.ComponentModel.DataAnnotations;

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
        public DateTime CreadoUtc { get; set; } = DateTime.UtcNow;

        [Required]
        public PlayLogKind Kind { get; set; } = PlayLogKind.BeforePlay;

      
        [Required]
        public string SnapshotJson { get; set; } = string.Empty;
        
        // Optional: link to the player and result for redoing/analytics
        public int? JugadorId { get; set; }
        public ResultadoTurno? Resultado { get; set; }
        public int RunsScored { get; set; }

        // Soft-delete / active flag for undo/redo
        public bool IsActive { get; set; } = true;

        // Backwards-compatible Fecha property used by some services/tests
        public DateTime Fecha
        {
            get => CreadoUtc;
            set => CreadoUtc = value;
        }
    }
}
