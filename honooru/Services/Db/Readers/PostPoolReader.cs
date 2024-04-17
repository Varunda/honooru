using honooru.Code.ExtensionMethods;
using honooru.Models.App;
using Npgsql;
using System.Data;

namespace honooru.Services.Db.Readers {

    public class PostPoolReader : IDataReader<PostPool> {

        public override PostPool? ReadEntry(NpgsqlDataReader reader) {
            PostPool pool = new();

            pool.ID = reader.GetUInt64("id");
            pool.Name = reader.GetString("name");
            pool.CreatedByID = reader.GetUInt64("created_by_id");
            pool.Timestamp = reader.GetDateTime("timestamp");

            return pool;
        }

    }
}
