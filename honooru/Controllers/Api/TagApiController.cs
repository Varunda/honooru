using honooru.Code;
using honooru.Models;
using honooru.Models.Api;
using honooru.Models.App;
using honooru.Models.Internal;
using honooru.Models.Queues;
using honooru.Services.Db;
using honooru.Services.Queues;
using honooru.Services.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace honooru.Controllers.Api {

    [ApiController]
    [Route("/api/tag")]
    public class TagApiController : ApiControllerBase {

        private readonly ILogger<TagApiController> _Logger;

        private readonly TagRepository _TagRepository;
        private readonly TagTypeRepository _TagTypeRepository;
        private readonly TagInfoRepository _TagInfoRepository;

        private readonly BaseQueue<TagInfoUpdateQueueEntry> _TagInfoUpdateQueue;

        public TagApiController(ILogger<TagApiController> logger,
            TagRepository tagRepository, TagTypeRepository tagTypeRepository,
            TagInfoRepository tagInfoRepository, BaseQueue<TagInfoUpdateQueueEntry> tagInfoUpdateQueue) {

            _Logger = logger;

            _TagRepository = tagRepository;
            _TagTypeRepository = tagTypeRepository;
            _TagInfoRepository = tagInfoRepository;
            _TagInfoUpdateQueue = tagInfoUpdateQueue;
        }

        /// <summary>
        ///     get the basic <see cref="Tag"/> information by ID
        /// </summary>
        /// <param name="tagID">ID of the <see cref="Tag"/> to get</param>
        /// <response code="200">
        ///     the <see cref="Tag"/> with <see cref="Tag.ID"/> of <paramref name="tagID"/>
        /// </response>
        /// <response code="204">
        ///     no <see cref="Tag"/> with <see cref="Tag.ID"/> of <paramref name="tagID"/> exists
        /// </response>
        [HttpGet("{tagID}")]
        [PermissionNeeded(AppPermission.APP_VIEW)]
        public async Task<ApiResponse<Tag>> GetByID(ulong tagID) {
            Tag? tag = await _TagRepository.GetByID(tagID);
            if (tag == null) {
                return ApiNoContent<Tag>();
            }

            return ApiOk(tag);
        }

        /// <summary>
        ///     get the <see cref="ExtendedTag"/> information about a tag
        /// </summary>
        /// <param name="tagID">ID of the <see cref="Tag"/> to get the <see cref="ExtendedTag"/> of</param>
        /// <response code="200">
        ///     the <see cref="ExtendedTag"/> that corresponds to the <see cref="Tag"/>
        ///     with <see cref="Tag.ID"/> of <paramref name="tagID"/>
        /// </response>
        /// <response code="204">
        ///     no <see cref="Tag"/> with <see cref="Tag.ID"/> of <paramref name="tagID"/> exists
        /// </response>
        [HttpGet("{tagID}/extended")]
        [PermissionNeeded(AppPermission.APP_VIEW)]
        public async Task<ApiResponse<ExtendedTag>> GetExtenedByID(ulong tagID) {
            Tag? tag = await _TagRepository.GetByID(tagID);
            if (tag == null) {
                return ApiNoContent<ExtendedTag>();
            }

            ExtendedTag et = await _TagRepository.CreateExtended(tag);

            return ApiOk(et);
        }

        /// <summary>
        ///     get the <see cref="ExtendedTag"/> of the the <see cref="Tag"/> with <see cref="Tag.Name"/> of <paramref name="name"/>
        /// </summary>
        /// <param name="name">name of the <see cref="Tag"/> (case does not matter)</param>
        /// <response code="200">
        ///     the <see cref="ExtendedTag"/> for the <see cref="Tag"/> with <see cref="Tag.Name"/> of <paramref name="name"/>
        /// </response>
        /// <response code="204">
        ///     no <see cref="Tag"/> with <see cref="Tag.Name"/> of <paramref name="name"/> exists
        /// </response>
        [HttpGet("name")]
        [PermissionNeeded(AppPermission.APP_VIEW)]
        public async Task<ApiResponse<ExtendedTag>> GetByName([FromQuery] string name) {
            Tag? tag = await _TagRepository.GetByName(name);

            if (tag == null) {
                return ApiNoContent<ExtendedTag>();
            }

            ExtendedTag et = await _TagRepository.CreateExtended(tag);
            return ApiOk(et);
        }

        /// <summary>
        ///     search for all tags based on a name. this performs a string distance search as well,
        ///     so tags that are similar to the input are returned as well. see https://en.wikipedia.org/wiki/Levenshtein_distance for more.
        /// </summary>
        /// <param name="name">name of the tags to search for. required</param>
        /// <param name="limit">sets the maximum of how many tags are returned in the search results. defaults to 20</param>
        /// <param name="sortBy">
        ///     how to sort the returned results. valid values are: "uses", "name".
        ///     defaults to "uses", which uses <see cref="ExtendedTag.Uses"/>
        /// </param>
        /// <param name="sortAscending">if the sorted results will be in ascending (0 to 9) order or descending (9 to 0). defaults to false</param>
        /// <returns></returns>
        /// <exception cref="System.Exception"></exception>
        [HttpGet("search")]
        [PermissionNeeded(AppPermission.APP_VIEW)]
        public async Task<ApiResponse<TagSearchResults>> Search(
            [FromQuery] string name,
            [FromQuery] int? limit = 20,
            [FromQuery] string? sortBy = "uses",
            [FromQuery] bool? sortAscending = false
        ) {

            int realLimit = limit ?? 20;
            sortBy ??= "uses";
            sortAscending ??= false;

            name = name.Trim();

            if (name.Length <= 1) {
                return ApiBadRequest<TagSearchResults>($"please provide at least 2 characters to search");
            }

            if (sortBy != "uses" && sortBy != "name") {
                return ApiBadRequest<TagSearchResults>($"can only sort by 'uses' or 'name'");
            }

            if (realLimit > 500) {
                return ApiBadRequest<TagSearchResults>($"limit cannot go above 500");
            }

            List<Tag> tags = await _TagRepository.SearchByName(name.Trim(), CancellationToken.None);

            List<ExtendedTag> ex = await _TagRepository.CreateExtended(tags);
            ex.Sort((a, b) => {
                if (sortBy == "uses") {
                    if (sortAscending == true) {
                        return (int)(a.Uses - b.Uses);
                    } else {
                        return (int)(b.Uses - a.Uses);
                    }
                } else if (sortBy == "name") {
                    if (sortAscending == true) {
                        return a.Name.CompareTo(b.Name);
                    } else {
                        return b.Name.CompareTo(a.Name);
                    }
                } else {
                    throw new System.Exception($"invalid sortBy");
                }
            });

            TagSearchResults res = new();
            res.Input = name;
            res.Tags = ex.Take(realLimit).ToList();

            return ApiOk(res);
        }

        /// <summary>
        ///     create a new <see cref="Tag"/>
        /// </summary>
        /// <param name="name">name of the tag to use</param>
        /// <param name="typeID">ID of the <see cref="TagType"/></param>
        /// <response code="200">
        ///     the response contains the <see cref="Tag"/> that was newly created,
        ///     with <see cref="Tag.ID"/> updated
        /// </response>
        /// <response code="400">
        ///     one of the following validation errors occured:
        ///     <ul>
        ///         <li><paramref name="name"/> is not valid for a tag name. the response will contain a string explaining why</li>
        ///         <li>a <see cref="Tag"/> with <see cref="Tag.Name"/> of <paramref name="name"/> already exists</li>
        ///     </ul>
        /// </response>
        /// <response code="404">
        ///     no <see cref="TagType"/> with <see cref="TagType.ID"/> of <paramref name="typeID"/> exists
        /// </response>
        [HttpPost]
        [PermissionNeeded(AppPermission.APP_UPLOAD)]
        public async Task<ApiResponse<Tag>> Create([FromQuery] string name, [FromQuery] ulong typeID) {
            name = name.ToLower();

            TagNameValidationResult valid = _TagRepository.ValidateTagName(name);
            if (valid.Valid == false) {
                return ApiBadRequest<Tag>(valid.Reason);
            }

            Tag? existingName = await _TagRepository.GetByName(name);
            if (existingName != null) {
                return ApiBadRequest<Tag>($"tag {existingName.ID} already exists with the same name");
            }

            TagType? tagType = await _TagTypeRepository.GetByID(typeID);
            if (tagType == null) {
                return ApiNotFound<Tag>($"{nameof(TagType)} {typeID}");
            }

            Tag newTag = new();
            newTag.Name = name;
            newTag.TypeID = typeID;

            newTag.ID = await _TagRepository.Insert(newTag);

            return ApiOk(newTag);
        }

        /// <summary>
        ///     update an existing tag with new info
        /// </summary>
        /// <param name="tagID"></param>
        /// <param name="tag"></param>
        /// <returns></returns>
        [HttpPost("{tagID}")]
        [PermissionNeeded(AppPermission.APP_UPLOAD)]
        public async Task<ApiResponse<Tag>> Update(ulong tagID, [FromBody] ExtendedTag tag) {
            Tag? etag = await _TagRepository.GetByID(tagID);
            if (etag == null) {
                return ApiNotFound<Tag>($"{nameof(Tag)} {tagID}");
            }

            tag.Name = tag.Name.ToLower().Trim();

            TagNameValidationResult nameValidation = _TagRepository.ValidateTagName(tag.Name);
            if (nameValidation.Valid == false) {
                return ApiBadRequest<Tag>($"name validation failed: {nameValidation.Reason}");
            }

            Tag? existingTagByName = await _TagRepository.GetByName(tag.Name);
            if (existingTagByName != null && existingTagByName.ID != tag.ID) {
                return ApiBadRequest<Tag>($"cannot update {tagID}: tag with name of {tag.Name} already exists ({existingTagByName.ID})");
            }

            TagType? tagType = await _TagTypeRepository.GetByID(tag.TypeID);
            if (tagType == null) {
                return ApiBadRequest<Tag>($"cannot update {tagID}: no {nameof(TagType)} with ID of {tag.TypeID} exists");
            }

            if (etag.Name != tag.Name || etag.TypeID != tag.TypeID) {
                etag.Name = tag.Name;
                etag.TypeID = tag.TypeID;
                await _TagRepository.Update(etag);
            }

            TagInfo tagInfo = await _TagInfoRepository.GetOrCreateByID(tagID);
            if (tagInfo.Description != tag.Description) {
                tagInfo.Description = tag.Description;
                await _TagInfoRepository.Upsert(tagInfo);
            }

            return ApiOk(etag);
        }

        /// <summary>
        ///     submit a tag for recount
        /// </summary>
        /// <param name="tagID">ID of the tag to recount</param>
        /// <response code="200">
        ///     the <see cref="Tag"/> with <see cref="Tag.ID"/> of <paramref name="tagID"/>
        ///     was successfully queued for a usage recount. this does not mean that the recount
        ///     is complete
        /// </response>
        /// <response code="404">
        ///     no <see cref="Tag"/> with <see cref="Tag.ID"/> of <paramref name="tagID"/> exists
        /// </response>
        [HttpPost("{tagID}/recount")]
        [PermissionNeeded(AppPermission.APP_UPLOAD)]
        public async Task<ApiResponse> RecountTagUsage(ulong tagID) {
            Tag? tag = await _TagRepository.GetByID(tagID);
            if (tag == null) {
                return ApiNotFound($"{nameof(Tag)} {tagID}");
            }

            _Logger.LogInformation($"manually inserted tag for update [tagID={tagID}]");
            _TagInfoUpdateQueue.Queue(new TagInfoUpdateQueueEntry() {
                TagID = tagID
            });

            return ApiOk();
        }

    }
}
