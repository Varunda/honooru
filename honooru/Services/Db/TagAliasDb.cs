using honooru.Code.ExtensionMethods;
using honooru.Models.App;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace honooru.Services.Db {

    public class TagAliasDb {

        private readonly ILogger<TagAliasDb> _Logger;
        private readonly IDbHelper _DbHelper;
        private readonly IDataReader<TagAlias> _Reader;

        public TagAliasDb(ILogger<TagAliasDb> logger,
            IDbHelper dbHelper, IDataReader<TagAlias> reader) {

            _Logger = logger;

            _DbHelper = dbHelper;
            _Reader = reader;
        }

        public async Task<List<TagAlias>> GetAll() {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                SELECT *
                    FROM tag_alias;
            ");

            await cmd.PrepareAsync();

            List<TagAlias> aliases = await _Reader.ReadList(cmd);
            await conn.CloseAsync();

            return aliases;
        }

        public async Task<TagAlias?> GetByAlias(string alias) {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                SELECT *
                    FROM tag_alias
                    WHERE alias = @Alias;
            ");

            cmd.AddParameter("Alias", alias.Trim().ToLower());
            await cmd.PrepareAsync();

            TagAlias? r = await _Reader.ReadSingle(cmd);
            await conn.CloseAsync();

            return r;
        }

        public async Task<List<TagAlias>> GetByTagID(ulong tagID) {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                SELECT *
                    FROM tag_alias
                    WHERE tag_id = @TagID;
            ");

            cmd.AddParameter("TagID", tagID);
            await cmd.PrepareAsync();

            List<TagAlias> r = await _Reader.ReadList(cmd);
            await conn.CloseAsync();

            return r;
        }

        public async Task Insert(TagAlias alias) {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                INSERT INTO tag_alias (
                    alias, tag_id
                ) VALUES (
                    @Alias, @TagID
                );
            ");

            cmd.AddParameter("Alias", alias.Alias);
            cmd.AddParameter("TagID", alias.TagID);
            await cmd.PrepareAsync();

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

        public async Task Delete(string alias) {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                DELETE FROM
                    tag_alias
                WHERE
                    alias = @Alias;
            ");

            cmd.AddParameter("Alias", alias.Trim().ToLower());
            await cmd.PrepareAsync();

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
