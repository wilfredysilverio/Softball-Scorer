using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Scoreboard.Web.Modelos;

namespace Scoreboard.Web.Modelos.ViewModels
{
    public class VerPartidoVm
    {
        [Required]
        public required Partido Partido { get; set; }

        public RegistrarTurnoVm Turno { get; set; } = new();

        public RegistrarEventoCorredorVm Evento { get; set; } = new();

        public IReadOnlyList<Jugador> Lineup { get; set; } = Array.Empty<Jugador>();

        public Jugador? BateadorEsperado { get; set; }

        public int MaxInnings { get; set; } = 9;

        public string? MotivoBloqueoTurno { get; set; }

        public int Outs { get; set; }
    }

    public class RegistrarTurnoVm
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Partido inválido.")]
        public int PartidoId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "El bateador esperado es inválido.")]
        public int? JugadorId { get; set; }

        [Required]
        public ResultadoTurno Resultado { get; set; } = ResultadoTurno.Sencillo;

        public EventoCorredor? EventoCorredor { get; set; }

        public BaseCorredor? BaseEvento { get; set; }
    }

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
