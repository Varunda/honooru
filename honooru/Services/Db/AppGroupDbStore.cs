using honooru.Code.ExtensionMethods;
using honooru.Models.Internal;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Threading;
using System.Threading.Tasks;

namespace honooru.Services.Db {

    public class AppGroupDbStore {

        private readonly ILogger<AppGroupDbStore> _Logger;
        private readonly IDataReader<AppGroup> _Reader;
        private readonly IDbHelper _DbHelper;

        public AppGroupDbStore(ILogger<AppGroupDbStore> logger,
            IDataReader<AppGroup> reader, IDbHelper dbHelper) {

            _Logger = logger;

            _Reader = reader;
            _DbHelper = dbHelper;
        }

        public async Task<AppGroup?> GetByID(ulong groupID) {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                SELECT *
                    FROM app_group
                    WHERE id = @ID;
            ");

            cmd.AddParameter("ID", groupID);

            await cmd.PrepareAsync();

            AppGroup? group = await _Reader.ReadSingle(cmd);
            await conn.CloseAsync();

            return group;
        }

        public async Task<ulong> Upsert(AppGroup group) {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                INSERT INTO app_group (
                    name, hex_color, implies
                ) VALUES (
                    @Name, @HexColor, @Implies
                ) ON CONFLICT (id) DO UPDATE
                    SET name = @Name,
                        implies = @Implies
                RETURNING id;
            ");

            cmd.AddParameter("Name", group.Name);
            cmd.AddParameter("HexColor", group.HexColor);
            cmd.AddParameter("Implies", string.Join(" ", group.Implies));

            await cmd.PrepareAsync();

            ulong id = await cmd.ExecuteUInt64(CancellationToken.None);

            await conn.CloseAsync();

            return id;
        }

    }
}
