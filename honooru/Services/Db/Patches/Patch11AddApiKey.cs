using Npgsql;
using System.Threading.Tasks;

namespace honooru.Services.Db.Patches {

    public class Patch11AddApiKey : IDbPatch {
        public int MinVersion => 11;
        public string Name => "add api_key";

        public async Task Execute(IDbHelper helper) {

            using NpgsqlConnection conn = helper.Connection();
            using NpgsqlCommand cmd = await helper.Command(conn, @"
                CREATE TABLE IF NOT EXISTS api_key (
                    user_id bigint NOT NULL PRIMARY KEY,
                    client_secret varchar NOT NULL,
                    timestamp timestamptz NOT NULL
                );
            ");

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
