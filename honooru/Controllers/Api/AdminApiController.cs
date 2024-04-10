using honooru.Code;
using honooru.Models;
using honooru.Models.App;
using honooru.Models.Config;
using honooru.Models.Db;
using honooru.Models.Internal;
using honooru.Models.Queues;
using honooru.Services;
using honooru.Services.Queues;
using honooru.Services.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace honooru.Controllers.Api {

    [ApiController]
    [Route("/api/admin")]
    public class AdminApiController : ApiControllerBase {

        private readonly ILogger<AdminApiController> _Logger;
        private readonly IOptions<StorageOptions> _StorageOptions;
        private readonly IMemoryCache _Cache;

        private readonly PostRepository _PostRepository;
        private readonly MediaAssetRepository _MediaAssetRepository;
        private readonly IqdbClient _Iqdb;

        private readonly BaseQueue<ThumbnailCreationQueueEntry> _ThumbnailQueue;

        public AdminApiController(ILogger<AdminApiController> logger,
            PostRepository postRepository, BaseQueue<ThumbnailCreationQueueEntry> thumbnailQueue,
            IqdbClient iqdb, IOptions<StorageOptions> storageOptions,
            MediaAssetRepository mediaAssetRepository, IMemoryCache cache) {

            _Logger = logger;

            _PostRepository = postRepository;
            _ThumbnailQueue = thumbnailQueue;
            _Iqdb = iqdb;
            _StorageOptions = storageOptions;
            _MediaAssetRepository = mediaAssetRepository;
            _Cache = cache;
        }

        /// <summary>
        ///     submit all posts for thumbnail recreation
        /// </summary>
        /// <response code="200">
        ///     all posts were submitted for thumbnail recreation. the recreation could still be going, there is
        ///     no way to check the progress of the queue
        /// </response>
        [HttpPost("remake-all-thumbnails")]
        [PermissionNeeded(AppPermission.APP_ACCOUNT_ADMIN)]
        public async Task<ApiResponse> RemakeAllThumbnails() {
            List<Post> posts = await _PostRepository.GetAll();

            foreach (Post post in posts) {
                _ThumbnailQueue.Queue(new ThumbnailCreationQueueEntry() {
                    MD5 = post.MD5,
                    FileExtension = post.FileExtension,
                    RecreateIfNeeded = true
                });
            }

            _Logger.LogInformation($"inserted all posts for thumbnail recreation [posts.Count={posts.Count}]");

            return ApiOk();
        }

        /// <summary>
        ///     submit all posts to recreate IQDB hashes
        /// </summary>
        /// <response code="200">
        ///     the request is complete. this response code has no bearing on the success of the IQDB entry recreation
        /// </response>
        [HttpPost("remake-all-iqdb-entries")]
        [PermissionNeeded(AppPermission.APP_ACCOUNT_ADMIN)]
        public async Task<ApiResponse> RemakeAllIqdbEntries() {
            List<Post> posts = await _PostRepository.GetAll();
            List<MediaAsset> assets = await _MediaAssetRepository.GetAll();
            _Logger.LogInformation($"regenerating IQDB entries [posts.Count={posts.Count}]");

            new Thread(async () => {
                try {
                    _Logger.LogDebug($"starting background thread for recreating IQDB hashes");

                    foreach (Post post in posts) {
                        await _Iqdb.RemoveByMD5(post.MD5);

                        string path = Path.Combine(_StorageOptions.Value.RootDirectory, "original", post.MD5 + "." + post.FileExtension);
                        IqdbEntry? entry = await _Iqdb.Create(path, post.MD5, post.FileExtension);
                    }

                    foreach (MediaAsset asset in assets) {
                        if (asset.MD5 == "") {
                            continue;
                        }

                        await _Iqdb.RemoveByMD5(asset.MD5);

                        string path = Path.Combine(_StorageOptions.Value.RootDirectory, "original", asset.MD5 + "." + asset.FileExtension);
                        IqdbEntry? entry = await _Iqdb.Create(path, asset.MD5, asset.FileExtension);
                    }

                    _Logger.LogInformation($"recreated all IQDB entries [posts.Count={posts.Count}]");
                } catch (Exception ex) {
                    _Logger.LogError(ex, $"failed to recreate all IQDB entries");
                }
            }).Start();

            return ApiOk();
        }

        [HttpPost("cache-evict")]
        public ApiResponse CacheEvict([FromQuery] string key) {
            _Logger.LogInformation($"removing entry from cache key={key}]");

            _Cache.Remove(key);

            return ApiOk();
        }

    }
}
