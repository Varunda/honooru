using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using snail.Models;
using snail.Services;

namespace snail {

    public class Program {

        public static void Main(string[] args) {
            Console.WriteLine("snail started");

            IHostBuilder builder = Host.CreateDefaultBuilder();

            builder.ConfigureAppConfiguration((config) => {
                config.AddJsonFile("appsettings.json");
                config.AddJsonFile("secrets.json");
            });

            builder.ConfigureServices((context, services) => {
                services.Configure<snail.Models.HostOptions>(context.Configuration.GetSection("Host"));

                services.AddSingleton<honooru_common.Services.PathEnvironmentService>();
                services.AddSingleton<YoutubeExtractor>();
                services.AddSingleton<JobApi>();

                services.AddHostedService<App>();
            });

            using IHost host = builder.Build();
            host.Run();
        }

    }
}
