using honooru.Models.Internal;
using honooru.Services.Db;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace honooru.Services.Repositories {

    public class AppAccountGroupMembershipRepository {

        private readonly ILogger<AppAccountGroupMembershipRepository> _Logger;
        private readonly AppAccountGroupMembershipDbStore _MembershipDb;

        private readonly IMemoryCache _Cache;

        private const string CACHE_KEY_ACCOUNT = "App.AccountGroupMembership.Account.{0}"; // {0} => account ID

        public AppAccountGroupMembershipRepository(ILogger<AppAccountGroupMembershipRepository> logger,
            AppAccountGroupMembershipDbStore membershipDb, IMemoryCache cache) {

            _Logger = logger;
            _MembershipDb = membershipDb;
            _Cache = cache;
        }

        public Task<AppAccountGroupMembership?> GetByID(ulong ID) {
            return _MembershipDb.GetByID(ID);
        }

        public Task<List<AppAccountGroupMembership>> GetByAccountID(ulong accountID) {
            return _MembershipDb.GetByAccountID(accountID);
        }

        public Task<List<AppAccountGroupMembership>> GetByGroupID(ulong groupID) {
            return _MembershipDb.GetByGroupID(groupID);
        }

        public Task<ulong> Insert(AppAccountGroupMembership membership) {
            return _MembershipDb.Insert(membership);
        }

        public Task Delete(AppAccountGroupMembership membership) {
            return _MembershipDb.Delete(membership);
        }

    }
}
