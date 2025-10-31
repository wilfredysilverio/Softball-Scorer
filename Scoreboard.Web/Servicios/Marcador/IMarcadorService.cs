using System.Threading.Tasks;
using Scoreboard.Web.Modelos;

namespace Scoreboard.Web.Servicios.Marcador
{
    public interface IMarcadorService
    {
        Task IniciarPartidoAsync(int partidoId);
        Task SuspenderPartidoAsync(int partidoId);
        Task ReanudarPartidoAsync(int partidoId);
        Task FinalizarPartidoAsync(int partidoId);
        Task<Partido> RegistrarTurnoAsync(int partidoId, int jugadorId, ResultadoTurno resultado);
        Task<Partido> DeshacerUltimaJugadaAsync(int partidoId);
        Task<Partido> RehacerUltimaJugadaAsync(int partidoId);
        Task<Scoreboard.Web.Dtos.MarcadorDto> ObtenerMarcadorAsync(int partidoId);
    }
}
