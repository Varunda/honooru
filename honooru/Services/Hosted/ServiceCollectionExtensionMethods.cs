using honooru.Services.Hosted.QueueProcessor;
using honooru.Services.Hosted.Startup;
using Microsoft.Extensions.DependencyInjection;

namespace honooru.Services.Hosted {

    public static class ServiceCollectionExtensionMethods {

        public static void AddAppStartupServices(this IServiceCollection services) {
            services.AddHostedService<StorageOptionCheckStartupService>();
            services.AddHostedService<FFmpegConfigureStartupService>();
        }

        public static void AddAppHostedServices(this IServiceCollection services) {
            services.AddSingleton<ExampleBackgroundService>();

            services.AddHostedService<HostedUploadStepProgressBroadcast>();
            services.AddHostedService<HealthServiceChecker>();
        }

        public static void AddAppQueueProcessorServices(this IServiceCollection services) {
            services.AddHostedService<ThumbnailCreationQueueProcessor>();
            services.AddHostedService<UploadStepsQueueProcessor>();
            services.AddHostedService<TagInfoUpdateQueueProcessor>();
        }

    }
}
