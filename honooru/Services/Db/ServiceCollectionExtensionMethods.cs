using Microsoft.Extensions.DependencyInjection;

namespace honooru.Services.Db {

    public static class ServiceCollectionExtensionMethods {

        public static void AddDatabasesServices(this IServiceCollection services) {
            services.AddSingleton<ExpEventDbStore>();
            services.AddSingleton<AppAccountDbStore>();
            services.AddSingleton<AppAccountPermissionDbStore>();
            services.AddSingleton<AppMetadataDbStore>();
            services.AddSingleton<AppAccountGroupMembershipDbStore>();

            services.AddSingleton<PostDb>();
            services.AddSingleton<MediaAssetDb>();
            services.AddSingleton<TagDb>();
            services.AddSingleton<TagTypeDb>();
            services.AddSingleton<PostTagDb>();
        }

    }

}