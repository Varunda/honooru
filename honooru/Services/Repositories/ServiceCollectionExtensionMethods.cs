using System;
using Microsoft.Extensions.DependencyInjection;
using honooru.Models.Db;
using honooru.Models.Queues;
using honooru.Services.Db.Implementations;
using honooru.Services.Hosted;

namespace honooru.Services.Repositories {

    public static class ServiceCollectionExtensionMethods {

        public static void AddAppRepositoryServices(this IServiceCollection services) {
            services.AddSingleton<AppPermissionRepository>();
            services.AddSingleton<AppMetadataRepository>();
            services.AddSingleton<AppGroupRepository>();
            services.AddSingleton<AppAccountGroupMembershipRepository>();

            services.AddSingleton<PostRepository>();
            services.AddSingleton<SearchQueryRepository>();
            services.AddSingleton<TagRepository>();
            services.AddSingleton<PostTagRepository>();
            services.AddSingleton<TagTypeRepository>();
            services.AddSingleton<TagInfoRepository>();
            services.AddSingleton<MediaAssetRepository>();
            services.AddSingleton<TagAliasRepository>();
            services.AddSingleton<TagImplicationRepository>();
        }

    }

}