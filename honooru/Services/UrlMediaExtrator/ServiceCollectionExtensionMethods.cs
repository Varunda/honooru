using Microsoft.Extensions.DependencyInjection;

namespace honooru.Services.UrlMediaExtrator {

    public static class ServiceCollectionExtensionMethods {

        public static void AddUrlExtractors(this IServiceCollection services) {
            services.AddSingleton<UrlMediaExtractorHandler>();

            services.AddScoped<StaticFileExtractor>();
            services.AddScoped<YoutubeExtractor>();
        }
        
    }
}
