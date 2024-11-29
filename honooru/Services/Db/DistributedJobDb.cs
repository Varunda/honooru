using honooru.Code.ExtensionMethods;
using honooru_common.Models;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.ObjectiveC;
using System.Text.Json;
using System.Threading.Tasks;

namespace honooru.Services.Db {

    public class DistributedJobDb {

        private readonly ILogger<DistributedJobDb> _Logger;
        private readonly IDbHelper _DbHelper;
        private readonly IDataReader<DistributedJob> _Reader;

        public DistributedJobDb(ILogger<DistributedJobDb> logger,
            IDbHelper dbHelper, IDataReader<DistributedJob> reader) {

            _Logger = logger;
            _DbHelper = dbHelper;
            _Reader = reader;
        }

        /// <summary>
        ///     get a single <see cref="DistributedJob"/> by it's <see cref="DistributedJob.ID"/>
        /// </summary>
        /// <param name="ID"></param>
        /// <returns></returns>
        public async Task<DistributedJob?> GetByID(Guid ID) {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                SELECT *
                    FROM distributed_job
                    WHERE id = @ID;
            ");

            cmd.AddParameter("ID", ID);
            await cmd.PrepareAsync();

            DistributedJob? job = await _Reader.ReadSingle(cmd);
            await conn.CloseAsync();

            return job;
        }

        /// <summary>
        ///     get a list of all <see cref="DistributedJob"/>s
        /// </summary>
        /// <returns></returns>
        public async Task<List<DistributedJob>> GetAll() {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                SELECT *
                    FROM distributed_job;
            ");

            await cmd.PrepareAsync();

            List<DistributedJob> jobs = await _Reader.ReadList(cmd);
            await conn.CloseAsync();

            return jobs;
        }

        /// <summary>
        ///     get all jobs that are unclaimed (which have <see cref="DistributedJob.ClaimedAt"/> of null)
        /// </summary>
        /// <returns></returns>
        public async Task<List<DistributedJob>> GetUnclaimed() {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                SELECT *
                    FROM distributed_job
                    WHERE claimed_at IS NULL;
            ");

            await cmd.PrepareAsync();

            List<DistributedJob> jobs = await _Reader.ReadList(cmd);
            await conn.CloseAsync();

            return jobs;
        }

        /// <summary>
        ///     update or insert (upsert) a <see cref="DistributedJob"/>
        /// </summary>
        /// <param name="job"></param>
        /// <returns></returns>
        public async Task Upsert(DistributedJob job) {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                INSERT INTO distributed_job (
                    id, type, done, claimed_by_user_id, claimed_at, last_progress_update, values
                ) VALUES (
                    @ID, @Type, @Done, @ClaimedByUserID, @ClaimedAt, @LastProgressUpdate, @Values
                ) ON CONFLICT (id) DO UPDATE 
                    SET type = @Type,
                        done = @Done,
                        claimed_by_user_id = @ClaimedByUserID,
                        claimed_at = @ClaimedAt,
                        last_progress_update = @LastProgressUpdate,
                        values = @Values;
            ");

            cmd.AddParameter("ID", job.ID);
            cmd.AddParameter("Type", job.Type);
            cmd.AddParameter("Done", job.Done);
            cmd.AddParameter("ClaimedByUserID", job.ClaimedByUserID);
            cmd.AddParameter("ClaimedAt", job.ClaimedAt);
            cmd.AddParameter("LastProgressUpdate", job.LastProgressUpdate);
            cmd.AddParameter("Values", JsonSerializer.SerializeToElement(job.Values, JsonSerializerOptions.Default));
            await cmd.PrepareAsync();

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

        /// <summary>
        ///     delete a <see cref="DistributedJob"/>
        /// </summary>
        /// <param name="ID"></param>
        /// <returns></returns>
        public async Task Delete(Guid ID) {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                DELETE FROM distributed_job
                    WHERE id = @ID;
            ");

            cmd.AddParameter("ID", ID);
            await cmd.PrepareAsync();

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
