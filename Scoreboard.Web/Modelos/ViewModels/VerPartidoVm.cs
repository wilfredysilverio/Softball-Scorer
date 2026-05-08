using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Scoreboard.Web.Modelos;

namespace Scoreboard.Web.Modelos.ViewModels
{
    /// <summary>
    /// ViewModel principal de la pantalla de anotacion de partido.
    ///
    /// Se conecta con:
    /// - PartidosController.VerPartido: que lo llena.
    /// - Views/Partidos/VerPartido.cshtml: que lo muestra.
    /// - RegistrarTurnoVm y RegistrarEventoCorredorVm: formularios internos.
    ///
    /// Flujo simple:
    /// 1. Agrupa partido, lineup, bateador esperado e historial.
    /// 2. Permite renderizar la pantalla del anotador.
    /// 3. Indica si el turno esta bloqueado o disponible.
    ///
    /// Cuidado:
    /// Si se cambia una propiedad, revisar la vista VerPartido.cshtml.
    /// </summary>
    public class VerPartidoVm
    {
        [Required]
        public required Partido Partido { get; set; }

        public RegistrarTurnoVm Turno { get; set; } = new();

        public RegistrarEventoCorredorVm Evento { get; set; } = new();

        public IReadOnlyList<Jugador> Lineup { get; set; } = Array.Empty<Jugador>();

        public Jugador? BateadorEsperado { get; set; }

        public IReadOnlyList<PlayLog> UltimasJugadas { get; set; } = Array.Empty<PlayLog>();

        public int MaxInnings { get; set; } = 9;

        public string? MotivoBloqueoTurno { get; set; }

        public int Outs { get; set; }
    }

    /// <summary>
    /// Datos que envia el formulario para registrar un turno al bate.
    /// </summary>
    public class RegistrarTurnoVm
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Partido inválido.")]
        public int PartidoId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "El bateador esperado es inválido.")]
        public int? JugadorId { get; set; }

        [Required]
        public ResultadoTurno Resultado { get; set; } = ResultadoTurno.Sencillo;

        public bool ConfirmarFueraTurno { get; set; }

        public EventoCorredor? EventoCorredor { get; set; }

        public BaseCorredor? BaseEvento { get; set; }
    }

    /// <summary>
    /// Datos que envia el formulario para registrar un evento de corredor.
    /// </summary>
    public class RegistrarEventoCorredorVm
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int PartidoId { get; set; }

        [Required]
        public EventoCorredor Evento { get; set; }

        [Required]
        public BaseCorredor Base { get; set; }
    }
}
