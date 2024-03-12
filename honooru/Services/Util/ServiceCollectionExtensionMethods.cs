using Microsoft.Extensions.DependencyInjection;

namespace honooru.Services.Util {
    public static class ServiceCollectionExtensionMethods {

        public static void AddUtilServices(this IServiceCollection services) {
            services.AddTransient<FileExtensionService>();
        }

    }
}
