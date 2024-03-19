using honooru.Code.ExtensionMethods;
using honooru.Models;
using honooru.Models.Api;
using honooru.Models.App;
using honooru.Services.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace honooru.Controllers.Api {

    [Route("/api/tag-implication")]
    [ApiController]
    public class TagImplicationApiController : ApiControllerBase {

        private readonly ILogger<TagImplicationApiController> _Logger;

        private readonly TagImplicationRepository _TagImplicationRepository;
        private readonly TagRepository _TagRepository;

        public TagImplicationApiController(ILogger<TagImplicationApiController> logger,
            TagImplicationRepository tagImplicationRepository, TagRepository tagRepository) {

            _Logger = logger;
            _TagImplicationRepository = tagImplicationRepository;
            _TagRepository = tagRepository;
        }

        [HttpGet("{tagID}/block")]
        public async Task<ApiResponse<TagImplicationBlock>> GetBlockByTagID(ulong tagID) {
            if (await _TagRepository.GetByID(tagID) == null) {
                return ApiNotFound<TagImplicationBlock>($"{nameof(Tag)} {tagID}");
            }

            TagImplicationBlock block = new();

            block.TagID = tagID;
            block.Sources = await _TagImplicationRepository.GetBySourceTagID(tagID);
            block.Targets = await _TagImplicationRepository.GetByTargetTagID(tagID);

            HashSet<ulong> tagIDs = [.. block.Sources.Select(iter => iter.TagB), .. block.Targets.Select(iter => iter.TagA)];
            block.Tags = await _TagRepository.GetByIDs(tagIDs);

            return ApiOk(block);
        }

        [HttpGet("{tagID}/source")]
        public async Task<ApiResponse<List<TagImplication>>> GetBySourceTag(ulong tagID) {
            List<TagImplication> targets = await _TagImplicationRepository.GetBySourceTagID(tagID);

            return ApiOk(targets);
        }

        [HttpGet("{tagID}/target")]
        public async Task<ApiResponse<List<TagImplication>>> GetByTargetTag(ulong tagID) { 
            List<TagImplication> sources = await _TagImplicationRepository.GetByTargetTagID(tagID);

            return ApiOk(sources);
        }

        /// <summary>
        ///     create a new <see cref="TagImplication"/>
        /// </summary>
        /// <param name="sourceTagID">
        ///     ID of the <see cref="Tag"/> with <see cref="Tag.ID"/> of <paramref name="sourceTagID"/> that when applied to a post,
        ///     will also add the <see cref="Tag"/> with <see cref="Tag.ID"/> of <paramref name="targetTagID"/>
        /// </param>
        /// <param name="targetTagID">
        ///     when the <see cref="Tag"/> with <see cref="Tag.ID"/> of <paramref name="sourceTagID"/> is applied to a post,
        ///     then this <see cref="Tag"/> with <see cref="Tag.ID"/> of <paramref name="targetTagID"/> is also added
        /// </param>
        /// <response code="200">
        ///     a new <see cref="TagImplication"/> was successfully created
        /// </response>
        /// <response code="400">
        ///     one of the following validation errors occured:
        ///     <ul>
        ///         <li><paramref name="sourceTagID"/> equals <paramref name="targetTagID"/></li>
        ///         <li>
        ///             there is already an implication with <see cref="TagImplication.TagA"/> of <paramref name="sourceTagID"/>,
        ///             and <see cref="TagImplication.TagB"/> of <paramref name="targetTagID"/>
        ///         </li>
        ///         <li>
        ///             there is already an implication with <see cref="TagImplication.TagA"/> of <paramref name="targetTagID"/>,
        ///             and <see cref="TagImplication.TagB"/> of <paramref name="sourceTagID"/>, which would create a loop
        ///         </li>
        ///     </ul>
        /// </response>
        /// <response code="404">
        ///     either the <see cref="Tag"/> with <see cref="Tag.ID"/> of <paramref name="sourceTagID"/> or <paramref name="targetTagID"/>
        ///     does not exist
        /// </response>
        [HttpPost]
        public async Task<ApiResponse> Create([FromQuery] ulong sourceTagID, [FromQuery] ulong targetTagID) {

            if (sourceTagID == targetTagID) {
                return ApiBadRequest($"the source and target of an implication cannot be the same");
            }

            Tag? sourceTag = await _TagRepository.GetByID(sourceTagID);
            if (sourceTag == null) {
                return ApiNotFound($"{nameof(Tag)} {sourceTagID}");
            }

            Tag? targetTag = await _TagRepository.GetByID(targetTagID);
            if (targetTag == null) {
                return ApiNotFound($"{nameof(Tag)} {targetTagID}");
            }

            // source cannot already imply target
            List<TagImplication> tagASources = await _TagImplicationRepository.GetBySourceTagID(sourceTagID);
            if (tagASources.FirstOrDefault(iter => iter.TagB == targetTagID) != null) {
                return ApiBadRequest($"tag {sourceTagID} already implies tag {targetTagID}");
            }

            // target cannot already imply source
            List<TagImplication> tagBSources = await _TagImplicationRepository.GetBySourceTagID(targetTagID);
            if (tagBSources.FirstOrDefault(iter => iter.TagB == sourceTagID) != null) {
                return ApiBadRequest($"tag {targetTagID} already implies tag {sourceTagID} (this would create a loop)");
            }

            TagImplication implication = new();
            implication.TagA = sourceTagID;
            implication.TagB = targetTagID;

            await _TagImplicationRepository.Insert(implication);

            return ApiOk();
        }

        [HttpDelete]
        public async Task<ApiResponse> Delete([FromQuery] ulong sourceTagID, [FromQuery] ulong targetTagID) {

            List<TagImplication> sources = await _TagImplicationRepository.GetBySourceTagID(sourceTagID);
            TagImplication? implication = sources.FirstOrDefault(iter => iter.TagB == targetTagID);
            if (implication == null) {
                return ApiNotFound($"{nameof(TagImplication)} {sourceTagID} {targetTagID}");
            }

            await _TagImplicationRepository.Delete(implication);

            return ApiOk();
        }

    }
}
