using System;
using System.Collections.Generic;
using System.Linq;
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

        public async Task Left(int playerId)
        {
            Globals.Simulator.Players.Single(p => p.Id == playerId).LeftPressed = true;
        }

        public async Task Right(int playerId)
        {
            Globals.Simulator.Players.Single(p => p.Id == playerId).RightPressed = true;
        }

        public async Task Up(int playerId)
        {
            Globals.Simulator.Players.Single(p => p.Id == playerId).UpPressed = true;
        }

        public async Task Down(int playerId)
        {
            Globals.Simulator.Players.Single(p => p.Id == playerId).DownPressed = true;
        }

        #endregion
    }
}