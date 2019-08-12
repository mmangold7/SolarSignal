using System.Collections.Generic;
using System.Threading.Tasks;
using SolarSignal.SolarModels;

namespace SolarSignal.Hubs
{
    public interface ISolarHub
    {
        Task GameState(List<Body> bodies);
        Task Message(string user, string message);
        Task Left();
        Task Right();
        Task Up();
        Task Down();
        Task Shoot();
        Task Pause();
        Task Resume();
        Task ToggleCalculateFuturePaths();
        Task IncreaseFuturesCalculations();
        Task DecreaseFuturesCalculations();
    }
}