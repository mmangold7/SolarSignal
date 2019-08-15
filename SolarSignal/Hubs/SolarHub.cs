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
            if (UserHandler.ConnectedIds.Count == 0 && Globals.Simulator != null) Globals.Simulator = null;
            await base.OnDisconnectedAsync(exception);
        }

        public override async Task OnConnectedAsync()
        {
            UserHandler.ConnectedIds.Add(Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        public async Task CreatePlayerWithId(string connectionId, string rgbColor)
        {
            if(UserHandler.ConnectedIds.Contains(connectionId) && Globals.Simulator.Players.All(p => p.Id != connectionId))
                Globals.Simulator.CreatePlayerWithId(connectionId, rgbColor);
        }

        public string GetConnectionId()
        {
            return Context.ConnectionId;
        }

        public async Task GameState(List<Body> bodies, bool alreadyCalculatedPaths)
        {
            await Clients.All.GameState(bodies, alreadyCalculatedPaths);
        }

        public async Task Message(string user, string message)
        {
            await Clients.All.Message(user, message);
        }

        public async Task Input(Input input)
        {
            Globals.Simulator.Players.Single(p => p.Id == Context.ConnectionId).Input = input;
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