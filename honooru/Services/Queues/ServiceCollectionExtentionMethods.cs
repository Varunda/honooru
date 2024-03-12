using honooru.Models.Queues;
using Microsoft.Extensions.DependencyInjection;

namespace honooru.Services.Queues {

    public static class ServiceCollectionExtentionMethods {

        /// <summary>
        ///     And the various queues the app uses
        /// </summary>
        /// <param name="services">Extension instance</param>
        public static void AddAppQueueServices(this IServiceCollection services) {
            services.AddSingleton<DiscordMessageQueue>();

            services.AddSingleton<BaseQueue<ThumbnailCreationQueueEntry>, ThumbnailCreationQueue>();
            services.AddSingleton<BaseQueue<MediaAssetReencodeQueueEntry>, MediaAssetReencodeQueue>();
        }

    }
}
