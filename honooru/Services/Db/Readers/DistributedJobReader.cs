using honooru.Code.ExtensionMethods;
using honooru_common.Models;
using Npgsql;
using System.Collections.Generic;
using System.Data;
using System.Text.Json;

namespace honooru.Services.Db.Readers {

    public class DistributedJobReader : IDataReader<DistributedJob> {

        public override DistributedJob? ReadEntry(NpgsqlDataReader reader) {
            DistributedJob job = new();

            job.ID = reader.GetGuid("id");
            job.Type = reader.GetString("type");
            job.Done = reader.GetBoolean("done");
            job.ClaimedByUserID = reader.GetNullableUInt64("claimed_by_user_id");
            job.ClaimedAt = reader.GetNullableDateTime("claimed_at");
            job.LastProgressUpdate = reader.GetNullableDateTime("last_progress_update");
            job.Values = JsonSerializer.Deserialize<Dictionary<string, string>>(reader.GetString("values"))
                ?? throw new System.Exception($"failed to get a valid JSON dictionary from values from ID {job.ID}");

            return job;
        }

    }
}
