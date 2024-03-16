using honooru.Code.ExtensionMethods;
using honooru.Models.App;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace honooru.Services.Db {

    public class PostTagDb {

        private readonly ILogger<PostTagDb> _Logger;
        private readonly IDataReader<PostTag> _Reader;
        private readonly IDbHelper _DbHelper;

        public PostTagDb(ILogger<PostTagDb> logger,
            IDataReader<PostTag> reader, IDbHelper dbHelper) {

            _Logger = logger;

            _Reader = reader;
            _DbHelper = dbHelper;
        }

        public async Task<List<PostTag>> GetByPostID(ulong postID) {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                SELECT *
                    FROM post_tag
                    WHERE post_id = @PostID;
            ");

            cmd.AddParameter("PostID", postID);
            await cmd.PrepareAsync();

            List<PostTag> tags = await _Reader.ReadList(cmd);
            await conn.CloseAsync();

            return tags;
        }

        public async Task<List<PostTag>> GetByTagID(ulong tagID) {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                SELECT *
                    FROM post_tag
                    WHERE tag_id = @TagID;
            ");

            cmd.AddParameter("TagID", tagID);
            await cmd.PrepareAsync();

            List<PostTag> tags = await _Reader.ReadList(cmd);
            await conn.CloseAsync();

            return tags;
        }

        public async Task Insert(PostTag tag) {
            if (tag.TagID == 0) {

            }

            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                INSERT INTO post_tag (
                    post_id, tag_id 
                ) VALUES (
                    @PostID, @TagID
                );
            ");

            cmd.AddParameter("PostID", tag.PostID);
            cmd.AddParameter("TagID", tag.TagID);
            await cmd.PrepareAsync();

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

        public Task Delete(PostTag tag) {
            return Delete(tag.PostID, tag.TagID);
        }

        public async Task Delete(ulong postID, ulong tagID) {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                DELETE FROM 
                    post_tag
                WHERE 
                    post_id = @PostID
                    AND tag_id = @TagID;
            ");

            cmd.AddParameter("PostID", postID);
            cmd.AddParameter("TagID", tagID);
            await cmd.PrepareAsync();

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
