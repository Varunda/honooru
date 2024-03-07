using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using honooru.Code.ExtensionMethods;
using honooru.Models.Db;

namespace honooru.Services.Db.Readers {

    public class ExpEventReader : IDataReader<ExpEvent> {

        public override ExpEvent ReadEntry(NpgsqlDataReader reader) {
            ExpEvent ev = new ExpEvent();

            ev.ID = reader.GetInt64("id");

            return ev;
        }

    }
}
