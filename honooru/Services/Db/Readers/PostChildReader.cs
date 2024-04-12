using honooru.Code.ExtensionMethods;
using honooru.Models.App;
using Npgsql;

namespace honooru.Services.Db.Readers {

    public class PostChildReader : IDataReader<PostChild> {

        public override PostChild? ReadEntry(NpgsqlDataReader reader) {
            PostChild child = new();

            child.ParentPostID = reader.GetUInt64("parent_post_id");
            child.ChildPostID = reader.GetUInt64("child_post_id");

            return child;
        }

    }
}
