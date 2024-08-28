using Npgsql;
using System.Threading.Tasks;

namespace honooru.Services.Db.Patches {

    public class Patch10AddPrivatePost : IDbPatch {

        public int MinVersion => 10;

        public string Name => "add private to post";

        public async Task Execute(IDbHelper helper) {
            using NpgsqlConnection conn = helper.Connection();
            using NpgsqlCommand cmd = await helper.Command(conn, @"
                ALTER TABLE post 
                    ADD COLUMN IF NOT EXISTS private boolean NOT NULL DEFAULT FALSE;
            ");

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
