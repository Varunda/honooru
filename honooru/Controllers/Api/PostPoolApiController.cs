using honooru.Code;
using honooru.Models;
using honooru.Models.App;
using honooru.Models.Db;
using honooru.Models.Internal;
using honooru.Services;
using honooru.Services.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace honooru.Controllers.Api {


    [ApiController]
    [Route("/api/post-pool")]
    public class PostPoolApiController : ApiControllerBase {

        private readonly ILogger<PostPoolApiController> _Logger;
        private readonly PostPoolRepository _PoolRepository;
        private readonly PostPoolEntryRepository _PoolEntryRepository;
        private readonly PostRepository _PostRepository;

        private readonly AppCurrentAccount _CurrentUser;

        public PostPoolApiController(ILogger<PostPoolApiController> logger,
            PostPoolRepository poolRepository, PostPoolEntryRepository poolEntryRepository,
            PostRepository postRepository, AppCurrentAccount currentUser) {

            _Logger = logger;
            _PoolRepository = poolRepository;
            _PoolEntryRepository = poolEntryRepository;
            _PostRepository = postRepository;
            _CurrentUser = currentUser;
        }

        /// <summary>
        ///     get all <see cref="PostPool"/>s
        /// </summary>
        /// <response code="200">
        ///     the response will contain a list of <see cref="PostPool"/>s
        /// </response>
        [HttpGet]
        [PermissionNeeded(AppPermission.APP_VIEW)]
        public async Task<ApiResponse<List<PostPool>>> GetAll() {
            List<PostPool> pools = await _PoolRepository.GetAll();

            return ApiOk(pools);
        }

        /// <summary>
        ///     get a specific <see cref="PostPool"/> by it's <see cref="PostPool.ID"/>
        /// </summary>
        /// <param name="poolID"></param>
        /// <returns></returns>
        [HttpGet("{poolID}")]
        [PermissionNeeded(AppPermission.APP_VIEW)]
        public async Task<ApiResponse<PostPool>> GetByID(ulong poolID) {
            PostPool? pool = await _PoolRepository.GetByID(poolID);
            if (pool == null) {
                return ApiNoContent<PostPool>();
            }

            return ApiOk(pool);
        }

        /// <summary>
        ///     get all the <see cref="PostPool"/>s a post is a part of
        /// </summary>
        /// <param name="postID"></param>
        /// <returns></returns>
        [HttpGet("{postID}/pools")]
        public async Task<ApiResponse<List<PostPool>>> GetByPostID(ulong postID) {
            Post? post = await _PostRepository.GetByID(postID);
            if (post == null) {
                return ApiNotFound<List<PostPool>>($"{nameof(Post)} {postID}");
            }

            List<PostPoolEntry> entries = await _PoolEntryRepository.GetByPostID(postID);

            List<PostPool> pools = [];
            foreach (PostPoolEntry entry in entries) {
                PostPool? pool = await _PoolRepository.GetByID(entry.PoolID);
                if (pool != null) {
                    pools.Add(pool);
                }
            }

            return ApiOk(pools);
        }

        /// <summary>
        ///     create a new <see cref="PostPool"/> with a given <see cref="PostPool.Name"/>
        /// </summary>
        /// <param name="name">name of the pool to be created</param>
        /// <response code="200">
        ///     the response will contain the newly created <see cref="PostPool"/>
        ///     setting <see cref="PostPool.Name"/> to <paramref name="name"/>
        /// </response>
        [HttpPost]
        [PermissionNeeded(AppPermission.APP_POOL_CREATE)]
        public async Task<ApiResponse<PostPool>> Create([FromQuery] string name) {
            AppAccount? currentUser = await _CurrentUser.Get();
            if (currentUser == null) {
                return ApiAuthorize<PostPool>();
            }

            if (string.IsNullOrWhiteSpace(name)) {
                return ApiBadRequest<PostPool>($"{nameof(name)} cannot be blank or only contain whitespace");
            }

            PostPool pool = new();
            pool.Name = name;
            pool.CreatedByID = currentUser.ID;
            pool.Timestamp = DateTime.UtcNow;

            ulong id = await _PoolRepository.Insert(pool);
            pool.ID = id;

            return ApiOk(pool);
        }

        /// <summary>
        ///     delete a <see cref="PostPool"/>
        /// </summary>
        /// <param name="poolID">ID of the <see cref="PostPool"/> to delete</param>
        /// <response code="200">
        ///     the <see cref="PostPool"/> with <see cref="PostPool.ID"/> of <paramref name="poolID"/>
        ///     was successfully deleted
        /// </response>
        [HttpDelete("{poolID}")]
        [PermissionNeeded(AppPermission.APP_POOL_DELETE)]
        public async Task<ApiResponse> Delete(ulong poolID) {
            PostPool? pool = await _PoolRepository.GetByID(poolID);
            if (pool == null) {
                return ApiNotFound($"{nameof(PostPool)} {poolID}");
            }

            await _PoolEntryRepository.DeleteByPoolID(poolID);
            await _PoolRepository.Delete(poolID);

            return ApiOk();
        }

    }
}
