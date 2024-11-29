using honooru.Code;
using honooru.Code.ExtensionMethods;
using honooru.Models;
using honooru.Models.App;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace honooru.Services.Db {

    public class ApiKeyDb {

        private readonly ILogger<ApiKeyDb> _Logger;
        private readonly IDbHelper _DbHelper;
        private readonly IDataReader<ApiKey> _Reader;

        public ApiKeyDb(ILogger<ApiKeyDb> logger,
            IDbHelper dbHelper, IDataReader<ApiKey> reader) {

            _Logger = logger;

            _DbHelper = dbHelper;
            _Reader = reader;
        }

        /// <summary>
        ///     get the <see cref="ApiKey"/> with <see cref="ApiKey.UserID"/> of <paramref name="userID"/>
        /// </summary>
        /// <param name="userID">ID of the <see cref="AppAccount"/> to get the <see cref="ApiKey"/> of</param>
        /// <returns>
        ///     the <see cref="ApiKey"/> with <see cref="ApiKey.UserID"/> of <paramref name="userID"/>,
        ///     or <c>null</c> if it does not exist
        /// </returns>
        public async Task<ApiKey?> GetByUserID(ulong userID) {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                SELECT *
                    FROM api_key
                    WHERE user_id = @UserID
            ");

            cmd.AddParameter("UserID", userID);
            await cmd.PrepareAsync();

            ApiKey? key = await _Reader.ReadSingle(cmd);
            await conn.CloseAsync();

            return key;
        }

        /// <summary>
        ///     create a new <see cref="ApiKey"/> for a user. the <see cref="ApiKey.ClientSecret"/> will be randomly generated
        /// </summary>
        /// <param name="userID"></param>
        /// <returns></returns>
        public async Task Create(ulong userID) {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                INSERT INTO api_key (
                    user_id, client_secret, timestamp
                ) VALUES (
                    @UserID, @ClientSecret, now() AT TIME ZONE 'utc'
                );
            ");

            cmd.AddParameter("UserID", userID);
            cmd.AddParameter("ClientSecret", ClientSecretGenerator.GetUniqueKey(128));
            await cmd.PrepareAsync();

            await cmd.ExecuteNonQueryAsync();

            await conn.CloseAsync();
        }

        /// <summary>
        ///     delete a <see cref="ApiKey"/> for a user
        /// </summary>
        /// <param name="userID"></param>
        /// <returns></returns>
        public async Task Delete(ulong userID) {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                DELETE FROM api_key
                    WHERE user_id = @UserID;
            ");

            cmd.AddParameter("UserID", userID);
            await cmd.PrepareAsync();

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
