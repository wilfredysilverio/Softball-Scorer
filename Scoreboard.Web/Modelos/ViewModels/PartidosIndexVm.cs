using System.Collections.Generic;
using System.Linq;

namespace Scoreboard.Web.Modelos.ViewModels
{
    public class PartidosIndexVm
    {
        public IEnumerable<Partido> Programados { get; set; } = Enumerable.Empty<Partido>();
        public IEnumerable<Partido> EnCurso { get; set; } = Enumerable.Empty<Partido>();
        public IEnumerable<Partido> Finalizados { get; set; } = Enumerable.Empty<Partido>();
    }
}
