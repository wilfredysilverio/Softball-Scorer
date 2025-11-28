using System.Collections.Generic;

namespace Scoreboard.Web.Modelos.ViewModels
{
    public class PartidosIndexViewModel
    {
        public List<Partido> PartidosEnCurso { get; set; }
        public List<Partido> PartidosProgramados { get; set; }
        public List<Partido> PartidosFinalizados { get; set; }

        public PartidosIndexViewModel()
        {
            PartidosEnCurso = new List<Partido>();
            PartidosProgramados = new List<Partido>();
            PartidosFinalizados = new List<Partido>();
        }
    }
}
