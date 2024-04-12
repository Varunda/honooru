using Npgsql;
using System.Threading.Tasks;

namespace honooru.Services.Db.Patches {

    public class Patch04AddPostChildTable : IDbPatch {

        public int MinVersion => 4;
        public string Name => "add post_child table";

        public async Task Execute(IDbHelper helper) {
            using NpgsqlConnection conn = helper.Connection();
            using NpgsqlCommand cmd = await helper.Command(conn, @"
                CREATE TABLE IF NOT EXISTS post_child (
                    parent_post_id bigint NOT NULL,
                    child_post_id bigint NOT NULL,
                
                    PRIMARY KEY (parent_post_id, child_post_id)
                );

                CREATE INDEX IF NOT EXISTS idx_post_child_parent ON post_child(parent_post_id);
                CREATE INDEX IF NOT EXISTS idx_post_child_child ON post_child(child_post_id);
            ");

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
