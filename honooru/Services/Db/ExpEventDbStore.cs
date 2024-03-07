using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using honooru.Code.ExtensionMethods;
using honooru.Code.Tracking;
using honooru.Models.Db;

namespace honooru.Services.Db {

    public class ExpEventDbStore {

        private readonly ILogger<ExpEventDbStore> _Logger;
        private readonly IDbHelper _DbHelper;

        private readonly IDataReader<ExpEvent> _ExpDataReader;

        public ExpEventDbStore(ILogger<ExpEventDbStore> logger,
            IDbHelper dbHelper, IDataReader<ExpEvent> expReader) {

            _Logger = logger;
            _DbHelper = dbHelper;

            _ExpDataReader = expReader ?? throw new ArgumentNullException(nameof(expReader));
        }

        /// <summary>
        ///     Load wrapped exp data for a character in a year
        /// </summary>
        /// <param name="charID"></param>
        /// <param name="year"></param>
        /// <returns></returns>
        public async Task<List<ExpEvent>> LoadWrapped(string charID, DateTime year) {
            string db = $"wrapped_{year:yyyy}";

            using NpgsqlConnection conn = _DbHelper.Connection(db);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, $@"
                SELECT *
                    from wt_exp_{year:yyyy}
                    WHERE source_character_id = @CharID;
            ");

            cmd.AddParameter("CharID", charID);

            List<ExpEvent> evs = await _ExpDataReader.ReadList(cmd);
            await conn.CloseAsync();

            return evs;
        }

    }

}
