using honooru.Models;
using honooru.Models.Api;
using honooru.Models.App;
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
        public async Task<ApiResponse<List<ExtendedTag>>> GetByPostID(ulong postID) {
            List<PostTag> postTags = await _PostTagDb.GetByPostID(postID);

            List<Tag> tags = await _TagRepository.GetByIDs(postTags.Select(iter => iter.TagID));

            Dictionary<ulong, TagInfo> infos = (await _TagInfoRepository.GetByIDs(tags.Select(iter => iter.ID))).ToDictionary(iter => iter.ID);
            Dictionary<ulong, TagType> types = (await _TagTypeRepository.GetByIDs(tags.Select(iter => iter.TypeID).Distinct())).ToDictionary(iter => iter.ID);

            List<ExtendedTag> ex = new List<ExtendedTag>(tags.Count);

            foreach (Tag tag in tags) {
                ExtendedTag et = new();
                et.ID = tag.ID;
                et.Name = tag.Name;
                et.TypeID = tag.TypeID;

                TagType? type = types.GetValueOrDefault(tag.TypeID);
                et.TypeName = type?.Name ?? $"<missing {tag.TypeID}>";
                et.HexColor = type?.HexColor ?? "000000";

                TagInfo? info = infos.GetValueOrDefault(tag.ID);
                et.Uses = info?.Uses ?? 0;
                et.Description = info?.Description;

                ex.Add(et);
            }

            return ApiOk(ex);
        }

    }
}
