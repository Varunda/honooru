using honooru.Models.App;
using honooru.Models.Db;
using honooru.Services.Db;
using honooru.Services.Util;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace honooru.Services.Repositories {

    public class PostPoolEntryRepository {

        private readonly ILogger<PostPoolEntryRepository> _Logger;
        private readonly PostPoolEntryDb _Db;
        private readonly IMemoryCache _Cache;

        private readonly UserSearchCache _SearchCache;

        public PostPoolEntryRepository(ILogger<PostPoolEntryRepository> logger,
            IMemoryCache cache, PostPoolEntryDb db,
            UserSearchCache searchCache) {

            _Logger = logger;
            _Cache = cache;
            _Db = db;

            _SearchCache = searchCache;
        }

        public Task<List<PostPoolEntry>> GetByPoolID(ulong poolID) {
            return _Db.GetByPoolID(poolID);
        }

        public Task<List<PostPoolEntry>> GetByPostID(ulong postID) {
            return _Db.GetByPostID(postID);
        }

        public Task Insert(PostPoolEntry entry) {
            _SearchCache.Clear();
            return _Db.Insert(entry);
        }

        public Task Delete(PostPoolEntry entry) {
            _SearchCache.Clear();
            return _Db.Delete(entry);
        }

        /// <summary>
        ///     delete all <see cref="PostPoolEntry"/>s in a <see cref="PostPool"/>
        /// </summary>
        /// <param name="poolID">ID of the <see cref="PostPool"/> to remove all posts from</param>
        /// <returns></returns>
        public async Task DeleteByPoolID(ulong poolID) {
            _SearchCache.Clear();
            List<PostPoolEntry> entries = await GetByPoolID(poolID);

            foreach (PostPoolEntry entry in entries) {
                await _Db.Delete(entry);
            }
        }

        /// <summary>
        ///     check if a post is within a <see cref="PostPool"/>
        /// </summary>
        /// <param name="poolID">ID of the <see cref="PostPool"/></param>
        /// <param name="postID">ID of the <see cref="Post"/> to check if its in the pool</param>
        /// <returns></returns>
        public async Task<bool> IsPostInPool(ulong poolID, ulong postID) {
            List<PostPoolEntry> poolEntries = await GetByPoolID(poolID);

            return poolEntries.Find(iter => iter.PostID == postID) != null;
        }

    }
}
