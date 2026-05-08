using System.Threading.Tasks;
using Scoreboard.Web.Modelos;

namespace Scoreboard.Web.Servicios.Marcador
{
    /// <summary>
    /// Contrato de la logica principal del marcador.
    ///
    /// Se conecta con:
    /// - PartidosController: que llama estos metodos desde las pantallas.
    /// - MarcadorService: implementacion real de las reglas.
    /// - MarcadorDto: estado enviado al frontend.
    ///
    /// Flujo simple:
    /// 1. Recibe una accion del partido.
    /// 2. Devuelve el partido actualizado o el marcador actual.
    /// 3. Permite probar la logica sin depender directamente del controlador.
    ///
    /// Cuidado:
    /// Cambiar firmas de metodos obliga a actualizar controladores, tests y servicios.
    /// </summary>
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
        Task<Partido> RegistrarTurnoAsync(int partidoId, int? jugadorConfirmadoId, ResultadoTurno resultado, EventoCorredor eventoCorredor = EventoCorredor.Ninguno, BaseCorredor baseEvento = BaseCorredor.Primera, bool permitirFueraTurno = false);
        Task<Partido> RegistrarEventoCorredorAsync(int partidoId, EventoCorredor eventoCorredor, BaseCorredor baseCorredor);
        Task<Partido> DeshacerUltimaJugadaAsync(int partidoId);
        Task<Partido> RehacerUltimaJugadaAsync(int partidoId);
        Task<Scoreboard.Web.Dtos.MarcadorDto> ObtenerMarcadorAsync(int partidoId);
    }
}
