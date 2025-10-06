using System.Threading.Tasks;
using Scoreboard.Web.Modelos.ViewModels;

namespace Scoreboard.Web.Servicios
{
    public interface IEstadisticasService
    {
        // Devuelve null si el jugador no existe
        Task<EstadisticasJugadorVm?> ObtenerEstadisticasJugadorAsync(int jugadorId);

        Task<System.Collections.Generic.List<Scoreboard.Web.Modelos.ViewModels.LineaTemporadaVm>> ObtenerLineasTemporadaAsync(int jugadorId);
    }
}
