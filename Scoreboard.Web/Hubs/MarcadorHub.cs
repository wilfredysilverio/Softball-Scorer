using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace Scoreboard.Web.Hubs
{
    public class MarcadorHub : Hub
    {
        public async Task JoinGroup(int partidoId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"partido-{partidoId}");
        }

        public async Task LeaveGroup(int partidoId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"partido-{partidoId}");
        }
    }
}
