using Npgsql;
using System.Threading.Tasks;

namespace honooru.Services.Db.Patches {

    public class Patch06AddPostPool : IDbPatch {
        public int MinVersion => 6;
        public string Name => "created post_pool";

        public async Task Execute(IDbHelper helper) {

            using NpgsqlConnection conn = helper.Connection();
            using NpgsqlCommand cmd = await helper.Command(conn, @"
                CREATE TABLE IF NOT EXISTS post_pool (
                    id bigint NOT NULL PRIMARY KEY GENERATED ALWAYS AS IDENTITY (START WITH 1),
                    name varchar NOT NULL,
                    created_by_id bigint NOT NULL,
                    timestamp timestamptz
                );

                CREATE TABLE IF NOT EXISTS post_pool_entry (
                    pool_id bigint NOT NULL,
                    post_id bigint NOT NULL,
                
                    CONSTRAINT fk_post_pool_entry_pool_id FOREIGN KEY (pool_id) REFERENCES post_pool(id),
                    CONSTRAINT fk_post_pool_entry_post_id FOREIGN KEY (post_id) REFERENCES post(id)
                );

                CREATE INDEX IF NOT EXISTS idx_post_pool_entry_pool_id ON post_pool_entry(pool_id);
                CREATE INDEX IF NOT EXISTS idx_post_pool_entry_post_id ON post_pool_entry(post_id);
            ");

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
