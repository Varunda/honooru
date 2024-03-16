using honooru.Models.Internal;
using honooru.Services.Db;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace honooru.Services.Repositories {

    public class AppGroupRepository {

        private readonly ILogger<AppGroupRepository> _Logger;
        private readonly AppGroupDbStore _AppGroupDb;

        public AppGroupRepository(ILogger<AppGroupRepository> logger,
            AppGroupDbStore appGroupDb) {

            _Logger = logger;
            _AppGroupDb = appGroupDb;
        }

        public Task<List<AppGroup>> GetAll() {
            return _AppGroupDb.GetAll();
        }

        public Task<AppGroup?> GetByID(ulong groupID) {
            return _AppGroupDb.GetByID(groupID);
        }

    }
}
