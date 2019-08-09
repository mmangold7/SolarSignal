using System.Collections.Generic;
using System.Threading.Tasks;
using SolarSignal.SolarModels;

namespace SolarSignal.Hubs
{
    public interface ISolarHub
    {
        Task GameState(List<Body> bodies);
        Task Message(string user, string message);
        Task Left(int playerId);
        Task Right(int playerId);
        Task Up(int playerId);
        Task Down(int playerId);
    }
}