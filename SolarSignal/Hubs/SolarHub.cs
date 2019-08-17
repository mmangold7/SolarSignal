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
        private Simulator _simulator = Globals.Simulator;

        #region ///  Methods  ///

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            UserHandler.ConnectedIds.Remove(Context.ConnectionId);
            _simulator.DestroyPlayerWithId(Context.ConnectionId);
            if (UserHandler.ConnectedIds.Count == 0 && _simulator != null) _simulator = null;
            await base.OnDisconnectedAsync(exception);
        }

        public override async Task OnConnectedAsync()
        {
            UserHandler.ConnectedIds.Add(Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        public async Task CreatePlayerWithId(string connectionId, string rgbColor)
        {
            if (UserHandler.ConnectedIds.Contains(connectionId) &&
                _simulator.Players.All(p => p.Id != connectionId))
                _simulator.CreatePlayerWithId(connectionId, rgbColor);
            if (_simulator.Players.Count == 1 && _simulator.IsPaused)
            {
                _simulator.Resume();
            }
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
            _simulator.Players.Single(p => p.Id == Context.ConnectionId).Input = input;
        }

        public async Task Shoot()
        {
            //todo:create a bullet or missile or whatever derived body and have them shoot out from the current player
            throw new NotImplementedException();
        }

        public async Task Pause()
        {
            _simulator.Pause();
        }

        public async Task Resume()
        {
            _simulator.Resume();
        }

        public async Task ToggleCalculateFuturePaths(bool currentShouldCalculateFuturePaths)
        {
            var shouldCalculateFuturePaths = !_simulator.ShouldCalculateFuturePaths;
            _simulator.ShouldCalculateFuturePaths = shouldCalculateFuturePaths;
            await Clients.All.ToggleCalculateFuturePaths(shouldCalculateFuturePaths);
        }

        public async Task TogglePaused()
        {
            if (_simulator.IsPaused)
                _simulator.Resume();
            else
                _simulator.Pause();
        }

        public async Task IncreaseFuturesCalculations()
        {
            _simulator.Players.Single(p => p.Id == Context.ConnectionId).FuturesIncremented = true;
            _simulator.IncreaseFuturesCalculations();
        }

        public async Task DecreaseFuturesCalculations()
        {
            _simulator.Players.Single(p => p.Id == Context.ConnectionId).FuturesDecremented = true;
            _simulator.DecreaseFuturesCalculations();
        }

        #endregion
    }
}