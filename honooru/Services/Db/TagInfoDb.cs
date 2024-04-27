using honooru.Code.ExtensionMethods;
using honooru.Models.App;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Threading.Tasks;

namespace honooru.Services.Db {

    public class TagInfoDb {

        private readonly ILogger<TagInfoDb> _Logger;
        private readonly IDbHelper _DbHelper;
        private readonly IDataReader<TagInfo> _Reader;

        public TagInfoDb(ILogger<TagInfoDb> logger,
            IDbHelper dbHelper, IDataReader<TagInfo> reader) {

            _Logger = logger;
            _DbHelper = dbHelper;
            _Reader = reader;
        }

        public async Task<TagInfo?> GetByID(ulong tagID) {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                SELECT *
                    FROM tag_info
                    WHERE id = @ID;
            ");

            cmd.AddParameter("ID", tagID);
            await cmd.PrepareAsync();

            TagInfo? info = await _Reader.ReadSingle(cmd);
            await conn.CloseAsync();

            return info;
        }

        public async Task Upsert(TagInfo info) {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                INSERT INTO tag_info (
                    id, uses, description
                ) VALUES (
                    @ID, @Uses, @Description
                ) ON CONFLICT (id) DO UPDATE 
                    SET uses = @Uses,
                        description = @Description;
            ");

            cmd.AddParameter("ID", info.ID);
            cmd.AddParameter("Uses", info.Uses);
            cmd.AddParameter("Description", info.Description);
            await cmd.PrepareAsync();

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

        public async Task Delete(ulong tagID) {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                DELETE FROM
                    tag_info
                WHERE
                    id = @ID;
            ");

            cmd.AddParameter("ID", tagID);
            await cmd.PrepareAsync();

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
