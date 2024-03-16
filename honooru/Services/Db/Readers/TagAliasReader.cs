using honooru.Code.ExtensionMethods;
using honooru.Models.App;
using Npgsql;
using System.Data;

namespace honooru.Services.Db.Readers {

    public class TagAliasReader : IDataReader<TagAlias> {

        public override TagAlias? ReadEntry(NpgsqlDataReader reader) {
            TagAlias? alias = new();

            alias.Alias = reader.GetString("alias");
            alias.TagID = reader.GetUInt64("tag_id");

            return alias;
        }

    }
}
