using honooru.Models.App;
using honooru.Services.Db;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace honooru.Services.Repositories {

    public class TagImplicationRepository {

        private readonly ILogger<TagImplicationRepository> _Logger;
        private readonly TagImplicationDb _TagImplicationDb;

        public TagImplicationRepository(ILogger<TagImplicationRepository> logger,
            TagImplicationDb tagImplicationDb) {

            _Logger = logger;
            _TagImplicationDb = tagImplicationDb;
        }

        public Task<List<TagImplication>> GetAll() {
            return _TagImplicationDb.GetAll();
        }

        public async Task<List<TagImplication>> GetBySourceTagID(ulong tagID) {
            Queue<ulong> getImplies = [];
            HashSet<ulong> found = [];
            List<TagImplication> implies = [];

            getImplies.Enqueue(tagID);

            while (getImplies.Count > 0) {
                ulong iterID = getImplies.Dequeue();

                List<TagImplication> imps = await _TagImplicationDb.GetBySourceTagID(iterID);

                _Logger.LogDebug($"getting tag implications [tagID={tagID}] [iterID={iterID}] [imps.Count={imps.Count}] "
                    + $"[imps={string.Join(", ", imps.Select(iter => $"{iter.TagA}->{iter.TagB}"))}");

                foreach (TagImplication imp in imps) {
                    if (found.Contains(imp.TagB)) {
                        continue;
                    }

                    implies.Add(imp);
                    getImplies.Enqueue(imp.TagB);
                    found.Add(imp.TagB);
                }
            }

            return implies;
        }

        public Task<List<TagImplication>> GetByTargetTagID(ulong tagID) {
            return _TagImplicationDb.GetByTargetTagID(tagID);
        }

        public Task Insert(TagImplication implication) {
            return _TagImplicationDb.Insert(implication);
        }

        public Task Delete(TagImplication implication) {
            return _TagImplicationDb.Delete(implication);
        }

    }
}
