using honooru.Models;
using honooru.Services.Db;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace honooru.Services.Repositories {

    public class AppAccountRepository {

        private readonly ILogger<AppAccountRepository> _Logger;
        private readonly AppAccountDbStore _AccountDb;

        private readonly IMemoryCache _Cache;

        private const string CACHE_KEY_ID = "Account.ID.{0}";

        private const string CACHE_KEY_DISCORD = "Account.Discord.{0}";

        public AppAccountRepository(ILogger<AppAccountRepository> logger,
            AppAccountDbStore accountDb, IMemoryCache cache) {

            _Logger = logger;
            
            _AccountDb = accountDb;
            _Cache = cache;
        }

        public async Task<AppAccount?> GetByID(ulong accountID, CancellationToken cancel = default) {
            string cacheKey = string.Format(CACHE_KEY_ID, accountID);

            if (_Cache.TryGetValue(cacheKey, out AppAccount? acc) == false) {
                acc = await _AccountDb.GetByID(accountID, cancel);

                _Cache.Set(cacheKey, acc, new MemoryCacheEntryOptions() {
                    SlidingExpiration = TimeSpan.FromMinutes(10)
                });
            }

            return acc;
        }

        public async Task<AppAccount?> GetByDiscordID(ulong discordID, CancellationToken cancel = default) {
            string cacheKey = string.Format(CACHE_KEY_DISCORD, discordID);

            if (_Cache.TryGetValue(cacheKey, out AppAccount? acc) == false) {
                acc = await _AccountDb.GetByDiscordID(discordID, cancel);

                _Cache.Set(cacheKey, acc, new MemoryCacheEntryOptions() {
                    SlidingExpiration = TimeSpan.FromMinutes(10)
                });
            }

            return acc;
        }

        /// <summary>
        ///     get a single <see cref="AppAccount"/> by <see cref="AppAccount.Name"/> (case-insensitive)
        /// </summary>
        /// <param name="name">name of the <see cref="AppAccount"/> to get</param>
        /// <param name="cancel">cancellation token</param>
        /// <returns>
        ///     the <see cref="AppAccount"/> with <see cref="AppAccount.Name"/> of <paramref name="name"/>,
        ///     or <c>null</c> if it does not exists (case-insensitive)
        /// </returns>
        public async Task<AppAccount?> GetByName(string name, CancellationToken cancel) {
            List<AppAccount> accounts = await _AccountDb.GetAll(cancel);

            foreach (AppAccount acc in accounts) {
                if (acc.Name.ToLower() == name.ToLower()) {
                    return acc;
                }
            }

            return null;
        }

    }
}
