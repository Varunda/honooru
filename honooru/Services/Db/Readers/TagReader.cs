using honooru.Code.ExtensionMethods;
using honooru.Models.App;
using Npgsql;
using System.Data;

namespace honooru.Services.Db.Readers {

    public class TagReader : IDataReader<Tag> {

        public override Tag? ReadEntry(NpgsqlDataReader reader) {
            Tag tag = new();

            tag.ID = reader.GetUInt64("id");
            tag.Name = reader.GetString("name");
            tag.TypeID = reader.GetUInt64("type_id");
            tag.Timestamp = reader.GetDateTime("timestamp");

            return tag;
        }

    }
}
