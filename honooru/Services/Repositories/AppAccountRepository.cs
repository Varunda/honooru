using honooru.Models;
using honooru.Services.Db;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace honooru.Services.Repositories {

    public class AppAccountRepository {

        private readonly ILogger<AppAccountRepository> _Logger;
        private readonly AppAccountDbStore _AccountDb;

        private readonly IMemoryCache _Cache;

        public AppAccountRepository(ILogger<AppAccountRepository> logger,
            AppAccountDbStore accountDb, IMemoryCache cache) {

            _Logger = logger;
            
            _AccountDb = accountDb;
            _Cache = cache;
        }

        public Task<AppAccount?> GetByID(ulong accountID, CancellationToken cancel) {
            return _AccountDb.GetByID(accountID, cancel);
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
