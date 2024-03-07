using Microsoft.Extensions.DependencyInjection;

namespace honooru.Services.Parsing {

    public static class ServiceCollectionExtensionMethods {

        public static void AddSearchServices(this IServiceCollection services) {
            services.AddTransient<SearchTokenizer>();
            services.AddTransient<AstBuilder>();
            services.AddTransient<SearchQueryParser>();
        }

    }
}
