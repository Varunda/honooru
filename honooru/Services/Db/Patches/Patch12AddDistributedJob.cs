using Npgsql;
using System.Threading.Tasks;

namespace honooru.Services.Db.Patches {

    public class Patch12AddDistributedJob : IDbPatch {
        public int MinVersion => 12;

        public string Name => "add distributed_job";

        public async Task Execute(IDbHelper helper) {
            using NpgsqlConnection conn = helper.Connection();
            using NpgsqlCommand cmd = await helper.Command(conn, @"
                CREATE TABLE IF NOT EXISTS distributed_job (
                    id UUID NOT NULL PRIMARY KEY,
                    type varchar NOT NULL,
                    done boolean NOT NULL,
                    claimed_by_user_id bigint NULL,
                    claimed_at timestamptz NULL,
                    last_progress_update timestamptz NULL,
                    values jsonb NOT NULL
                );
            ");

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }
    }
}
