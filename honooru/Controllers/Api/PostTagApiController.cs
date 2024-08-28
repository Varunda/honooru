using honooru.Code;
using honooru.Models;
using honooru.Models.Api;
using honooru.Models.App;
using honooru.Models.Db;
using honooru.Models.Internal;
using honooru.Services;
using honooru.Services.Db;
using honooru.Services.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace honooru.Controllers.Api {

    [ApiController]
    [Route("/api/post-tag")]
    public class PostTagApiController : ApiControllerBase {

        private readonly ILogger<PostTagApiController> _Logger;

        private readonly AppCurrentAccount _CurrentAccount;
        private readonly PostTagDb _PostTagDb;
        private readonly PostRepository _PostRepository;
        private readonly TagRepository _TagRepository;
        private readonly TagInfoRepository _TagInfoRepository;
        private readonly TagTypeRepository _TagTypeRepository;

        public PostTagApiController(ILogger<PostTagApiController> logger,
            PostTagDb postTagDb, TagRepository tagRepository,
            TagInfoRepository tagInfoRepository, TagTypeRepository tagTypeRepository,
            PostRepository postRepository, AppCurrentAccount currentAccount) {

            _Logger = logger;

            _PostTagDb = postTagDb;
            _TagRepository = tagRepository;
            _TagInfoRepository = tagInfoRepository;
            _TagTypeRepository = tagTypeRepository;
            _PostRepository = postRepository;
            _CurrentAccount = currentAccount;
        }

        /// <summary>
        ///     get the <see cref="Tag"/>s of a <see cref="Post"/>
        /// </summary>
        /// <param name="postID">ID of the post</param>
        /// <response code="200">
        ///     the response will contain a list of <see cref="ExtendedTag"/>s
        ///     for each <see cref="PostTag"/> a <see cref="Post"/> has
        /// </response>
        /// <response code="400">
        ///     the <see cref="Post"/> with <see cref="Post.ID"/> of <paramref name="postID"/>
        ///     is private, and the current user cannot view this post
        /// </response>
        [HttpGet("post/{postID}")]
        [PermissionNeeded(AppPermission.APP_VIEW)]
        public async Task<ApiResponse<List<ExtendedTag>>> GetByPostID(ulong postID) {
            AppAccount? currentUser = await _CurrentAccount.Get();
            if (currentUser == null) {
                return ApiAuthorize<List<ExtendedTag>>();
            }

            Post? post = await _PostRepository.GetByID(postID);
            if (post != null && post.Private == true && post.PosterUserID != currentUser.ID) {
                return ApiBadRequest<List<ExtendedTag>>($"post {postID} is private, cannot get tags");
            }

            List<PostTag> postTags = await _PostTagDb.GetByPostID(postID);

            List<Tag> tags = await _TagRepository.GetByIDs(postTags.Select(iter => iter.TagID));
            List<ExtendedTag> ex = await _TagRepository.CreateExtended(tags);

            return ApiOk(ex);
        }

    }
}
