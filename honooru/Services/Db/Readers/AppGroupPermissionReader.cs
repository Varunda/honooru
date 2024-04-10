using Npgsql;
using System.Data;
using honooru.Models;
using honooru.Code.ExtensionMethods;

namespace honooru.Services.Db.Readers {

    public class AppGroupPermissionReader : IDataReader<AppGroupPermission> {

        public override AppGroupPermission? ReadEntry(NpgsqlDataReader reader) {
            AppGroupPermission perm = new();

            perm.ID = reader.GetUInt64("id");
            perm.GroupID = reader.GetUInt64("group_id");
            perm.Permission = reader.GetString("permission");
            perm.Timestamp = reader.GetDateTime("timestamp");
            perm.GrantedByID = reader.GetUInt64("granted_by_id");

            return perm;
        }

    }
}
