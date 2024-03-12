using honooru.Models.App;
using honooru.Services.Db;
using Microsoft.Extensions.Logging;
using System;
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

        public Task<MediaAsset?> GetByID(Guid assetID) {
            return _MediaAssetDb.GetByID(assetID);
        }

        public Task<MediaAsset?> GetByMD5(string md5) {
            return _MediaAssetDb.GetByMD5(md5);
        }

        public Task Upsert(MediaAsset asset) {
            return _MediaAssetDb.Upsert(asset);
        }

    }
}
