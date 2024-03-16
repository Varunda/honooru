using honooru.Code.ExtensionMethods;
using honooru.Models.Internal;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace honooru.Services.Db {

    public class AppAccountGroupMembershipDbStore {

        private readonly ILogger<AppAccountGroupMembershipDbStore> _Logger;
        private readonly IDataReader<AppAccountGroupMembership> _Reader;
        private readonly IDbHelper _DbHelper;

        public AppAccountGroupMembershipDbStore(ILogger<AppAccountGroupMembershipDbStore> logger,
            IDataReader<AppAccountGroupMembership> reader, IDbHelper dbHelper) {

            _Logger = logger;

            _Reader = reader;
            _DbHelper = dbHelper;
        }

        public async Task<AppAccountGroupMembership?> GetByID(ulong memberID) {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                SELECT *
                    FROM app_account_group_membership
                    WHERE id = @ID;
            ");

            cmd.AddParameter("ID", memberID);

            await cmd.PrepareAsync();

            AppAccountGroupMembership? member = await _Reader.ReadSingle(cmd);
            await conn.CloseAsync();

            return member;
        }

        /// <summary>
        ///     get the group memberships a user is a part of
        /// </summary>
        /// <param name="accountID">ID of the account to get the memberships of</param>
        /// <returns>
        ///     
        /// </returns>
        public async Task<List<AppAccountGroupMembership>> GetByAccountID(ulong accountID) {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                SELECT *
                    FROM app_account_group_membership
                    WHERE account_id = @AccountID;
            ");

            cmd.AddParameter("AccountID", accountID);

            await cmd.PrepareAsync();

            List<AppAccountGroupMembership> members = await _Reader.ReadList(cmd);
            await conn.CloseAsync();

            return members;
        }

        /// <summary>
        ///     get the group memberships a user is a part of
        /// </summary>
        /// <param name="groupID">ID of the group to get the memberships of</param>
        /// <returns>
        ///     
        /// </returns>
        public async Task<List<AppAccountGroupMembership>> GetByGroupID(ulong groupID) {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                SELECT *
                    FROM app_account_group_membership
                    WHERE group_id = @GroupID;
            ");

            cmd.AddParameter("GroupID", groupID);

            await cmd.PrepareAsync();

            List<AppAccountGroupMembership> members = await _Reader.ReadList(cmd);
            await conn.CloseAsync();

            return members;
        }

    }
}
