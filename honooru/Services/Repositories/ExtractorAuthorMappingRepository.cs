using Google.Apis.Util;
using honooru.Models.App;
using honooru.Services.Db;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace honooru.Services.Repositories {

    public class ExtractorAuthorMappingRepository {

        private readonly ILogger<ExtractorAuthorMappingRepository> _Logger;
        private readonly ExtractorAuthorMappingDb _AuthorMappingDb;

        public ExtractorAuthorMappingRepository(ILogger<ExtractorAuthorMappingRepository> logger,
            ExtractorAuthorMappingDb authorMappingDb) {

            _Logger = logger;

            _AuthorMappingDb = authorMappingDb;
        }

        public Task<List<ExtractorAuthorMapping>> GetAll() {
            return _AuthorMappingDb.GetAll();
        }

        public async Task<ExtractorAuthorMapping?> GetMapping(string site, string author) {
            List<ExtractorAuthorMapping> mappings = await _AuthorMappingDb.GetAll();

            foreach (ExtractorAuthorMapping mapping in mappings) {
                if (mapping.Site == site && mapping.Author == author) {
                    return mapping;
                }
            }

            return null;
        }

        public Task Upsert(ExtractorAuthorMapping mapping) {
            return _AuthorMappingDb.Upsert(mapping);
        }

        public Task Delete(ExtractorAuthorMapping mapping) {
            return _AuthorMappingDb.Delete(mapping);
        }

    }
}
