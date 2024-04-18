using honooru.Code.ExtensionMethods;
using honooru.Models.App;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace honooru.Services.Db {

    /// <summary>
    ///     service to interact with the tag type table in the DB
    /// </summary>
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

        /// <summary>
        ///     get all <see cref="TagType"/>s in the DB
        /// </summary>
        /// <returns></returns>
        public async Task<List<TagType>> GetAll() {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                SELECT *
                    FROM tag_type;
            ");

            await cmd.PrepareAsync();

            List<TagType> types = await _Reader.ReadList(cmd);
            await conn.CloseAsync();

            return types;
        }

        /// <summary>
        ///     get a <see cref="TagType"/> by its <see cref="TagType.ID"/>
        /// </summary>
        /// <param name="ID"></param>
        /// <returns></returns>
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

        /// <summary>
        ///     get a <see cref="TagType"/> by its <see cref="TagType.Name"/>
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
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

        /// <summary>
        ///     get a <see cref="TagType"/> by its <see cref="TagType.Alias"/>
        /// </summary>
        /// <param name="alias"></param>
        /// <returns></returns>
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

        /// <summary>
        ///     insert a new <see cref="TagType"/> and return the <see cref="TagType.ID"/> that was just inserted into the DB
        /// </summary>
        /// <param name="type">parameters of the <see cref="TagType"/> to insert</param>
        /// <returns>
        ///     the <see cref="TagType.ID"/> of the <see cref="TagType"/> that was just inserted into the DB
        /// </returns>
        public async Task<ulong> Insert(TagType type) {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                INSERT INTO tag_type (
                    name, hex_color, alias, sort, dark_text
                ) VALUES (
                    @Name, @HexColor, @Alias, @Order, @DarkText
                ) RETURNING id;
            ");

            cmd.AddParameter("Name", type.Name);
            cmd.AddParameter("HexColor", type.HexColor);
            cmd.AddParameter("Alias", type.Alias);
            cmd.AddParameter("Order", type.Order);
            cmd.AddParameter("DarkText", type.DarkText);
            await cmd.PrepareAsync();

            ulong id = await cmd.ExecuteUInt64(CancellationToken.None);
            await conn.CloseAsync();

            return id;
        }

        /// <summary>
        ///     update an existing <see cref="TagType"/> with new info
        /// </summary>
        /// <param name="typeID">ID of hte <see cref="TagType"/> that is being updated</param>
        /// <param name="type"><see cref="TagType"/> to update</param>
        /// <returns>
        ///     <paramref name="type"/>
        /// </returns>
        public async Task<TagType> Update(ulong typeID, TagType type) {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                UPDATE tag_type
                    SET name = @Name,
                        hex_color = @HexColor,
                        alias = @Alias,
                        sort = @Order,
                        dark_text = @DarkText
                    WHERE id = @ID;
            ");

            cmd.AddParameter("ID", typeID);
            cmd.AddParameter("Name", type.Name);
            cmd.AddParameter("HexColor", type.HexColor);
            cmd.AddParameter("Alias", type.Alias);
            cmd.AddParameter("Order", type.Order);
            cmd.AddParameter("DarkText", type.DarkText);
            await cmd.PrepareAsync();

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();

            return type;
        }

    }
}
