using honooru.Code.ExtensionMethods;
using honooru.Models.App;
using Npgsql;
using System;
using System.Data;

namespace honooru.Services.Db.Readers {

    public class MediaAssetReader : IDataReader<MediaAsset> {

        public override MediaAsset? ReadEntry(NpgsqlDataReader reader) {
            MediaAsset asset = new();

            asset.Guid = reader.GetGuid("id");
            asset.PostID = reader.GetNullableUInt64("post_id");
            asset.MD5 = reader.GetString("md5");

            int statusID = reader.GetInt32("status");
            if (Enum.IsDefined((MediaAssetStatus) statusID)) {
                asset.Status = (MediaAssetStatus)statusID;
            } else {
                throw new InvalidCastException($"{statusID} is not a valid {nameof(MediaAssetStatus)}");
            }

            asset.FileName = reader.GetString("file_name");
            asset.FileExtension = reader.GetString("file_extension");
            asset.Timestamp = reader.GetDateTime("timestamp");
            asset.FileSizeBytes = reader.GetInt64("file_size_bytes");
            asset.Source = reader.GetString("source");
            asset.IqdbHash = reader.GetNullableString("iqdb_hash");
            asset.AdditionalTags = reader.GetString("additional_tags");
            asset.Title = reader.GetString("title");
            asset.Description = reader.GetString("description");

            return asset;
        }

    }
}
