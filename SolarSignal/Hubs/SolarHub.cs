using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using SolarSignal.SolarModels;

namespace SolarSignal.Hubs
{
    public class SolarHub : Hub<ISolarHub>
    {
        #region ///  Methods  ///

        public async Task GameState(List<Body> bodies)
        {
            await Clients.All.GameState(bodies);
        }

        public async Task Message(string user, string message)
        {
            await Clients.All.Message(user, message);
        }

        #endregion
    }

    public interface ISolarHub
    {
        Task GameState(List<Body> bodies);
        Task Message(string user, string message);
    }
}