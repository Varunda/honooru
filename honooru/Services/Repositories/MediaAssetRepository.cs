using honooru.Models.App;
using honooru.Models.Config;
using honooru.Services.Db;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace honooru.Services.Repositories {

    public class MediaAssetRepository {

        private readonly ILogger<MediaAssetRepository> _Logger;
        private readonly MediaAssetDb _MediaAssetDb;
        private readonly IOptions<StorageOptions> _StorageOptions;

        public MediaAssetRepository(ILogger<MediaAssetRepository> logger,
            MediaAssetDb mediaAssetDb, IOptions<StorageOptions> storageOptions) {

            _Logger = logger;
            _MediaAssetDb = mediaAssetDb;
            _StorageOptions = storageOptions;
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

        /// <summary>
        ///     delete a <see cref="MediaAsset"/>. use <see cref="Erase(Guid)"/> if you want to remove the files as well
        /// </summary>
        /// <param name="assetID">ID of the asset to delete</param>
        /// <returns>a task once the async operation is complete</returns>
        public async Task Delete(Guid assetID) {
            await _MediaAssetDb.Delete(assetID);
        }

        /// <summary>
        ///     erase a <see cref="MediaAsset"/>, which also deletes all files associated with it
        /// </summary>
        /// <param name="assetID"></param>
        /// <returns></returns>
        public async Task Erase(Guid assetID) {
            MediaAsset? asset = await GetByID(assetID);
            if (asset == null) {
                return;
            }

            await _MediaAssetDb.Delete(assetID);

            _DeletePossiblePath(Path.Combine(_StorageOptions.Value.RootDirectory, "original", asset.MD5 + "." + asset.FileExtension));
            _DeletePossiblePath(Path.Combine(_StorageOptions.Value.RootDirectory, "upload", asset.MD5 + "." + asset.FileExtension));
            _DeletePossiblePath(Path.Combine(_StorageOptions.Value.RootDirectory, "upload", asset.Guid + "." + asset.FileExtension));
            _DeletePossiblePath(Path.Combine(_StorageOptions.Value.RootDirectory, "work", asset.MD5 + "." + asset.FileExtension));
            _DeletePossiblePath(Path.Combine(_StorageOptions.Value.RootDirectory, "work", asset.Guid + "." + asset.FileExtension));
        }

        private void _DeletePossiblePath(string path) {
            _Logger.LogTrace($"checking if deletion for file is needed [path={path}]");
            if (File.Exists(path) == true) {
                _Logger.LogDebug($"deleting file for media asset [path={path}]");
                File.Delete(path);
            }
        }

    }
}
