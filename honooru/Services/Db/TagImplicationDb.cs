using honooru.Code.ExtensionMethods;
using honooru.Models.App;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace honooru.Services.Db {

    public class TagImplicationDb {

        private readonly ILogger<TagImplicationDb> _Logger;
        private readonly IDataReader<TagImplication> _Reader;
        private readonly IDbHelper _DbHelper;

        public TagImplicationDb(ILogger<TagImplicationDb> logger,
            IDataReader<TagImplication> reader, IDbHelper dbHelper) {

            _Logger = logger;

            _Reader = reader;
            _DbHelper = dbHelper;
        }

        public async Task<List<TagImplication>> GetAll() {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                SELECT *
                    FROM tag_implication;
            ");
            await cmd.PrepareAsync();

            List<TagImplication> implications = await _Reader.ReadList(cmd);
            await conn.CloseAsync();

            return implications;
        }

        public async Task<List<TagImplication>> GetBySourceTagID(ulong tagID) {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                SELECT *
                    FROM tag_implication
                    WHERE tag_a = @TagID;
            ");
            cmd.AddParameter("TagID", tagID);
            await cmd.PrepareAsync();

            List<TagImplication> implications = await _Reader.ReadList(cmd);
            await conn.CloseAsync();

            return implications;
        }

        public async Task<List<TagImplication>> GetByTargetTagID(ulong tagID) {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                SELECT *
                    FROM tag_implication
                    WHERE tag_b = @TagID;
            ");
            cmd.AddParameter("TagID", tagID);
            await cmd.PrepareAsync();

            List<TagImplication> implications = await _Reader.ReadList(cmd);
            await conn.CloseAsync();

            return implications;
        }

        public async Task Insert(TagImplication implication) {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                INSERT INTO tag_implication (
                    tag_a, tag_b
                ) VALUES (
                    @TagA, @TagB
                );
            ");
            cmd.AddParameter("TagA", implication.TagA);
            cmd.AddParameter("TagB", implication.TagB);
            await cmd.PrepareAsync();

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

        public async Task Delete(TagImplication implication) {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                DELETE FROM
                    tag_implication
                WHERE
                    tag_a = @TagA
                    AND tag_b = @TagB;
            ");
            cmd.AddParameter("TagA", implication.TagA);
            cmd.AddParameter("TagB", implication.TagB);
            await cmd.PrepareAsync();

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
