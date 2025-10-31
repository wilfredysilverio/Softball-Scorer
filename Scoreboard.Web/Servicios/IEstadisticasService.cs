using System.Threading.Tasks;
using Scoreboard.Web.Modelos.ViewModels;

namespace Scoreboard.Web.Servicios
{
    public interface IEstadisticasService
    {
        // Devuelve null si el jugador no existe
        Task<EstadisticasJugadorVm?> ObtenerEstadisticasJugadorAsync(int jugadorId);

        Task<System.Collections.Generic.List<Scoreboard.Web.Modelos.ViewModels.LineaTemporadaVm>> ObtenerLineasTemporadaAsync(int jugadorId);

        // Estadísticas agregadas por equipo (devuelve null si el equipo no existe)
        Task<Scoreboard.Web.Modelos.ViewModels.EstadisticasEquipoVm?> ObtenerEstadisticasEquipoAsync(int equipoId);
    }
}
