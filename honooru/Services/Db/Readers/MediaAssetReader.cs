using honooru.Code.ExtensionMethods;
using honooru.Models.App;
using Npgsql;
using System.Data;

namespace honooru.Services.Db.Readers {

    public class MediaAssetReader : IDataReader<MediaAsset> {

        public override MediaAsset? ReadEntry(NpgsqlDataReader reader) {
            MediaAsset asset = new();

            asset.ID = reader.GetUInt64("id");
            asset.MD5 = reader.GetString("md5");
            asset.FileName = reader.GetString("file_name");
            asset.FileLocation = reader.GetString("file_location");
            asset.Timestamp = reader.GetDateTime("timestamp");
            asset.FileSizeBytes = reader.GetInt64("file_size_bytes");

            return asset;
        }

    }
}
