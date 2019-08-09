using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SolarSignal.Models;
using SolarSignal.SolarModels;

namespace SolarSignal.Controllers
{
    public class HomeController : Controller
    {
        private readonly IServiceProvider _serviceProvider;

        public HomeController(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IActionResult Index()
        {
            StartSimulationIfStopped();

            var playerId = Globals.Simulator.CreatePlayerAndReturnId();

            return View(playerId);
        }

        private void StartSimulationIfStopped()
        {
            var simulator = Globals.Simulator;
            if (simulator != null) return; //not stopped

            simulator = new Simulator(_serviceProvider);

            //make the sun
            var sun = new Body
            {
                Name = "sun",
                Mass = 330000,
                Radius = 30,
                XPosition = 0,
                YPosition = 0,
                XVelocity = 0,
                YVelocity = 0,
                Color = "Yellow"
            };
            simulator.Bodies.Add(sun);

            //make the earth
            var earth = simulator.CreateCircularOrbiterOf(sun, 300, 3000, 3, "blue", "earth");

            //make the moon
            var moon = simulator.CreateCircularOrbiterOf(earth, 15, .1, 1, "white", "moon");

            //simulator.Bodies.Remove(sun);

            //start it up
            Task.Run(() => simulator.Simulate());

            Globals.Simulator = simulator;
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel {RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier});
        }
    }
}