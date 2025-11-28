using System;
using System.Collections.Generic;
using System.Linq;
using Scoreboard.Web.Modelos;

namespace Scoreboard.Web.Modelos.ViewModels
{
    public class PartidoDetalleVm
    {
        public required Partido Partido { get; set; }
        public List<EntradaDetalleVm> Entradas { get; set; } = new();
        public List<JugadaDetalleVm> Jugadas { get; set; } = new();
    }

    public class EntradaDetalleVm
    {
        public int Numero { get; set; }
        public int CarrerasCasa { get; set; }
        public int CarrerasVisita { get; set; }
        public int HitsCasa { get; set; }
        public int HitsVisita { get; set; }
        public int ErroresCasa { get; set; }
        public int ErroresVisita { get; set; }
    }

    public class JugadaDetalleVm
    {
        public int Numero { get; set; }
        public int Entrada { get; set; }
        public string Mitad { get; set; } = string.Empty;
        public DateTime Momento { get; set; }
        public string Bateador { get; set; } = string.Empty;
        public string Resultado { get; set; } = string.Empty;
        public int OutsDespues { get; set; }
        public string Bases { get; set; } = string.Empty;
        public int CarrerasAnotadas { get; set; }
    }
}
