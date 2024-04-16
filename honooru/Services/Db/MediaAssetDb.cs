using honooru.Code.ExtensionMethods;
using honooru.Models.App;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace honooru.Services.Db {

    public class MediaAssetDb {

        private readonly ILogger<MediaAssetDb> _Logger;
        private readonly IDbHelper _DbHelper;
        private readonly IDataReader<MediaAsset> _Reader;

        public MediaAssetDb(ILogger<MediaAssetDb> logger,
            IDbHelper dbHelper, IDataReader<MediaAsset> reader) {

            _Logger = logger;

            _DbHelper = dbHelper;
            _Reader = reader;
        }

        /// <summary>
        ///     get all <see cref="MediaAsset"/>s
        /// </summary>
        /// <returns></returns>
        public async Task<List<MediaAsset>> GetAll() {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                SELECT *
                    FROM media_asset;
            ");

            await cmd.PrepareAsync();

            List<MediaAsset> assets = await _Reader.ReadList(cmd);
            await conn.CloseAsync();

            return assets;
        }

        /// <summary>
        ///     get a single <see cref="MediaAsset"/> by it's <see cref="MediaAsset.Guid"/>
        /// </summary>
        /// <param name="guid">Guid of the <see cref="MediaAsset"/> to get</param>
        /// <returns>
        ///     the <see cref="MediaAsset"/> with <see cref="MediaAsset.Guid"/> of <paramref name="guid"/>,
        ///     or <c>null</c> if it does not exist
        /// </returns>
        public async Task<MediaAsset?> GetByID(Guid guid) {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                SELECT *
                    FROM media_asset
                    WHERE id = @ID;
            ");

            cmd.AddParameter("ID", guid);

            await cmd.PrepareAsync();

            MediaAsset? asset = await _Reader.ReadSingle(cmd);
            await conn.CloseAsync();

            return asset;
        }

        /// <summary>
        ///     get a <see cref="MediaAsset"/> by <see cref="MediaAsset.MD5"/>
        /// </summary>
        /// <param name="md5">md5 hash</param>
        /// <returns></returns>
        public async Task<MediaAsset?> GetByMD5(string md5) {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                SELECT *
                    FROM media_asset
                    WHERE md5 = @MD5;
            ");

            cmd.AddParameter("MD5", md5.ToLower());

            await cmd.PrepareAsync();

            MediaAsset? asset = await _Reader.ReadSingle(cmd);
            await conn.CloseAsync();

            return asset;
        }

        public async Task Upsert(MediaAsset asset) {
            if (asset.Guid == Guid.Empty) {
                throw new Exception($"not inserting a {nameof(MediaAsset)} with an empty guid");
            }
            if (asset.MD5 == "") {
                throw new Exception($"not inserting a {nameof(MediaAsset)} with an empty MD5");
            }

            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                INSERT INTO media_asset (
                    id, post_id, md5, status, file_name, file_extension, timestamp, file_size_bytes, source, additional_tags, title, description, iqdb_hash, file_type
                ) VALUES (
                    @ID, @PostID, @MD5, @Status, @FileName, @FileExtension, @Timestamp, @FileSizeBytes, @Source, @AdditionalTags, @Title, @Description, @IqdbHash, @FileType
                ) ON CONFLICT (id) DO UPDATE 
                    set md5 = @MD5,
                        post_id = @PostID,
                        status = @Status,
                        file_name = @FileName,
                        file_extension = @FileExtension,
                        timestamp = @Timestamp,
                        file_size_bytes = @FileSizeBytes,
                        source = @Source,
                        additional_tags = @AdditionalTags,
                        title = @Title,
                        description = @Description,
                        iqdb_hash = @IqdbHash,
                        file_type = @FileType
                ;
            ");

            cmd.AddParameter("ID", asset.Guid);
            cmd.AddParameter("PostID", asset.PostID);
            cmd.AddParameter("MD5", asset.MD5);
            cmd.AddParameter("Status", (int)asset.Status);
            cmd.AddParameter("FileName", asset.FileName);
            cmd.AddParameter("FileExtension", asset.FileExtension);
            cmd.AddParameter("IqdbHash", asset.IqdbHash);
            cmd.AddParameter("Timestamp", asset.Timestamp);
            cmd.AddParameter("FileSizeBytes", asset.FileSizeBytes);
            cmd.AddParameter("Source", asset.Source);
            cmd.AddParameter("AdditionalTags", asset.AdditionalTags);
            cmd.AddParameter("Title", asset.Title);
            cmd.AddParameter("Description", asset.Description);
            cmd.AddParameter("FileType", asset.FileType);
            await cmd.PrepareAsync();

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

        /// <summary>
        ///     delete a <see cref="MediaAsset"/> from the DB
        /// </summary>
        /// <param name="assetID"></param>
        /// <returns></returns>
        public async Task Delete(Guid assetID) {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                DELETE FROM
                    media_asset
                WHERE
                    id = @AssetID;
            ");

            cmd.AddParameter("AssetID", assetID);
            await cmd.PrepareAsync();

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
