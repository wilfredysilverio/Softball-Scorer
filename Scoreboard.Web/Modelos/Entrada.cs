using System.ComponentModel.DataAnnotations;

namespace Scoreboard.Web.Modelos
{
    /// <summary>
    /// Entidad que guarda el resumen de una entrada o inning.
    ///
    /// Se conecta con:
    /// - Partido: pertenece a un partido.
    /// - MarcadorService: actualiza carreras, hits y errores por entrada.
    /// - ReportesService: la usa para exportar box score.
    ///
    /// Flujo simple:
    /// 1. Identifica el numero de inning.
    /// 2. Guarda totales de casa y visitante en esa entrada.
    /// 3. Se muestra en la tabla del marcador.
    ///
    /// Cuidado:
    /// Si cambian estos totales, revisar marcador por entradas y reportes.
    /// </summary>
    public class Entrada
    {
        public int Id { get; set; }

        [Required]
        public int PartidoId { get; set; }

        // Numero del inning (1..9+)
        public int NumeroInning { get; set; }

        // Carreras anotadas por el equipo de casa y visita en esta entrada
        public int CarrerasCasa { get; set; }
        public int CarrerasVisita { get; set; }

        // Hits y errores por entrada (opcionales)
        public int HitsCasa { get; set; }
        public int HitsVisita { get; set; }
        public int ErroresCasa { get; set; }
        public int ErroresVisita { get; set; }

        public Partido? Partido { get; set; }
    }
}
