using honooru.Models.App;
using honooru.Services.Db;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace honooru.Services.Repositories {

    public class TagInfoRepository {

        private readonly ILogger<TagInfoRepository> _Logger;

        private readonly TagInfoDb _TagInfoDb;

        private readonly IMemoryCache _Cache;

        private const string CACHE_KEY_ID = "TagInfo.{0}"; // {0} => tag ID

        public TagInfoRepository(ILogger<TagInfoRepository> logger,
            TagInfoDb tagInfoDb, IMemoryCache cache) {

            _Logger = logger;
            _TagInfoDb = tagInfoDb;
            _Cache = cache;
        }

        public async Task<TagInfo?> GetByID(ulong tagID) {
            string cacheKey = string.Format(CACHE_KEY_ID, tagID);
            if (_Cache.TryGetValue(cacheKey, out TagInfo? info) == false) {
                info = await _TagInfoDb.GetByID(tagID);
                _Cache.Set(cacheKey, info, new MemoryCacheEntryOptions() {
                    SlidingExpiration = TimeSpan.FromMinutes(10)
                });
            }

            return info;
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
            string cacheKey = string.Format(CACHE_KEY_ID, info.ID);
            _Cache.Remove(cacheKey);

            return _TagInfoDb.Upsert(info);
        }

        public Task Delete(ulong tagID) {
            string cacheKey = string.Format(CACHE_KEY_ID, tagID);
            _Cache.Remove(cacheKey);

            return _TagInfoDb.Delete(tagID);
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
