using honooru.Models.App;
using honooru.Services.Db;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
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

        public Task<List<TagImplication>> GetBySourceTagID(ulong tagID) {
            return _TagImplicationDb.GetBySourceTagID(tagID);
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
