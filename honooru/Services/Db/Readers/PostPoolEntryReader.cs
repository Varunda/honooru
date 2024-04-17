using honooru.Code.ExtensionMethods;
using honooru.Models.App;
using Npgsql;

namespace honooru.Services.Db.Readers {

    public class PostPoolEntryReader : IDataReader<PostPoolEntry> {

        public override PostPoolEntry? ReadEntry(NpgsqlDataReader reader) {
            PostPoolEntry entry = new();

            entry.PoolID = reader.GetUInt64("pool_id");
            entry.PostID = reader.GetUInt64("post_id");

            return entry;
        }

    }
}
