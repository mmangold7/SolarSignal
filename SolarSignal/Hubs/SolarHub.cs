using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using SolarSignal.SolarModels;

namespace SolarSignal.Hubs
{
    public static class UserHandler
    {
        public static HashSet<string> ConnectedIds = new HashSet<string>();
    }

    public class SolarHub : Hub<ISolarHub>
    {
        #region ///  Methods  ///

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            UserHandler.ConnectedIds.Remove(Context.ConnectionId);
            Globals.Simulator.DestroyPlayerWithId(Context.ConnectionId);
            if (UserHandler.ConnectedIds.Count == 0 && Globals.Simulator != null)
            {
                Globals.Simulator = null;
            }
            await base.OnDisconnectedAsync(exception);
        }

        public override async Task OnConnectedAsync()
        {
            UserHandler.ConnectedIds.Add(Context.ConnectionId);
            Globals.Simulator.CreatePlayerWithId(Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        public string GetConnectionId()
        {
            return Context.ConnectionId;
        }

        public async Task GameState(List<Body> bodies)
        {
            await Clients.All.GameState(bodies);
        }

        public async Task Message(string user, string message)
        {
            await Clients.All.Message(user, message);
        }

        public async Task Left()
        {
            Globals.Simulator.Players.Single(p => p.Id == Context.ConnectionId).LeftPressed = true;
        }

        public async Task Right()
        {
            Globals.Simulator.Players.Single(p => p.Id == Context.ConnectionId).RightPressed = true;
        }

        public async Task Up()
        {
            Globals.Simulator.Players.Single(p => p.Id == Context.ConnectionId).UpPressed = true;
        }

        public async Task Down()
        {
            Globals.Simulator.Players.Single(p => p.Id == Context.ConnectionId).DownPressed = true;
        }

        public async Task Shoot()
        {
            //todo:create a bullet or missile or whatever derived body and have them shoot out from the current player
            throw new NotImplementedException();
        }

        public async Task Pause()
        {
            Globals.Simulator.Pause();
        }

        public async Task Resume()
        {
            Globals.Simulator.Resume();
        }

        public async Task ToggleCalculateFuturePaths()
        {
            Globals.Simulator.ShouldCalculateFuturePaths = !Globals.Simulator.ShouldCalculateFuturePaths;
        }

        public async Task IncreaseFuturesCalculations()
        {
            Globals.Simulator.Players.Single(p => p.Id == Context.ConnectionId).FuturesIncremented = true;
            Globals.Simulator.IncreaseFuturesCalculations();
        }

        public async Task DecreaseFuturesCalculations()
        {
            Globals.Simulator.Players.Single(p => p.Id == Context.ConnectionId).FuturesDecremented = true;
            Globals.Simulator.DecreaseFuturesCalculations();
        }

        #endregion
    }
}