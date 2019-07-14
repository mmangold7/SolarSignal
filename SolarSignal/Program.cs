using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace SolarSignal
{
    public class Program
    {
        #region ///  Methods  ///

        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        {
            return WebHost.CreateDefaultBuilder(args).UseStartup<Startup>();
        }

        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        #endregion
    }
}