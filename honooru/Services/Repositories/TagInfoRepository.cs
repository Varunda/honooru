using honooru.Models.App;
using honooru.Services.Db;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace honooru.Services.Repositories {

    public class TagInfoRepository {

        private readonly ILogger<TagInfoRepository> _Logger;

        private readonly TagInfoDb _TagInfoDb;

        public TagInfoRepository(ILogger<TagInfoRepository> logger,
            TagInfoDb tagInfoDb) {

            _Logger = logger;
            _TagInfoDb = tagInfoDb;
        }

        public Task<TagInfo?> GetByID(ulong tagID) {
            return _TagInfoDb.GetByID(tagID);
        }

        public async Task<List<TagInfo>> GetByIDs(IEnumerable<ulong> ids) {
            List<TagInfo> infos = new(ids.Count());
            foreach (ulong tagId in ids) {
                TagInfo? i = await GetByID(tagId);
                if (i != null) {
                    infos.Add(i);
                }
            }

            return infos;
        }

        public Task Upsert(TagInfo info) {
            return _TagInfoDb.Upsert(info);
        }

    }

    public static class TagInfoRepositoryExtensionMethods {

        public static async Task<TagInfo> GetOrCreateByID(this TagInfoRepository repo, ulong tagID) {
            return (await repo.GetByID(tagID)) ?? new TagInfo() {
                ID = tagID,
                Uses = 0,
                Description = null
            };
        }

    }


}
