using honooru.Code.ExtensionMethods;
using honooru.Models.App;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
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

            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                INSERT INTO media_asset (
                    id, md5, status, file_name, file_extension, timestamp, file_size_bytes
                ) VALUES (
                    @ID, @MD5, @Status, @FileName, @FileExtension, @Timestamp, @FileSizeBytes
                ) ON CONFLICT (id) DO UPDATE 
                    set md5 = @MD5,
                        status = @Status,
                        file_name = @FileName,
                        file_extension = @FileExtension,
                        timestamp = @Timestamp,
                        file_size_bytes = @FileSizeBytes;
            ");

            cmd.AddParameter("ID", asset.Guid);
            cmd.AddParameter("MD5", asset.MD5);
            cmd.AddParameter("Status", (int)asset.Status);
            cmd.AddParameter("FileName", asset.FileName);
            cmd.AddParameter("FileExtension", asset.FileExtension);
            cmd.AddParameter("Timestamp", asset.Timestamp);
            cmd.AddParameter("FileSizeBytes", asset.FileSizeBytes);
            await cmd.PrepareAsync();

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
