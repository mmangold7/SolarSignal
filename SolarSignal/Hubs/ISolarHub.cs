using System.Collections.Generic;
using System.Threading.Tasks;
using SolarSignal.SolarModels;

namespace SolarSignal.Hubs
{
    public interface ISolarHub
    {
        #region ///  Methods  ///

        Task DecreaseFuturesCalculations();
        Task GameState(List<Body> bodies, bool alreadyCalculatedPaths);
        Task IncreaseFuturesCalculations();
        Task Input(Input keyMap);
        Task Message(string user, string message);
        Task Pause();
        Task Resume();
        Task Shoot();
        Task ToggleCalculateFuturePaths(bool currentShouldCalculateFuturePaths);
        Task TogglePaused();

        #endregion
    }
}