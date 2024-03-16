using Npgsql;
using System.Data;
using honooru.Code.ExtensionMethods;
using honooru.Models;

namespace honooru.Services.Db.Readers {

    public class AppAccountReader : IDataReader<AppAccount> {

        public override AppAccount? ReadEntry(NpgsqlDataReader reader) {
            AppAccount acc = new();

            acc.ID = reader.GetUInt64("id");
            acc.Name = reader.GetString("name");
            acc.Timestamp = reader.GetDateTime("timestamp");
            acc.DiscordID = reader.GetUInt64("discord_id");
            acc.DeletedOn = reader.GetNullableDateTime("deleted_on");
            acc.DeletedBy = reader.GetNullableInt64("deleted_by");

            return acc;
        }

    }
}
