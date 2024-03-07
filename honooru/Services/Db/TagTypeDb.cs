using honooru.Code.ExtensionMethods;
using honooru.Models.App;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Threading;
using System.Threading.Tasks;

namespace honooru.Services.Db {

    public class TagTypeDb {

        private readonly ILogger<TagTypeDb> _Logger;
        private readonly IDataReader<TagType> _Reader;
        private readonly IDbHelper _DbHelper;

        public TagTypeDb(ILogger<TagTypeDb> logger,
            IDataReader<TagType> reader, IDbHelper dbHelper) {

            _Logger = logger;

            _Reader = reader;
            _DbHelper = dbHelper;
        }

        public async Task<TagType?> GetByID(ulong ID) {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                SELECT *
                    FROM tag_type
                    WHERE id = @ID;
            ");

            cmd.AddParameter("ID", ID);
            await cmd.PrepareAsync();

            TagType? type = await _Reader.ReadSingle(cmd);
            await conn.CloseAsync();

            return type;
        }

        public async Task<TagType?> GetByName(string name) {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                SELECT *
                    FROM tag_type
                    WHERE lower(name) = @Name;
            ");

            cmd.AddParameter("Name", name.ToLower());
            await cmd.PrepareAsync();

            TagType? type = await _Reader.ReadSingle(cmd);
            await conn.CloseAsync();

            return type;
        }

        public async Task<TagType?> GetByAlias(string alias) {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                SELECT *
                    FROM tag_type
                    WHERE alias = @Alias;
            ");

            cmd.AddParameter("Alias", alias.ToLower());
            await cmd.PrepareAsync();

            TagType? type = await _Reader.ReadSingle(cmd);
            await conn.CloseAsync();

            return type;
        }

        public async Task<ulong> Insert(TagType type) {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                INSERT INTO tag_type (
                    name, hex_color, alias
                ) VALUES (
                    @Name, @HexColor, @Alias
                ) RETURNING id;
            ");

            cmd.AddParameter("Name", type.Name);
            cmd.AddParameter("HexColor", type.HexColor);
            cmd.AddParameter("Alias", type.Alias);
            await cmd.PrepareAsync();

            ulong id = await cmd.ExecuteUInt64(CancellationToken.None);
            await conn.CloseAsync();

            return id;
        }

        public async Task<TagType> Update(TagType type) {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                UPDATE tag_type
                    SET name = @Name,
                        hex_color = @HexColor,
                        alias = @Alias
                    WHERE id = @ID;
            ");

            cmd.AddParameter("ID", type.ID);
            cmd.AddParameter("Name", type.Name);
            cmd.AddParameter("HexColor", type.HexColor);
            cmd.AddParameter("Alias", type.Alias);
            await cmd.PrepareAsync();

            ulong id = await cmd.ExecuteUInt64(CancellationToken.None);
            await conn.CloseAsync();

            return type;
        }

    }
}
