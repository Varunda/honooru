using Npgsql;
using System.Data;
using honooru.Code.ExtensionMethods;
using honooru.Models.Internal;

namespace honooru.Services.Db.Readers {

    public class UnsafeAppAccountReader : IDataReader<UnsafeAppAccount> {

        public override UnsafeAppAccount? ReadEntry(NpgsqlDataReader reader) {
            UnsafeAppAccount acc = new();

            acc.ID = reader.GetInt64("id");
            acc.Name = reader.GetString("name");
            acc.Timestamp = reader.GetDateTime("timestamp");
            acc.Email = reader.GetString("email");
            acc.DiscordID = reader.GetUInt64("discord_id");
            acc.DeletedOn = reader.GetNullableDateTime("deleted_on");
            acc.DeletedBy = reader.GetNullableInt64("deleted_by");

            return acc;
        }

    }
}
