using honooru.Code;
using honooru.Models;
using honooru.Models.App;
using honooru.Models.Db;
using honooru.Models.Internal;
using honooru.Services;
using honooru.Services.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MoreLinq.Extensions;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace honooru.Controllers.Api {

    [ApiController]
    [Route("/api/post-pool-entry")]
    public class PostPoolEntryApiController : ApiControllerBase {

        private readonly ILogger<PostPoolEntryApiController> _Logger;
        private readonly PostPoolRepository _PoolRepository;
        private readonly PostPoolEntryRepository _PoolEntryRepository;
        private readonly PostRepository _PostRepository;

        private readonly AppCurrentAccount _CurrentUser;

        public PostPoolEntryApiController(ILogger<PostPoolEntryApiController> logger,
            PostPoolRepository poolRepository, PostPoolEntryRepository poolEntryRepository,
            PostRepository postRepository, AppCurrentAccount currentUser) {

            _Logger = logger;
            _PoolRepository = poolRepository;
            _PoolEntryRepository = poolEntryRepository;
            _PostRepository = postRepository;
            _CurrentUser = currentUser;
        }

        /// <summary>
        ///     get all <see cref="PostPool"/>s a <see cref="Post"/> is a part of
        /// </summary>
        /// <param name="postID"></param>
        /// <returns></returns>
        [HttpGet("pool/{postID}")]
        [PermissionNeeded(AppPermission.APP_POOL_ENTRY_ADD)]
        public async Task<ApiResponse<List<PostPool>>> GetByPostID(ulong postID) {
            Post? post = await _PostRepository.GetByID(postID);
            if (post == null) {
                return ApiNotFound<List<PostPool>>($"{nameof(Post)} {postID}");
            }

            List<PostPool> pools = new();
            List<PostPoolEntry> entries = await _PoolEntryRepository.GetByPostID(postID);
            foreach (PostPoolEntry entry in entries) {
                PostPool? pool = await _PoolRepository.GetByID(entry.PoolID);
                if (pool == null) {
                    _Logger.LogWarning($"missing {nameof(PostPool)} from {nameof(PostPoolEntry)} [PoolID={entry.PoolID}]");
                } else {
                    pools.Add(pool);
                }
            }

            return ApiOk(pools);
        }

        /// <summary>
        ///     get all <see cref="Post"/>s in a <see cref="PostPool"/>
        /// </summary>
        /// <param name="poolID"></param>
        /// <returns></returns>
        [HttpGet("post/{poolID}")]
        public async Task<ApiResponse<List<Post>>> GetByPoolID(ulong poolID) {
            PostPool? pool = await _PoolRepository.GetByID(poolID);
            if (pool == null) {
                return ApiNoContent<List<Post>>();
            }

            List<Post> posts = new();
            List<PostPoolEntry> entries = await _PoolEntryRepository.GetByPoolID(poolID);
            foreach (PostPoolEntry entry in entries) {
                Post? post = await _PostRepository.GetByID(entry.PostID);
                if (post == null) {
                    _Logger.LogWarning($"missing {nameof(Post)} from {nameof(PostPoolEntry)} [PostID={entry.PostID}]");
                } else {
                    posts.Add(post);
                }
            }

            return ApiOk(posts);
        }

        /// <summary>
        ///     add a new <see cref="Post"/> to a <see cref="PostPool"/>
        /// </summary>
        /// <param name="poolID">ID of the <see cref="PostPool"/> to add the post to</param>
        /// <param name="postID">ID of the <see cref="Post"/> to be added to the pool</param>
        /// <returns></returns>
        [HttpPost("add")]
        [PermissionNeeded(AppPermission.APP_POOL_ENTRY_ADD)]
        public async Task<ApiResponse<PostPoolEntry>> AddToPool([FromQuery] ulong poolID, [FromQuery] ulong postID) {
            PostPool? pool = await _PoolRepository.GetByID(poolID);
            if (pool == null) {
                return ApiNotFound<PostPoolEntry>($"{nameof(PostPool)} {poolID}");
            }

            Post? post = await _PostRepository.GetByID(postID);
            if (post == null) {
                return ApiNotFound<PostPoolEntry>($"{nameof(PostPool)} {postID}");
            }

            _Logger.LogInformation($"adding post to pool [poolID={poolID}] [postID={postID}] [pool.Name={pool.Name}]");

            if ((await _PoolEntryRepository.IsPostInPool(poolID, postID)) == true) {
                _Logger.LogDebug($"cannot add post to pool: post is already in pool [poolID={poolID}] [postID={postID}]");
                return ApiBadRequest<PostPoolEntry>($"pool {poolID} already contains post {postID}");
            }

            PostPoolEntry entry = new();
            entry.PoolID = poolID;
            entry.PostID = postID;

            await _PoolEntryRepository.Insert(entry);

            return ApiOk(entry);
        }

        /// <summary>
        ///     remove a <see cref="Post"/> from a <see cref="PostPool"/>
        /// </summary>
        /// <param name="poolID">ID of the <see cref="PostPool"/> to remove the post from</param>
        /// <param name="postID">ID of the <see cref="Post"/> that will be removed from the pool</param>
        /// <returns></returns>
        [HttpDelete("{poolID}/{postID}")]
        [PermissionNeeded(AppPermission.APP_POOL_ENTRY_ADD)]
        public async Task<ApiResponse> RemoveFromPool(ulong poolID, ulong postID) {
            if ((await _PoolEntryRepository.IsPostInPool(poolID, postID)) == false) {
                return ApiBadRequest($"pool {poolID} does not contain {postID}");
            }

            _Logger.LogInformation($"removing post from pool [poolID={poolID}] [postID={postID}]");

            PostPoolEntry entry = new();
            entry.PoolID = poolID;
            entry.PostID = postID;

            await _PoolEntryRepository.Delete(entry);

            return ApiOk();
        }

    }
}
