using Npgsql;
using System.Threading.Tasks;

namespace honooru.Services.Db.Patches {

    public class Patch05AddFileTypeColumns : IDbPatch {
        public int MinVersion => 5;
        public string Name => "add file_type columns to post and media_asset";

        public async Task Execute(IDbHelper helper) {
            using NpgsqlConnection conn = helper.Connection();
            using NpgsqlCommand cmd = await helper.Command(conn, @"
                ALTER TABLE post ADD COLUMN IF NOT EXISTS file_type VARCHAR NOT NULL default '';

                ALTER TABLE media_asset ADD COLUMN IF NOT EXISTS file_type VARCHAR NOT NULL default '';
            ");

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
