using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace Scoreboard.Web.Hubs
{
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
