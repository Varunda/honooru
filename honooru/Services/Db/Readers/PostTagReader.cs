using honooru.Code.ExtensionMethods;
using honooru.Models.App;
using Npgsql;

namespace honooru.Services.Db.Readers {

    public class PostTagReader : IDataReader<PostTag> {

        public override PostTag? ReadEntry(NpgsqlDataReader reader) {
            PostTag pt = new();

            pt.PostID = reader.GetUInt64("post_id");
            pt.TagID = reader.GetUInt64("tag_id");

            return pt;
        }

    }
}
