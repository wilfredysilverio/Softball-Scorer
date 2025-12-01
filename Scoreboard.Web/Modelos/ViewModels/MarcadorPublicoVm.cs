using System.Collections.Generic;
using Scoreboard.Web.Modelos;

namespace Scoreboard.Web.Modelos.ViewModels
{
    public class MarcadorPublicoVm
    {
        public Partido Partido { get; set; } = new();
        public List<Entrada> Entradas { get; set; } = new();
        public List<PlayLog> UltimasJugadas { get; set; } = new();
        public int Outs { get; set; }
    }
}
