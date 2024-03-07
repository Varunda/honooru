using Microsoft.Extensions.DependencyInjection;

namespace honooru.Code.DiscordInteractions {

    public static class ServiceProviderExtensionMethods {

        /// <summary>
        ///     Add interaction services that generate responses based on inputs
        /// </summary>
        /// <param name="services">extension instance</param>
        public static void AddAppDiscord(this IServiceCollection services) {
            services.AddSingleton<ServerStatusInteractions>();
        }

    }
}
