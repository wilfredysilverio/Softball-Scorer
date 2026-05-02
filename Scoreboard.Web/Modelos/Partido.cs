using System.ComponentModel.DataAnnotations;

namespace Scoreboard.Web.Modelos
{
    /// <summary>
    /// Entidad central que representa un partido de softbol.
    ///
    /// Se conecta con:
    /// - Equipo: guarda equipo de casa y visitante.
    /// - Entrada: guarda carreras/hits/errores por inning.
    /// - LineupItem: controla el orden de bateo.
    /// - PlayLog y PlayerBattingStat: guardan historial y estadisticas.
    ///
    /// Flujo simple:
    /// 1. Mantiene marcador, entrada, mitad, outs y bases.
    /// 2. MarcadorService lo actualiza con cada jugada.
    /// 3. Las vistas lo muestran al anotador y al publico.
    ///
    /// Cuidado:
    /// Cambiar propiedades de estado puede romper calculo de outs, bases, carreras o turnos.
    /// </summary>
    public class Partido : IValidatableObject
    {
        public int Id { get; set; }

        [Required]
        public int EquipoCasaId { get; set; }

        [Required]
        public int EquipoVisitaId { get; set; }

        public DateTime Fecha { get; set; } = DateTime.Now;

        [Range(0, int.MaxValue, ErrorMessage = "Las carreras de casa no pueden ser negativas.")]
        public int CarrerasCasa { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Las carreras visitantes no pueden ser negativas.")]
        public int CarrerasVisita { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Los hits de casa no pueden ser negativos.")]
        public int HitsCasa { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Los hits visitantes no pueden ser negativos.")]
        public int HitsVisita { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Los errores de casa no pueden ser negativos.")]
        public int ErroresCasa { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Los errores visitantes no pueden ser negativos.")]
        public int ErroresVisita { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "La entrada actual debe ser mayor o igual a 1.")]
        public int EntradaActual { get; set; } = 1;

        public MitadEntrada Mitad { get; set; } = MitadEntrada.Alta;

        [Range(0, int.MaxValue, ErrorMessage = "Los outs no pueden ser negativos.")]
        public int Outs { get; set; }

        public EstadoPartido Estado { get; set; } = EstadoPartido.NoIniciado;

        public bool B1 { get; set; }
        public bool B2 { get; set; }
        public bool B3 { get; set; }

        public Equipo? EquipoCasa { get; set; }
        public Equipo? EquipoVisita { get; set; }

        public List<Entrada> Entradas { get; set; } = new();
        public List<PlayerBattingStat> PlayerBattingStats { get; set; } = new();

        public List<LineupItem> LineupCasa { get; set; } = new();
        public List<LineupItem> LineupVisita { get; set; } = new();

        public int? IndexBateadorCasa { get; set; }
        public int? IndexBateadorVisita { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (EquipoCasaId != 0 && EquipoCasaId == EquipoVisitaId)
            {
                yield return new ValidationResult(
                    "El equipo visitante debe ser diferente al equipo de casa.",
                    new[] { nameof(EquipoVisitaId) });
            }
        }
    }
}
