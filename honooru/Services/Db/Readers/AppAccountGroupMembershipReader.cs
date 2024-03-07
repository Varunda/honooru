using honooru.Code.ExtensionMethods;
using honooru.Models.Internal;
using Npgsql;
using System.Data;

namespace honooru.Services.Db.Readers {

    public class AppAccountGroupMembershipReader : IDataReader<AppAccountGroupMembership> {

        public override AppAccountGroupMembership? ReadEntry(NpgsqlDataReader reader) {
            AppAccountGroupMembership member = new();

            member.ID = reader.GetUInt64("id");
            member.AccountID = reader.GetUInt64("account_id");
            member.GroupID = reader.GetUInt64("group_id");
            member.Timestamp = reader.GetDateTime("timestamp");
            member.GrantedByAccountID = reader.GetUInt64("granted_by_account_id");

            return member;
        }

    }
}
