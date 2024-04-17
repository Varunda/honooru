using honooru.Code.ExtensionMethods;
using honooru.Models.App;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace honooru.Services.Db {

    public class PostPoolDb {

        private readonly ILogger<PostPoolDb> _Logger;
        private readonly IDataReader<PostPool> _Reader;
        private readonly IDbHelper _DbHelper;

        public PostPoolDb(ILogger<PostPoolDb> logger,
            IDataReader<PostPool> reader, IDbHelper dbHelper) {

            _Logger = logger;
            _Reader = reader;
            _DbHelper = dbHelper;
        }

        public async Task<List<PostPool>> GetAll() {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                SELECT *
                    FROM post_pool;
            ");

            await cmd.PrepareAsync();

            List<PostPool> pools = await _Reader.ReadList(cmd);
            await conn.CloseAsync();

            return pools;
        }

        public async Task<ulong> Insert(PostPool pool) {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                INSERT INTO post_pool (
                    name, created_by_id, timestamp
                ) VALUES (
                    @Name, @CreatedByID, @Timestamp
                );
            ");

            cmd.AddParameter("Name", pool.Name);
            cmd.AddParameter("CreatedByID", pool.CreatedByID);
            cmd.AddParameter("Timestamp", pool.Timestamp);
            await cmd.PrepareAsync();

            ulong id = await cmd.ExecuteUInt64(CancellationToken.None);
            await conn.CloseAsync();

            return id;
        }

        public async Task Update(ulong poolID, PostPool pool) {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                UPDATE post_pool
                    SET name = @Name
                WHERE id = @ID;
            ");

            cmd.AddParameter("Name", pool.Name);
            cmd.AddParameter("ID", poolID);
            await cmd.PrepareAsync();

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

        public async Task Delete(ulong poolID) {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                DELETE FROM post_pool
                    WHERE id = @ID;
            ");

            cmd.AddParameter("ID", poolID);
            await cmd.PrepareAsync();

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
