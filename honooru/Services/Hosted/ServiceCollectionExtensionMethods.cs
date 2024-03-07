using honooru.Services.Hosted.Startup;
using Microsoft.Extensions.DependencyInjection;

namespace honooru.Services.Hosted {

    public static class ServiceCollectionExtensionMethods {

        public static void AddAppStartupServices(this IServiceCollection services) {
            services.AddHostedService<StorageOptionCheckStartupService>();
        }

        public static void AddAppHostedServices(this IServiceCollection services) {
            services.AddSingleton<ExampleBackgroundService>();
        }

    }
}
