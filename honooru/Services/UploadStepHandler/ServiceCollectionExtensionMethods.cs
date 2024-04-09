using honooru.Models.App.MediaUploadStep;
using Microsoft.Extensions.DependencyInjection;

namespace honooru.Services.UploadStepHandler {

    public static class ServiceCollectionExtensionMethods {

        public static void AddUploadStepHandlers(this IServiceCollection services) {
            services.AddSingleton<UploadStepsProcessor>();
            services.AddSingleton<UploadStepProgressRepository>();

            services.AddScoped<ReencodeUploadStep.Worker>();
            services.AddScoped<MoveUploadStep.Worker>();
            services.AddScoped<ExtractStep.Worker>();
            services.AddScoped<GenerateImageHashUploadStep.Worker>();
        }

    }
}
