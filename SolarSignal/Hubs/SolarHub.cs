using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace SolarSignal.Hubs
{
    public class SolarHub : Hub
    {
        public async Task SendMessage(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }
    }
}