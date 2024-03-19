using honooru.Code.ExtensionMethods;
using honooru.Models.App;
using Npgsql;

namespace honooru.Services.Db.Readers {

    public class TagImplicationReader : IDataReader<TagImplication> {

        public override TagImplication? ReadEntry(NpgsqlDataReader reader) {
            TagImplication implication = new();

            implication.TagA = reader.GetUInt64("tag_a");
            implication.TagB = reader.GetUInt64("tag_b");

            return implication;
        }

    }
}
