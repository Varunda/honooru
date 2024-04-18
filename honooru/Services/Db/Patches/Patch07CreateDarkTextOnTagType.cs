using Microsoft.AspNetCore.Mvc.Localization;
using Npgsql;
using System.Threading.Tasks;

namespace honooru.Services.Db.Patches {

    public class Patch07CreateDarkTextOnTagType : IDbPatch {
        public int MinVersion => 7;

        public string Name => "created column dark_text on tag_type";

        public async Task Execute(IDbHelper helper) {

            using NpgsqlConnection conn = helper.Connection();
            using NpgsqlCommand cmd = await helper.Command(conn, @"
                ALTER TABLE tag_type
                    ADD COLUMN IF NOT EXISTS dark_text boolean NOT NULL default false;
            ");

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
