using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace Scoreboard.Web.Hubs
{
    /// <summary>
    /// Hub de SignalR para actualizacion en tiempo real del marcador.
    ///
    /// Se conecta con:
    /// - marcador.js: para unirse a grupos y recibir cambios.
    /// - MarcadorService: que notifica cuando cambia un partido.
    ///
    /// Flujo simple:
    /// 1. El navegador se une al grupo de un partido.
    /// 2. El servicio envia eventos al grupo cuando cambia el marcador.
    /// 3. JavaScript refresca la pantalla.
    ///
    /// Cuidado:
    /// Cambiar nombres de metodos o grupos requiere actualizar marcador.js.
    /// </summary>
    public class MarcadorHub : Hub
    {
        // Métodos existentes para compatibilidad
        public async Task JoinGroup(int partidoId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"partido-{partidoId}");
        }

        public async Task LeaveGroup(int partidoId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"partido-{partidoId}");
        }

        // Nuevos métodos solicitados (aceptan el nombre del grupo directamente)
        public async Task JoinPartido(string partidoGroup) => await Groups.AddToGroupAsync(Context.ConnectionId, partidoGroup);
        public async Task LeavePartido(string partidoGroup) => await Groups.RemoveFromGroupAsync(Context.ConnectionId, partidoGroup);

        // Verificación mínima: eco de vida del hub
        public async Task Ping()
        {
            await Clients.Caller.SendAsync("Pong");
        }
    }
}
