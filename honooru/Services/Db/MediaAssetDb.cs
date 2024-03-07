using honooru.Code.ExtensionMethods;
using honooru.Models.App;
using Microsoft.Extensions.Logging;
using Npgsql;
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

        public async Task<MediaAsset?> GetByID(ulong assetID) {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                SELECT *
                    FROM media_asset
                    WHERE id = @ID;
            ");

            cmd.AddParameter("ID", assetID);

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

        public async Task<ulong> Insert(MediaAsset asset) {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                INSERT INTO media_asset (
                    md5, file_name, file_location, timestamp, file_size_bytes
                ) VALUES (
                    @MD5, @FileName, @FileLocation, @Timestamp, @FileSizeBytes
                ) RETURNING id;
            ");

            cmd.AddParameter("md5", asset.MD5);
            cmd.AddParameter("FileName", asset.FileName);
            cmd.AddParameter("FileLocation", asset.FileLocation);
            cmd.AddParameter("Timestamp", asset.Timestamp);
            cmd.AddParameter("FileSizeBytes", asset.FileSizeBytes);

            await cmd.PrepareAsync();

            ulong id = await cmd.ExecuteUInt64(CancellationToken.None);

            return id;
        }

    }
}
