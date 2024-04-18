using honooru.Code.ExtensionMethods;
using honooru.Models.App;
using Npgsql;
using System.Data;

namespace honooru.Services.Db.Readers {

    public class TagTypeReader : IDataReader<TagType> {

        public override TagType? ReadEntry(NpgsqlDataReader reader) {
            TagType type = new();

            type.ID = reader.GetUInt64("id");
            type.Name = reader.GetString("name");
            type.HexColor = reader.GetString("hex_color");
            type.Alias = reader.GetString("alias");
            type.Order = reader.GetInt16("sort");
            type.DarkText = reader.GetBoolean("dark_text");

            return type;
        }

    }
}
