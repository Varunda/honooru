using honooru.Code.ExtensionMethods;
using honooru.Models.Internal;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading;
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

        public async Task<ulong> Insert(AppAccountGroupMembership membership) {
            if (membership.AccountID == 0) {
                throw new ArgumentException($"account id cannot be 0");
            }
            if (membership.GroupID == 0) {
                throw new ArgumentException($"group id cannot be 0");
            }

            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                INSERT INTO app_account_group_membership (
                    account_id, group_id, timestamp, granted_by_account_id
                ) VALUES (
                    @AccountID, @GroupID, @Timestamp, @GrantedBy
                ) RETURNING id;
            ");

            cmd.AddParameter("AccountID", membership.AccountID);
            cmd.AddParameter("GroupID", membership.GroupID);
            cmd.AddParameter("Timestamp", membership.Timestamp);
            cmd.AddParameter("GrantedBy", membership.GrantedByAccountID);
            await cmd.PrepareAsync();

            ulong id = await cmd.ExecuteUInt64(CancellationToken.None);
            await conn.CloseAsync();

            return id;
        }

        public async Task Delete(AppAccountGroupMembership membership) {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                DELETE FROM 
                    app_account_group_membership
                WHERE
                    group_id = @GroupID
                    AND account_id = @AccountID
            ");

            cmd.AddParameter("AccountID", membership.AccountID);
            cmd.AddParameter("GroupID", membership.GroupID);
            await cmd.PrepareAsync();

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
