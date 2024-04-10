using honooru.Code;
using honooru.Models;
using honooru.Models.Api;
using honooru.Models.App;
using honooru.Models.Internal;
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

        private readonly PostTagDb _PostTagDb;
        private readonly TagRepository _TagRepository;
        private readonly TagInfoRepository _TagInfoRepository;
        private readonly TagTypeRepository _TagTypeRepository;

        public PostTagApiController(ILogger<PostTagApiController> logger,
            PostTagDb postTagDb, TagRepository tagRepository,
            TagInfoRepository tagInfoRepository, TagTypeRepository tagTypeRepository) {

            _Logger = logger;

            _PostTagDb = postTagDb;
            _TagRepository = tagRepository;
            _TagInfoRepository = tagInfoRepository;
            _TagTypeRepository = tagTypeRepository;
        }

        [HttpGet("post/{postID}")]
        [PermissionNeeded(AppPermission.APP_VIEW)]
        public async Task<ApiResponse<List<ExtendedTag>>> GetByPostID(ulong postID) {
            List<PostTag> postTags = await _PostTagDb.GetByPostID(postID);

            List<Tag> tags = await _TagRepository.GetByIDs(postTags.Select(iter => iter.TagID));
            List<ExtendedTag> ex = await _TagRepository.CreateExtended(tags);

            return ApiOk(ex);
        }

    }
}
