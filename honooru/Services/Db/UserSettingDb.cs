using honooru.Code.ExtensionMethods;
using honooru.Models.App;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace honooru.Services.Db {

    public class UserSettingDb {

        private readonly ILogger<UserSettingDb> _Logger;
        private readonly IDataReader<UserSetting> _Reader;
        private readonly IDbHelper _DbHelper;

        public UserSettingDb(ILogger<UserSettingDb> logger,
            IDataReader<UserSetting> reader, IDbHelper dbHelper) {

            _Logger = logger;

            _Reader = reader;
            _DbHelper = dbHelper;
        }

        public async Task<List<UserSetting>> GetByAccountID(ulong accountID) {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                SELECT *
                    FROM app_user_setting
                    WHERE account_id = @AccountID;
            ");

            cmd.AddParameter("AccountID", accountID);
            await cmd.PrepareAsync();

            List<UserSetting> settings = await _Reader.ReadList(cmd);
            await conn.CloseAsync();

            return settings;
        }

        public async Task Upsert(UserSetting setting) {
            if (string.IsNullOrWhiteSpace(setting.Name)) {
                throw new ArgumentException($"{nameof(UserSetting.Name)} cannot be empty");
            }

            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                INSERT INTO app_user_setting (
                    account_id, name, type, value
                ) VALUES (
                    @AccountID, @Name, @Type, @Value
                ) ON CONFLICT (account_id, name) DO UPDATE 
                    SET value = @Value,
                        type = @Type;
            ");

            cmd.AddParameter("AccountID", setting.AccountID);
            cmd.AddParameter("Name", setting.Name);
            cmd.AddParameter("Type", setting.Type.ToString());
            cmd.AddParameter("Value", setting.Value);
            await cmd.PrepareAsync();

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
