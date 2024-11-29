using honooru.Code.ExtensionMethods;
using honooru.Models.App;
using Npgsql;
using System.Data;

namespace honooru.Services.Db.Readers {

    public class ApiKeyReader : IDataReader<ApiKey> {

        public override ApiKey? ReadEntry(NpgsqlDataReader reader) {
            ApiKey key = new();

            key.UserID = reader.GetUInt64("user_id");
            key.ClientSecret = reader.GetString("client_secret");
            key.Timestamp = reader.GetDateTime("timestamp");

            return key;
        }

    }
}
