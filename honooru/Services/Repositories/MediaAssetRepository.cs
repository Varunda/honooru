using honooru.Models.App;
using honooru.Services.Db;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace honooru.Services.Repositories {

    public class MediaAssetRepository {

        private readonly ILogger<MediaAssetRepository> _Logger;
        private readonly MediaAssetDb _MediaAssetDb;

        public MediaAssetRepository(ILogger<MediaAssetRepository> logger,
            MediaAssetDb mediaAssetDb) {

            _Logger = logger;
            _MediaAssetDb = mediaAssetDb;
        }

        public Task<List<MediaAsset>> GetAll() {
            return _MediaAssetDb.GetAll();
        }

        public async Task<List<MediaAsset>> GetByStatus(MediaAssetStatus status) {
            return (await GetAll()).Where(iter => iter.Status == status).ToList();
        }

        public Task<MediaAsset?> GetByID(Guid assetID) {
            return _MediaAssetDb.GetByID(assetID);
        }

        public Task<MediaAsset?> GetByMD5(string md5) {
            return _MediaAssetDb.GetByMD5(md5);
        }

        public Task Upsert(MediaAsset asset) {
            return _MediaAssetDb.Upsert(asset);
        }

        public Task Delete(Guid assetID) {
            return _MediaAssetDb.Delete(assetID);
        }

    }
}
