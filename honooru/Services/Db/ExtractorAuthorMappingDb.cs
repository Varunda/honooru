using honooru.Code.ExtensionMethods;
using honooru.Models.App;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace honooru.Services.Db {

    public class ExtractorAuthorMappingDb {

        private readonly ILogger<ExtractorAuthorMappingDb> _Logger;
        private readonly IDataReader<ExtractorAuthorMapping> _Reader;
        private readonly IDbHelper _DbHelper;

        public ExtractorAuthorMappingDb(ILogger<ExtractorAuthorMappingDb> logger,
            IDataReader<ExtractorAuthorMapping> reader, IDbHelper dbHelper) {

            _Logger = logger;

            _Reader = reader;
            _DbHelper = dbHelper;
        }

        public async Task<List<ExtractorAuthorMapping>> GetAll() {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                SELECT *
                    FROM extractor_author_mapping;
            ");

            List<ExtractorAuthorMapping> mappings = await _Reader.ReadList(cmd);
            await conn.CloseAsync();

            return mappings;
        }

        public async Task Upsert(ExtractorAuthorMapping mapping) {
            if (string.IsNullOrEmpty(mapping.Site)) { throw new ArgumentException($"{nameof(ExtractorAuthorMapping.Site)} cannot be empty"); }
            if (string.IsNullOrEmpty(mapping.Author)) { throw new ArgumentException($"{nameof(ExtractorAuthorMapping.Author)} cannot be empty"); }
            if (mapping.TagID == 0) { throw new ArgumentException($"{nameof(ExtractorAuthorMapping.TagID)} cannot be 0"); }

            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                INSERT INTO extractor_author_mapping (
                    site, author, tag_id, timestamp
                ) VALUES (
                    @Site, @Author, @TagID, @Timestamp
                ) ON CONFLICT (site, author) DO
                    UPDATE set tag_id = @TagID,
                        timestamp = @Timestamp;
            ");

            cmd.AddParameter("Site", mapping.Site);
            cmd.AddParameter("Author", mapping.Author);
            cmd.AddParameter("TagID", mapping.TagID);
            cmd.AddParameter("Timestamp", mapping.Timestamp);
            await cmd.PrepareAsync();

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

        public async Task Delete(ExtractorAuthorMapping mapping) {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                DELETE FROM
                    extractor_author_mapping
                WHERE
                    site = @Site
                    AND author = @Author;
            ");

            cmd.AddParameter("Site", mapping.Site);
            cmd.AddParameter("Author", mapping.Author);
            await cmd.PrepareAsync();

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
