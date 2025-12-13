using honooru.Models.Db;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace honooru.Services.Util {

    public class UserSearchCache {

        private readonly ILogger<UserSearchCache> _Logger;

        private readonly IMemoryCache _Cache;

        private const string CACHE_KEY_SEARCH = "Post.Search.{0}.{1}"; // {0} => user ID, {1} => hash key

        private readonly HashSet<string> _CachedKeys = new();

        public UserSearchCache(ILogger<UserSearchCache> logger,
            IMemoryCache cache) {

            _Logger = logger;
            _Cache = cache;
        }

        public bool TryGet(ulong userID, string hash, out List<Post>? posts) {
            // the whole result of a DB search is cached, and the offset//limit are taken after
            string cacheKey = string.Format(CACHE_KEY_SEARCH, userID, hash);
            return _Cache.TryGetValue(cacheKey, out posts);
        }

        public void Add(ulong userID, string hash, List<Post> posts) {
            // mark this key as cached, so when a post is updated, the cached data can be evicted
            string cacheKey = string.Format(CACHE_KEY_SEARCH, userID, hash);
            _CachedKeys.Add(cacheKey);
            _Cache.Set(cacheKey, posts, new MemoryCacheEntryOptions() {
                SlidingExpiration = TimeSpan.FromMinutes(5)
            });
        }

        public void Clear() {
            _Logger.LogDebug($"removing old search queries due to post update [_CachedKeys.Count={_CachedKeys.Count}]");

            int count = 0;
            int had = 0;
            int missing = 0;

            foreach (string key in _CachedKeys) {
                ++count;

                bool has = _Cache.TryGetValue(key, out _);
                if (has == true) {
                    ++had;
                } else {
                    ++missing;
                }

                _Cache.Remove(key);
            }

            _Logger.LogInformation($"removed old cached search query results [count={count}] [had={had}] [missing={missing}]");
        }

    }
}
