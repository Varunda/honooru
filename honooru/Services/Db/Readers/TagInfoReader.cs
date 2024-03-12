using honooru.Code.ExtensionMethods;
using honooru.Models.App;
using Npgsql;

namespace honooru.Services.Db.Readers {

    public class TagInfoReader : IDataReader<TagInfo> {
        
        public override TagInfo? ReadEntry(NpgsqlDataReader reader) {
            TagInfo info = new();

            info.ID = reader.GetUInt64("id");
            info.Uses = reader.GetUInt64("uses");
            info.Description = reader.GetNullableString("description");

            return info;
        }

    }
}
