using System;
using System.Diagnostics;
using System.Numerics;
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
            return View();
        }

        private void StartSimulationIfStopped()
        {
            if (Globals.Simulator != null) return; //not stopped

            //start it up
            Globals.Simulator = GetSolSimulator();
            Task.Run(() => Globals.Simulator.Simulate());
        }

        public Simulator GetSolSimulator()
        {
            var simulator = new Simulator(_serviceProvider);

            //make the sun
            var sun = new Body
            {
                Name = "sun",
                Mass = 330000,
                Radius = 30,
                Position = new Vector2(0, 0),
                Velocity = new Vector2(0, 0),
                Color = "Yellow"
            };
            simulator.Bodies.Add(sun);

            //make the earth
            var earth = simulator.CreateCircularOrbiterOf(sun, 300, 10000, 3, "blue", "earth");

            //make the moon
            var moon = simulator.CreateCircularOrbiterOf(earth, 15, .1, 2, "white", "moon");

            //simulator.Bodies.Remove(sun);

            return simulator;
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