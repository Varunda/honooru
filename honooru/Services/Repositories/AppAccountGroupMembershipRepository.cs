using honooru.Models.Internal;
using honooru.Services.Db;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace honooru.Services.Repositories {

    public class AppAccountGroupMembershipRepository {

        private readonly ILogger<AppAccountGroupMembershipRepository> _Logger;
        private readonly AppAccountGroupMembershipDbStore _MembershipDb;

        public AppAccountGroupMembershipRepository(ILogger<AppAccountGroupMembershipRepository> logger,
            AppAccountGroupMembershipDbStore membershipDb) {

            _Logger = logger;
            _MembershipDb = membershipDb;
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

    }
}
