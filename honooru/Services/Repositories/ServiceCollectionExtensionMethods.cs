using System;
using Microsoft.Extensions.DependencyInjection;
using honooru.Models.Db;
using honooru.Models.Queues;
using honooru.Services.Db.Implementations;
using honooru.Services.Hosted;

namespace honooru.Services.Repositories {

    public static class ServiceCollectionExtensionMethods {

        public static void AddAppRepositoryServices(this IServiceCollection services) {
            services.AddSingleton<AppAccountPermissionRepository>();
            services.AddSingleton<AppMetadataRepository>();
            services.AddSingleton<SearchQueryRepository>();
            services.AddSingleton<TagRepository>();
            services.AddSingleton<TagTypeRepository>();
            services.AddSingleton<TagInfoRepository>();
            services.AddSingleton<MediaAssetRepository>();
        }

    }

}