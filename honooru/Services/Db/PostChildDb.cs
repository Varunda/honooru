using honooru.Code.ExtensionMethods;
using honooru.Models.App;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace honooru.Services.Db {

    public class PostChildDb {

        private readonly ILogger<PostChildDb> _Logger;
        private readonly IDataReader<PostChild> _Reader;
        private readonly IDbHelper _DbHelper;

        public PostChildDb(ILogger<PostChildDb> logger,
            IDataReader<PostChild> reader, IDbHelper dbHelper) {

            _Logger = logger;
            _Reader = reader;
            _DbHelper = dbHelper;
        }

        public async Task<List<PostChild>> GetByParentID(ulong parentID) {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                SELECT *
                    FROM post_child
                    WHERE parent_post_id = @ParentID;
            ");

            cmd.AddParameter("ParentID", parentID);
            await cmd.PrepareAsync();

            List<PostChild> posts = await _Reader.ReadList(cmd);
            await conn.CloseAsync();

            return posts;
        }

        public async Task<List<PostChild>> GetByChildID(ulong childID) {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                SELECT *
                    FROM post_child
                    WHERE child_post_id = @ChildID;
            ");

            cmd.AddParameter("ChildID", childID);
            await cmd.PrepareAsync();

            List<PostChild> posts = await _Reader.ReadList(cmd);
            await conn.CloseAsync();

            return posts;
        }

        public async Task Insert(PostChild child) {
            if (child.ParentPostID == 0 || child.ChildPostID == 0 || child.ParentPostID == child.ChildPostID) {
                throw new Exception($"invalid relation, {child.ParentPostID} cannot be parent of {child.ChildPostID}");
            }

            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                INSERT INTO post_child (
                    parent_post_id, child_post_id
                ) VALUES (
                    @ParentPostID, @ChildPostID
                );
            ");

            cmd.AddParameter("ParentPostID", child.ParentPostID);
            cmd.AddParameter("ChildPostID", child.ChildPostID);
            await cmd.PrepareAsync();

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

        public async Task Remove(PostChild child) {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                DELETE FROM post_child
                    WHERE parent_post_id = @ParentPostID
                        AND child_post_id = @ChildPostID;
            ");

            cmd.AddParameter("ParentPostID", child.ParentPostID);
            cmd.AddParameter("ChildPostID", child.ChildPostID);
            await cmd.PrepareAsync();

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
