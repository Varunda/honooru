using Microsoft.AspNetCore.Mvc.Localization;
using Npgsql;
using System.Threading.Tasks;

namespace honooru.Services.Db.Patches {

    public class Patch08AddContextField : IDbPatch {
        public int MinVersion => 8;
        public string Name => "add context to media_asset and post";

        public async Task Execute(IDbHelper helper) {
            using NpgsqlConnection conn = helper.Connection();
            using NpgsqlCommand cmd = await helper.Command(conn, @"
                ALTER TABLE media_asset ADD COLUMN IF NOT EXISTS context varchar NOT NULL DEFAULT '';

                ALTER TABLE post ADD COLUMN IF NOT EXISTS context varchar NOT NULL DEFAULT '';

                ALTER TABLE pool_post ADD COLUMN IF NOT EXISTS description varchar NOT NULL DEFAULT '';
            ");

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
