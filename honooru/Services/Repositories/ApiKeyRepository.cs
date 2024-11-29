using honooru.Models.App;
using honooru.Services.Db;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace honooru.Services.Repositories {

    public class ApiKeyRepository {

        private readonly ILogger<ApiKeyRepository> _Logger;
        private readonly IMemoryCache _Cache;
        private readonly ApiKeyDb _Db;

        private const string CACHE_KEY = "ApiKey.{0}"; // {0} => user ID

        public ApiKeyRepository(ILogger<ApiKeyRepository> logger,
            IMemoryCache cache, ApiKeyDb db) {

            _Logger = logger;
            _Cache = cache;
            _Db = db;
        }

        public async Task<ApiKey?> GetByUserID(ulong userID) {
            string cacheKey = string.Format(CACHE_KEY, userID);

            if (_Cache.TryGetValue(cacheKey, out ApiKey? key) == false || key == null) {
                key = await _Db.GetByUserID(userID);

                _Cache.Set(cacheKey, key, new MemoryCacheEntryOptions() {
                    SlidingExpiration = TimeSpan.FromMinutes(5)
                });
            }

            return key;
        }

        public Task Create(ulong userID) {
            _Cache.Remove(string.Format(CACHE_KEY, userID));

            return _Db.Create(userID);
        }

        public Task Delete(ulong userID) {
            _Cache.Remove(string.Format(CACHE_KEY, userID));

            return _Db.Delete(userID);
        }

    }
}
