using Microsoft.Extensions.DependencyInjection;

namespace honooru.Services.Db {

    public static class ServiceCollectionExtensionMethods {

        public static void AddDatabasesServices(this IServiceCollection services) {
            services.AddSingleton<AppMetadataDbStore>();
            services.AddSingleton<AppAccountDbStore>();
            services.AddSingleton<AppGroupDbStore>();
            services.AddSingleton<AppGroupPermissionDbStore>();
            services.AddSingleton<AppAccountGroupMembershipDbStore>();
            services.AddSingleton<UserSettingDb>();

            services.AddSingleton<PostDb>();
            services.AddSingleton<MediaAssetDb>();
            services.AddSingleton<TagDb>();
            services.AddSingleton<TagTypeDb>();
            services.AddSingleton<PostTagDb>();
            services.AddSingleton<TagInfoDb>();
            services.AddSingleton<TagAliasDb>();
            services.AddSingleton<TagImplicationDb>();
        }

    }

}