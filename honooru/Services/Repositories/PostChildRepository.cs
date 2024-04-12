using honooru.Models.App;
using honooru.Services.Db;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace honooru.Services.Repositories {

    public class PostChildRepository {

        private readonly ILogger<PostChildRepository> _Logger;
        private readonly PostChildDb _PostChildDb;
        private readonly IMemoryCache _Cache;

        public PostChildRepository(ILogger<PostChildRepository> logger,
            PostChildDb postChildDb, IMemoryCache cache) {

            _Logger = logger;
            _PostChildDb = postChildDb;
            _Cache = cache;
        }

        public Task<List<PostChild>> GetByParentID(ulong parentID) {
            return _PostChildDb.GetByParentID(parentID);
        }

        public Task<List<PostChild>> GetByChildID(ulong childID) {
            return _PostChildDb.GetByChildID(childID);
        }

        public Task Insert(PostChild child) {
            return _PostChildDb.Insert(child);
        }

        public Task Remove(PostChild child) {
            return _PostChildDb.Remove(child);
        }

    }
}
