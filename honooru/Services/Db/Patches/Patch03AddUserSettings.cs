using Npgsql;
using System.Threading.Tasks;

namespace honooru.Services.Db.Patches {

    public class Patch03AddUserSettings : IDbPatch {
        public int MinVersion => 3;
        public string Name => "add user_settings";

        public async Task Execute(IDbHelper helper) {
            using NpgsqlConnection conn = helper.Connection();
            using NpgsqlCommand cmd = await helper.Command(conn, @"
                CREATE TABLE IF NOT EXISTS app_user_setting (
                    account_id bigint NOT NULL,
                    name varchar NOT NULL,
                    type varchar NOT NULL,
                    value varchar NOT NULL,
                    
                    PRIMARY KEY (account_id, name)
                );

                CREATE INDEX IF NOT EXISTS idx_app_user_setting_account_id ON app_user_setting(account_id);
            ");

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }
    }
}
