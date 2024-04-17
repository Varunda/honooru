using honooru.Models.App;
using honooru.Services.Db;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.RateLimiting;
using System.Threading.Tasks;

namespace honooru.Services.Repositories {

    public class PostPoolRepository {

        private readonly ILogger<PostPoolRepository> _Logger;
        private readonly PostPoolDb _PoolDb;
        private readonly IMemoryCache _Cache;

        private const string CACHE_KEY_ALL = "PostPool.All";

        public PostPoolRepository(ILogger<PostPoolRepository> logger,
            PostPoolDb poolDb, IMemoryCache cache) {

            _Logger = logger;
            _PoolDb = poolDb;
            _Cache = cache;
        }

        public async Task<List<PostPool>> GetAll() {
            return [.. (await _GetDictionary()).Values];
        }

        public async Task<PostPool?> GetByID(ulong poolID) {
            return (await _GetDictionary()).GetValueOrDefault(poolID);
        }

        private async Task<Dictionary<ulong, PostPool>> _GetDictionary() {
            if (_Cache.TryGetValue(CACHE_KEY_ALL, out Dictionary<ulong, PostPool>? dict) || dict == null) {
                List<PostPool> pools = await _PoolDb.GetAll();

                dict = new Dictionary<ulong, PostPool>();
                dict = pools.ToDictionary(iter => iter.ID);

                _Cache.Set(CACHE_KEY_ALL, dict, new MemoryCacheEntryOptions() {
                    SlidingExpiration = TimeSpan.FromMinutes(15)
                });
            }

            return dict;
        }

        public Task<ulong> Insert(PostPool pool) {
            _Cache.Remove(CACHE_KEY_ALL);
            return _PoolDb.Insert(pool);
        }

        public Task Update(ulong poolID, PostPool pool) {
            _Cache.Remove(CACHE_KEY_ALL);
            return _PoolDb.Update(poolID, pool);
        }

        public Task Delete(ulong poolID) {
            _Cache.Remove(CACHE_KEY_ALL);
            return _PoolDb.Delete(poolID);
        }

    }
}
