using honooru.Models.App;
using honooru.Services.Db;
using honooru.Services.Util;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace honooru.Services.Repositories {

    public class UserSettingRepository {

        private readonly ILogger<UserSettingRepository> _Logger;

        private readonly UserSettingDb _UserSettingDb;
        private readonly UserSearchCache _SearchCache;

        private readonly IMemoryCache _Cache;

        private const string CACHE_KEY = "UserSettings.{0}"; // {0} => account ID

        private static Dictionary<string, UserSettingType> _UserSettingNameTypeMapping = new() {
            { "postings.explicit.behavior", UserSettingType.STRING },
            { "postings.unsafe.behavior", UserSettingType.STRING },
            { "postings.count", UserSettingType.INTEGER },
            { "postings.sizing", UserSettingType.STRING }
        };

        public UserSettingRepository(ILogger<UserSettingRepository> logger,
            UserSettingDb userSettingDb, IMemoryCache cache,
            UserSearchCache searchCache) {

            _Logger = logger;
            _UserSettingDb = userSettingDb;
            _Cache = cache;
            _SearchCache = searchCache;
        }

        /// <summary>
        ///     get all <see cref="UserSetting"/>s for a specific account
        /// </summary>
        /// <param name="accountID">ID of the account to get the settings of</param>
        /// <returns></returns>
        public async Task<List<UserSetting>> GetByAccountID(ulong accountID) {
            string cacheKey = string.Format(CACHE_KEY, accountID);

            if (_Cache.TryGetValue(cacheKey, out List<UserSetting>? settings) == false || settings == null) {
                settings = await _UserSettingDb.GetByAccountID(accountID);

                _Cache.Set(cacheKey, settings, new MemoryCacheEntryOptions() {
                    SlidingExpiration = TimeSpan.FromMinutes(30)
                });
            }

            return settings;
        }

        public async Task<UserSetting?> GetByAccountIDAndName(ulong accountID, string name) {
            return (await GetByAccountID(accountID)).FirstOrDefault(iter => iter.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public UserSettingType? GetTypeMapping(string name) {
            name = name.ToLower();
            if (_UserSettingNameTypeMapping.ContainsKey(name) == false) {
                return null;
            }

            return _UserSettingNameTypeMapping.GetValueOrDefault(name);
        }

        public Task Upsert(UserSetting setting) {
            string cacheKey = string.Format(CACHE_KEY, setting.AccountID);
            _Cache.Remove(cacheKey);

            _SearchCache.Clear();

            return _UserSettingDb.Upsert(setting);
        }

    }
}
