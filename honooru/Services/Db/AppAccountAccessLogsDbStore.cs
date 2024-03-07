using honooru.Code.ExtensionMethods;
using honooru.Models;
using honooru.Services.Db;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Threading.Tasks;

namespace honooru.Services.Db {

    public class AppAccountAccessLogDbStore {

        private readonly ILogger<AppAccountAccessLogDbStore> _Logger;
        private readonly IDbHelper _DbHelper;
        
        public AppAccountAccessLogDbStore(ILogger<AppAccountAccessLogDbStore> logger,
            IDbHelper helper) {

            _Logger = logger;
            _DbHelper = helper;
        }

        /// <summary>
        ///     Insert a new <see cref="AppAccountAccessLog"/> to the DB
        /// </summary>
        /// <param name="log">Parameters used to insert</param>
        /// <exception cref="ArgumentException">
        ///     Throw if both <see cref="AppAccountAccessLog.AccountID"/> and <see cref="AppAccountAccessLog.Email"/> is null
        /// </exception>
        public async Task Insert(AppAccountAccessLog log) {
            if (log.Email == null && log.AccountID == null) {
                throw new ArgumentException($"Both Email and AccountID cannot be null");
            }

            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                INSERT INTO app_account_access_logs (
                    timestamp, success, account_id, email
                ) VALUES (
                    NOW() AT TIME ZONE 'utc', @Success, @AccountID, @Email
                );
            ");

            cmd.AddParameter("Success", log.Success);
            cmd.AddParameter("AccountID", log.AccountID);
            cmd.AddParameter("Email", log.Email);

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
