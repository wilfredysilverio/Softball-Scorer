using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Scoreboard.Web.Modelos
{
    /// <summary>
    /// Tipo de registro dentro del historial de jugadas.
    /// </summary>
    public enum PlayLogKind
    {
        BeforePlay = 0, 
        Redo = 1       
    }

    /// <summary>
    /// Entidad que guarda el historial de cada jugada del partido.
    ///
    /// Se conecta con:
    /// - Partido: indica a que juego pertenece la jugada.
    /// - Jugador: bateador registrado.
    /// - MarcadorService: crea el historial cuando aplica una jugada.
    /// - PlayLogSnapshotHelper: interpreta el snapshot para mostrar/reportar.
    ///
    /// Flujo simple:
    /// 1. Guarda resultado y carreras de la jugada.
    /// 2. Guarda snapshot antes/despues en JSON.
    /// 3. Permite mostrar historial, deshacer y exportar jugada por jugada.
    ///
    /// Cuidado:
    /// Cambiar el formato de SnapshotJson puede afectar historial, undo/redo y reportes.
    /// </summary>
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
        public int? JugadorEsperadoId { get; set; }
        public int? EquipoBateoId { get; set; }
        public ResultadoTurno? Resultado { get; set; }
        public int RunsScored { get; set; }
        public bool EsCorreccionManual { get; set; }
        [StringLength(240)]
        public string? Nota { get; set; }

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
