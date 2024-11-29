using Microsoft.Extensions.DependencyInjection;
using honooru.Models;
using honooru.Models.Db;
using honooru.Models.Health;
using honooru.Models.App;
using honooru.Models.Internal;
using honooru_common.Models;

namespace honooru.Services.Db.Readers {

    public static class ServiceCollectionExtensionMethods {

        public static void AddAppDatabaseReadersServices(this IServiceCollection services) {
            services.AddSingleton<IDataReader<ExpEvent>, ExpEventReader>();
            services.AddSingleton<IDataReader<AppAccount>, AppAccountReader>();
            services.AddSingleton<IDataReader<AppGroupPermission>, AppGroupPermissionReader>();
            services.AddSingleton<IDataReader<AppGroup>, AppGroupReader>();
            services.AddSingleton<IDataReader<AppAccountGroupMembership>, AppAccountGroupMembershipReader>();
            services.AddSingleton<IDataReader<UserSetting>, UserSettingReader>();
            services.AddSingleton<IDataReader<ApiKey>, ApiKeyReader>();

            services.AddSingleton<IDataReader<Post>, PostReader>();
            services.AddSingleton<IDataReader<MediaAsset>, MediaAssetReader>();
            services.AddSingleton<IDataReader<Tag>, TagReader>();
            services.AddSingleton<IDataReader<TagType>, TagTypeReader>();
            services.AddSingleton<IDataReader<PostTag>, PostTagReader>();
            services.AddSingleton<IDataReader<TagInfo>, TagInfoReader>();
            services.AddSingleton<IDataReader<TagAlias>, TagAliasReader>();
            services.AddSingleton<IDataReader<TagImplication>, TagImplicationReader>();
            services.AddSingleton<IDataReader<PostChild>, PostChildReader>();
            services.AddSingleton<IDataReader<PostPool>, PostPoolReader>();
            services.AddSingleton<IDataReader<PostPoolEntry>, PostPoolEntryReader>();
            services.AddSingleton<IDataReader<ExtractorAuthorMapping>, ExtractorAuthorMappingReader>();
            services.AddSingleton<IDataReader<DistributedJob>, DistributedJobReader>();
        }

    }

}