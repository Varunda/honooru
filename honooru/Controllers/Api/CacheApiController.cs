using honooru.Code;
using honooru.Models;
using honooru.Models.Internal;
using honooru.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace honooru.Controllers.Api {

    [ApiController]
    [Route("/api/cache")]
    public class CacheApiController : ApiControllerBase {

        private readonly ILogger<CacheApiController> _Logger;
        private readonly IMemoryCache _Cache;
        private readonly bool _HasAppCache = true;

        private AppCache? _Get() {
            if (_HasAppCache == false) {
                return null;
            }

            return _Cache as AppCache;
        }

        public CacheApiController(ILogger<CacheApiController> logger,
            IMemoryCache cache) {

            _Logger = logger;
            _Cache = cache;

            if (_Cache is not AppCache) {
                _Logger.LogWarning($"passed cache is not an {nameof(AppCache)} [cache.Type={cache.GetType().FullName}]");
                _HasAppCache = false;
            }
        }

        /// <summary>
        ///     get all cached keys
        /// </summary>
        /// <response code="200">
        ///     the response will return an array of strings that represent the cache key for each item in the cache
        /// </response>
        [HttpGet]
        [PermissionNeeded(AppPermission.APP_ACCOUNT_ADMIN)]
        public ApiResponse<List<string>> GetKeys() {
            AppCache? cache = _Get();
            if (_HasAppCache == false || cache == null) {
                return ApiInternalError<List<string>>($"cache is not a {nameof(AppCache)}, cannot perform any operation");
            }

            HashSet<string> keys = cache.GetTrackedKeys();

            List<string> keysList = new(keys);

            return ApiOk(keysList);
        }

        /// <summary>
        ///     get the cached value based on a key
        /// </summary>
        /// <param name="key">key to get from the cache</param>
        /// <response code="200">
        ///     the respones will contain a JSON string that represents the cached value 
        /// </response>
        /// <response code="204">
        ///     no entry within the cache has the key of <paramref name="key"/>
        /// </response>
        [HttpGet("{key}")]
        [PermissionNeeded(AppPermission.APP_ACCOUNT_ADMIN)]
        public ApiResponse<string> GetValue(string key) {
            if (_Cache.TryGetValue(key, out object? value) == false || value == null) {
                return ApiNoContent<string>();
            }

            string json = JsonSerializer.Serialize(value);
            return ApiOk(json);
        }

        /// <summary>
        ///     get the <see cref="CacheEntryMetadata"/> of the cached item
        /// </summary>
        /// <param name="key">key of the cache item</param>
        /// <response code="200">
        ///     the response will contain the <see cref="CacheEntryMetadata"/> of the cache key
        /// </response>
        /// <response code="204">
        ///     no entry within the cache has the key of <paramref name="key"/>
        /// </response>
        [HttpGet("{key}/meta")]
        [PermissionNeeded(AppPermission.APP_ACCOUNT_ADMIN)]
        public ApiResponse<CacheEntryMetadata> GetMetadata(string key) {
            AppCache? cache = _Get();
            if (_HasAppCache == false || cache == null) {
                return ApiInternalError<CacheEntryMetadata>($"cache is not a {nameof(AppCache)}, cannot perform any operation");
            }

            CacheEntryMetadata? meta = cache.GetMetadata(key);
            if (meta == null) {
                return ApiNoContent<CacheEntryMetadata>();
            }

            return ApiOk(meta);
        }

        /// <summary>
        ///     force evict an item from cache
        /// </summary>
        /// <param name="key">key to evict</param>
        /// <response code="200">
        ///     the response was evicted from cache, even if it might not have been cached
        /// </response>
        [HttpDelete("{key}")]
        [PermissionNeeded(AppPermission.APP_ACCOUNT_ADMIN)]
        public ApiResponse Evict(string key) {
            _Logger.LogInformation($"evicing key from cache [key={key}]");
            _Cache.Remove(key);

            return ApiOk();
        }

        [HttpGet("stats")]
        [PermissionNeeded(AppPermission.APP_ACCOUNT_ADMIN)]
        public ApiResponse<MemoryCacheStatistics> GetStats() {
            AppCache? cache = _Get();
            if (_HasAppCache == false || cache == null) {
                return ApiInternalError<MemoryCacheStatistics>($"cache is not a {nameof(AppCache)}, cannot perform any operation");

            }

            MemoryCacheStatistics? stats = cache.GetCurrentStatistics();
            if (stats == null) {
                return ApiNoContent<MemoryCacheStatistics>();
            }

            return ApiOk(stats);
        }

    }
}
