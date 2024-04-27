using Npgsql;
using System.Threading.Tasks;

namespace honooru.Services.Db.Patches {

    public class Patch09AddExtractorAuthorMapping : IDbPatch {
        public int MinVersion => 9;

        public string Name => "add extractor_author_mapping table";

        public async Task Execute(IDbHelper helper) {
            using NpgsqlConnection conn = helper.Connection();
            using NpgsqlCommand cmd = await helper.Command(conn, @"
                CREATE TABLE IF NOT EXISTS extractor_author_mapping (
                    site varchar NOT NULL,
                    channel varchar NOT NULL,
                    tag_id bigint NOT NULL,
                    timestamp timestamptz NOT NULL,

                    PRIMARY KEY (site, channel),
                    CONSTRAINT fk_extractor_author_mapping_tag_id FOREIGN KEY (tag_id) REFERENCES tag(id)
                );
            ");

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
