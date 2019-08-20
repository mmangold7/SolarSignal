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
        #region ///  Fields  ///

        private readonly IServiceProvider _serviceProvider;

        #endregion

        #region ///  Constructors  ///

        public HomeController(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        #endregion

        #region ///  Methods  ///

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

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() =>
            View(new ErrorViewModel
                 {
                     RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
                 });

        public Simulator GetSolSimulator()
        {
            var simulator = new Simulator(_serviceProvider);

            //make the sun
            var sun = new Body
                      {
                          Name = "sun",
                          Mass = 400000,
                          Radius = 40,
                          Position = new Vector2(0, 0),
                          Velocity = new Vector2(0, 0),
                          Color = "Yellow"
                      };
            simulator.Bodies.Add(sun);

            //make the earth
            var earth = simulator.CreateCircularOrbiterOf(sun, 300, 1000, 6, "blue", "earth");

            //make the moon
            var moon = simulator.CreateCircularOrbiterOf(earth, 15, .1f, 1, "white", "moon");

            //make some other planets
            var mercury = simulator.CreateCircularOrbiterOf(sun, 100, 100, 3, "orange", "mercury");
            var venus = simulator.CreateCircularOrbiterOf(sun, 200, 1000, 5, "yellow", "venus");
            var mars = simulator.CreateCircularOrbiterOf(sun, 400, 500, 4, "red", "mars");

            //simulator.Bodies.Remove(sun);

            return simulator;
        }

        public IActionResult Index()
        {
            StartSimulationIfStopped();
            return View();
        }

        public IActionResult Privacy() => View();

        private void StartSimulationIfStopped()
        {
            if (Globals.Simulator != null)
            {
                return; //not stopped
            }

            //start it up
            Globals.Simulator = GetSolSimulator();
            Task.Run(() => Globals.Simulator.Simulate());
        }

        #endregion
    }
}