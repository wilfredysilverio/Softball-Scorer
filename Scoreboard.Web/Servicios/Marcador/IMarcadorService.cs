using System.Threading.Tasks;
using Scoreboard.Web.Modelos;

namespace Scoreboard.Web.Servicios.Marcador
{
    public interface IMarcadorService
    {
        // Inicia un partido: valida estado, establece entrada y outs iniciales
        Task IniciarPartidoAsync(int partidoId);
        // Suspende un partido en curso sin alterar los totales/contadores
        Task SuspenderPartidoAsync(int partidoId);
        // Reanuda un partido suspendido manteniendo los valores actuales
        Task ReanudarPartidoAsync(int partidoId);
        // Finaliza un partido e impide más registros de jugadas
        Task FinalizarPartidoAsync(int partidoId);
        Task<Partido> RegistrarTurnoAsync(int partidoId, int? jugadorConfirmadoId, ResultadoTurno resultado);
        Task<Partido> DeshacerUltimaJugadaAsync(int partidoId);
        Task<Partido> RehacerUltimaJugadaAsync(int partidoId);
        Task<Scoreboard.Web.Dtos.MarcadorDto> ObtenerMarcadorAsync(int partidoId);
    }
}
