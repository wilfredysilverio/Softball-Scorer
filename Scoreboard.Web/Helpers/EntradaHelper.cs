using System.Collections.Generic;
using System.Linq;
using Scoreboard.Web.Modelos;

namespace Scoreboard.Web.Helpers
{
    public static class EntradaHelper
    {
        public static List<Entrada> NormalizarEntradas(IEnumerable<Entrada>? entradas)
        {
            if (entradas == null)
            {
                return new List<Entrada>();
            }

            return entradas
                .GroupBy(e => Math.Max(1, e.NumeroInning))
                .Select(g =>
                {
                    var first = g.First();
                    return new Entrada
                    {
                        Id = first.Id,
                        PartidoId = first.PartidoId,
                        NumeroInning = g.Key,
                        CarrerasCasa = g.Sum(x => x.CarrerasCasa),
                        CarrerasVisita = g.Sum(x => x.CarrerasVisita),
                        HitsCasa = g.Sum(x => x.HitsCasa),
                        HitsVisita = g.Sum(x => x.HitsVisita),
                        ErroresCasa = g.Sum(x => x.ErroresCasa),
                        ErroresVisita = g.Sum(x => x.ErroresVisita)
                    };
                })
                .OrderBy(e => e.NumeroInning)
                .ToList();
        }
    }
}
