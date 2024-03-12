using honooru.Models.App;
using honooru.Services.Db;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace honooru.Services.Repositories {

    public class TagTypeRepository {

        private readonly ILogger<TagTypeRepository> _Logger;

        private readonly TagTypeDb _TagTypeDb;

        public TagTypeRepository(ILogger<TagTypeRepository> logger,
            TagTypeDb tagTypeDb) {

            _Logger = logger;
            _TagTypeDb = tagTypeDb;
        }

        public Task<TagType?> GetByID(ulong typeID) {
            return _TagTypeDb.GetByID(typeID);
        }

        public async Task<List<TagType>> GetByIDs(IEnumerable<ulong> ids) {
            List<TagType> types = new();

            foreach (ulong id in ids) {
                TagType? type = await GetByID(id);
                if (type != null) {
                    types.Add(type);
                }
            }

            return types;
        }

    }
}
