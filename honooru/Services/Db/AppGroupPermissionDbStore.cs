using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using honooru.Code.ExtensionMethods;
using honooru.Models;

namespace honooru.Services.Db {

    public class AppGroupPermissionDbStore {

        private readonly ILogger<AppGroupPermissionDbStore> _Logger;
        private readonly IDbHelper _DbHelper;
        private readonly IDataReader<AppGroupPermission> _Reader;

        public AppGroupPermissionDbStore(ILogger<AppGroupPermissionDbStore> logger,
            IDbHelper dbHelper, IDataReader<AppGroupPermission> reader) {

            _Logger = logger;
            _DbHelper = dbHelper;
            _Reader = reader;
        }

        /// <summary>
        ///     Get a single group permission by its ID
        /// </summary>
        /// <param name="ID">ID of the specific permission to get</param>
        public async Task<AppGroupPermission?> GetByID(long ID) {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                SELECT *
                    FROM app_group_permission
                    WHERE id = @ID;
            ");

            cmd.AddParameter("ID", ID);

            AppGroupPermission? perm = await _Reader.ReadSingle(cmd);
            await conn.CloseAsync();

            return perm;
        }

        /// <summary>
        ///     Get the account permissions of a group
        /// </summary>
        /// <param name="groupID">ID of the group</param>
        public async Task<List<AppGroupPermission>> GetByGroupID(ulong groupID) {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                SELECT *
                    FROM app_group_permission
                    WHERE group_id = @GroupID;
            ");

            cmd.AddParameter("GroupID", groupID);

            List<AppGroupPermission> perms = await _Reader.ReadList(cmd);
            await conn.CloseAsync();

            return perms;
        }

        /// <summary>
        ///     Insert a new <see cref="AppGroupPermission"/>
        /// </summary>
        /// <param name="perm">Parameters used to insert</param>
        /// <returns>The ID the row was given in the table</returns>
        /// <exception cref="ArgumentException">If one of the fields in <paramref name="perm"/> was invalid</exception>
        public async Task<ulong> Insert(AppGroupPermission perm) {
            if (string.IsNullOrWhiteSpace(perm.Permission)) {
                throw new ArgumentException($"Passed permission has a {nameof(AppGroupPermission.Permission)} that is null or whitespace");
            }
            if (perm.GrantedByID <= 0) {
                throw new ArgumentException($"Passed permission has a {nameof(AppGroupPermission.GrantedByID)} that is 0 or lower");
            }

            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                INSERT INTO app_group_permission (
                    group_id, permission, timestamp, granted_by_id
                ) VALUES (
                    @GroupID, @Permission, @Timestamp, @GrantedByID 
                ) RETURNING id;
            ");

            cmd.AddParameter("GroupID", perm.GroupID);
            cmd.AddParameter("Permission", perm.Permission);
            cmd.AddParameter("Timestamp", DateTime.UtcNow);
            cmd.AddParameter("GrantedByID", perm.GrantedByID);

            ulong id = await cmd.ExecuteUInt64(CancellationToken.None);

            return id;
        }

        /// <summary>
        ///     Delete a specific <see cref="AppGroupPermission"/> by its ID
        /// </summary>
        /// <param name="ID">ID of the permission to delete</param>
        public async Task DeleteByID(long ID) {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                DELETE 
                    FROM app_group_permission
                    WHERE id = @ID;
            ");

            cmd.AddParameter("ID", ID);

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
