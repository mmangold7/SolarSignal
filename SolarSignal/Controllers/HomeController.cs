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

            simulator.SetUpSol();

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