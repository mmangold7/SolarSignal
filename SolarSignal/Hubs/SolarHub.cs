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
        #region ///  Fields  ///

        public static HashSet<string> ConnectedIds = new HashSet<string>();

        #endregion
    }

    public class SolarHub : Hub<ISolarHub>
    {
        #region ///  Fields  ///

        private Simulator _simulator = Globals.Simulator;

        #endregion

        #region ///  Methods  ///

        public async Task CreatePlayerWithId(string connectionId, string rgbColor)
        {
            if (UserHandler.ConnectedIds.Contains(connectionId) && _simulator.Players.All(p => p.Id != connectionId))
            {
                _simulator.CreatePlayerWithId(connectionId, rgbColor);
            }

            //optional unpause when first player joins, if paused by default
            //if (_simulator.Players.Count == 1 && _simulator.IsPaused)
            //{
            //    _simulator.Resume();
            //}
        }

        public async Task DecreaseFuturesCalculations()
        {
            _simulator.Players.Single(p => p.Id == Context.ConnectionId).FuturesDecremented = true;
            _simulator.DecreaseFuturesCalculations();
        }

        public async Task GameState(List<Body> bodies, bool alreadyCalculatedPaths)
        {
            await Clients.All.GameState(bodies, alreadyCalculatedPaths);
        }

        public string GetConnectionId() => Context.ConnectionId;

        public async Task IncreaseFuturesCalculations()
        {
            _simulator.Players.Single(p => p.Id == Context.ConnectionId).FuturesIncremented = true;
            _simulator.IncreaseFuturesCalculations();
        }

        public async Task Input(Input input)
        {
            _simulator.Players.Single(p => p.Id == Context.ConnectionId).Input = input;
        }

        public async Task Message(string user, string message)
        {
            await Clients.All.Message(user, message);
        }

        public override async Task OnConnectedAsync()
        {
            UserHandler.ConnectedIds.Add(Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            UserHandler.ConnectedIds.Remove(Context.ConnectionId);
            _simulator.DestroyPlayerWithId(Context.ConnectionId);
            if (UserHandler.ConnectedIds.Count == 0 && _simulator != null)
            {
                _simulator = null;
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task Pause() => _simulator.Pause();

        public async Task Resume() => _simulator.Resume();

        public async Task ToggleCalculateFuturePaths(bool currentShouldCalculateFuturePaths)
        {
            var shouldCalculateFuturePaths = !_simulator.ShouldCalculateFuturePaths;
            _simulator.ShouldCalculateFuturePaths = shouldCalculateFuturePaths;
            await Clients.All.ToggleCalculateFuturePaths(shouldCalculateFuturePaths);
        }

        public async Task TogglePaused()
        {
            if (_simulator.IsPaused)
            {
                _simulator.Resume();
            }
            else
            {
                _simulator.Pause();
            }
        }

        #endregion
    }
}