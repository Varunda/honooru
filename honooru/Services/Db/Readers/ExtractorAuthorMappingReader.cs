using honooru.Code.ExtensionMethods;
using honooru.Models.App;
using Npgsql;
using System.Data;

namespace honooru.Services.Db.Readers {

    public class ExtractorAuthorMappingReader : IDataReader<ExtractorAuthorMapping> {

        public override ExtractorAuthorMapping? ReadEntry(NpgsqlDataReader reader) {
            ExtractorAuthorMapping mapping = new();

            mapping.Site = reader.GetString("site");
            mapping.Author = reader.GetString("author");
            mapping.TagID = reader.GetUInt64("tag_id");
            mapping.Timestamp = reader.GetDateTime("timestamp");

            return mapping;
        }

    }
}
