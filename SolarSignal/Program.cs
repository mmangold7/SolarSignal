using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace SolarSignal
{
    public class Program
    {
        #region ///  Methods  ///

        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        {
            var hostUrl = "https://0.0.0.0:5001";

            return WebHost.CreateDefaultBuilder(args).UseUrls(hostUrl).UseStartup<Startup>();
        }

        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        #endregion
    }
}