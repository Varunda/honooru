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
using honooru.Services.Util;
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
        private readonly TagRepository _TagRepository;
        private readonly FileExtensionService _FileExtensionUtil;

        private readonly BaseQueue<ThumbnailCreationQueueEntry> _ThumbnailQueue;
        private readonly BaseQueue<TagInfoUpdateQueueEntry> _TagInfoUpdateQueue;

        public AdminApiController(ILogger<AdminApiController> logger,
            PostRepository postRepository, BaseQueue<ThumbnailCreationQueueEntry> thumbnailQueue,
            IqdbClient iqdb, IOptions<StorageOptions> storageOptions,
            MediaAssetRepository mediaAssetRepository, IMemoryCache cache,
            TagRepository tagRepository, BaseQueue<TagInfoUpdateQueueEntry> tagInfoUpdateQueue,
            FileExtensionService fileExtensionUtil) {

            _Logger = logger;

            _PostRepository = postRepository;
            _ThumbnailQueue = thumbnailQueue;
            _Iqdb = iqdb;
            _StorageOptions = storageOptions;
            _MediaAssetRepository = mediaAssetRepository;
            _Cache = cache;
            _TagRepository = tagRepository;
            _TagInfoUpdateQueue = tagInfoUpdateQueue;
            _FileExtensionUtil = fileExtensionUtil;
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
        ///     put all tags into the queue for recount
        /// </summary>
        /// <returns></returns>
        [HttpPost("recount-tags")]
        [PermissionNeeded(AppPermission.APP_ACCOUNT_ADMIN)]
        public async Task<ApiResponse> RecountAllTags() {

            List<Tag> tags = await _TagRepository.GetAll();

            foreach (Tag tag in tags) {
                _TagInfoUpdateQueue.Queue(new TagInfoUpdateQueueEntry() {
                    TagID = tag.ID
                });
            }

            return ApiOk();
        }

        /// <summary>
        ///     update the <see cref="Post.FileType"/> of all <see cref="Post"/>s
        /// </summary>
        /// <returns></returns>
        [HttpPost("update-file-types")]
        public async Task<ApiResponse> UpdateFileType() {
            List<Post> posts = await _PostRepository.GetAll();
            _Logger.LogInformation($"updating file types for all posts [posts.Count={posts.Count}]");

            // run in background thread in case this operation takes a long time
            try {
                new Thread(async () => {
                    try {
                        _Logger.LogDebug($"starting background thread for updating file type field");

                        foreach (Post post in posts) {
                            post.FileType = _FileExtensionUtil.GetFileType(post.FileExtension) ?? "";

                            await _PostRepository.Update(post.ID, post);
                        }

                        _Logger.LogInformation($"updated all post file type fields [posts.Count={posts.Count}]");
                    } catch (Exception ex) {
                        _Logger.LogError(ex, $"failed to post file types");
                    }
                }).Start();
            } catch (Exception ex) {
                _Logger.LogError(ex, $"thread exception when updating file types");
            }

            return ApiOk();
        }

        /// <summary>
        ///     submit all posts to recreate IQDB hashes
        /// </summary>
        /// <param name="force">
        ///     if the IQDB hashes will be set even if the post already has <see cref="Post.IqdbHash"/> set.
        ///     defaults to true, as this is likely the desired behavior
        /// </param>
        /// <response code="200">
        ///     the request is complete. this response code has no bearing on the success of the IQDB entry recreation
        /// </response>
        [HttpPost("remake-all-iqdb-entries")]
        [PermissionNeeded(AppPermission.APP_ACCOUNT_ADMIN)]
        public async Task<ApiResponse> RemakeAllIqdbEntries([FromQuery] bool force = true) {
            List<Post> posts = await _PostRepository.GetAll();
            List<MediaAsset> assets = await _MediaAssetRepository.GetAll();
            _Logger.LogInformation($"regenerating IQDB entries [posts.Count={posts.Count}]");

            try {
                new Thread(async () => {
                    try {
                        _Logger.LogDebug($"starting background thread for recreating IQDB hashes");

                        foreach (Post post in posts) {
                            if (force == false && post.IqdbHash != "") {
                                _Logger.LogDebug($"no need to recreate IQDB hash as it already set [post.ID={post.ID}]");
                                continue;
                            }

                            await _Iqdb.RemoveByMD5(post.MD5);

                            string path = Path.Combine(_StorageOptions.Value.RootDirectory, "original", post.FileLocation);
                            IqdbEntry? entry = await _Iqdb.Create(path, post.MD5, post.FileExtension);

                            if (entry == null) {
                                _Logger.LogWarning($"failed to create IQDB entry for post [post.ID={post.ID}] [post.MD5={post.MD5}]");
                            } else {
                                post.IqdbHash = entry.Hash;
                                await _PostRepository.Update(post.ID, post);
                            }
                        }

                        foreach (MediaAsset asset in assets) {
                            // skip if not hashed
                            if (asset.MD5 == "" || asset.IqdbHash == null) {
                                continue;
                            }

                            await _Iqdb.RemoveByMD5(asset.MD5);

                            string path = Path.Combine(_StorageOptions.Value.RootDirectory, "original", asset.FileLocation);
                            IqdbEntry? entry = await _Iqdb.Create(path, asset.MD5, asset.FileExtension);

                            if (entry == null) {
                                _Logger.LogWarning($"failed to create IQDB entry for media asset [asset.Guid={asset.Guid}] [asset.MD5={asset.MD5}]");
                            } else {
                                asset.IqdbHash = entry.Hash;
                                await _MediaAssetRepository.Upsert(asset);
                            }
                        }

                        _Logger.LogInformation($"recreated all IQDB entries [posts.Count={posts.Count}]");
                    } catch (Exception ex) {
                        _Logger.LogError(ex, $"failed to recreate all IQDB entries");
                    }
                }).Start();
            } catch (Exception ex) {
                _Logger.LogError(ex, $"thread exception when remaking all IQDB entries");
            }

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
