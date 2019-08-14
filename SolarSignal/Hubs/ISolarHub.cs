using System.Collections.Generic;
using System.Threading.Tasks;
using SolarSignal.SolarModels;

namespace SolarSignal.Hubs
{
    public interface ISolarHub
    {
        Task GameState(List<Body> bodies, bool alreadyCalculatedPaths);
        Task Message(string user, string message);
        Task Input(Input keyMap);
        Task Shoot();
        Task Pause();
        Task Resume();
        Task ToggleCalculateFuturePaths();
        Task IncreaseFuturesCalculations();
        Task DecreaseFuturesCalculations();
    }
}