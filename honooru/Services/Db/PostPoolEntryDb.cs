using honooru.Code.ExtensionMethods;
using honooru.Models.App;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace honooru.Services.Db {

    public class PostPoolEntryDb {

        private readonly ILogger<PostPoolEntryDb> _Logger;
        private readonly IDataReader<PostPoolEntry> _Reader;
        private readonly IDbHelper _DbHelper;

        public PostPoolEntryDb(ILogger<PostPoolEntryDb> logger,
            IDataReader<PostPoolEntry> reader, IDbHelper dbHelper) {

            _Logger = logger;
            _Reader = reader;
            _DbHelper = dbHelper;
        }

        public async Task<List<PostPoolEntry>> GetByPoolID(ulong poolID) {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                SELECT *
                    FROM post_pool_entry
                    WHERE pool_id = @PoolID;
            ");

            cmd.AddParameter("PoolID", poolID);
            await cmd.PrepareAsync();

            List<PostPoolEntry> entries = await _Reader.ReadList(cmd);
            await conn.CloseAsync();

            return entries;
        }

        public async Task<List<PostPoolEntry>> GetByPostID(ulong postID) {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                SELECT *
                    FROM post_pool_entry
                    WHERE post_id = @PostID;
            ");

            cmd.AddParameter("PostID", postID);
            await cmd.PrepareAsync();

            List<PostPoolEntry> entries = await _Reader.ReadList(cmd);
            await conn.CloseAsync();

            return entries;
        }

        public async Task Insert(PostPoolEntry entry) {
            if (entry.PoolID == 0) {
                throw new ArgumentException($"cannot insert a new {nameof(PostPoolEntry)}: {nameof(entry)} has 0 for {nameof(PostPoolEntry.PoolID)}");
            }

            if (entry.PostID == 0) {
                throw new ArgumentException($"cannot insert a new {nameof(PostPoolEntry)}: {nameof(entry)} has 0 for {nameof(PostPoolEntry.PostID)}");
            }

            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                INSERT INTO post_pool_entry (
                    pool_id, post_id
                ) VALUES (
                    @PoolID, @PostID
                );
            ");

            cmd.AddParameter("PoolID", entry.PoolID);
            cmd.AddParameter("PostID", entry.PostID);
            await cmd.PrepareAsync();

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

        public async Task Delete(PostPoolEntry entry) {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                DELETE FROM post_pool_entry
                    WHERE pool_id = @PoolID
                        AND post_id = @PostID;
            ");

            cmd.AddParameter("PoolID", entry.PoolID);
            cmd.AddParameter("PostID", entry.PostID);
            await cmd.PrepareAsync();

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
