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

        [HttpGet("{tagID}")]
        [PermissionNeeded(AppPermission.APP_VIEW)]
        public async Task<ApiResponse<Tag>> GetByID(ulong tagID) {
            Tag? tag = await _TagRepository.GetByID(tagID);
            if (tag == null) {
                return ApiNoContent<Tag>();
            }

            return ApiOk(tag);
        }

        [HttpGet("{tagID}/extended")]
        [PermissionNeeded(AppPermission.APP_VIEW)]
        public async Task<ApiResponse<ExtendedTag>> GetExtenedByID(ulong tagID) {
            Tag? tag = await _TagRepository.GetByID(tagID);
            if (tag == null) {
                return ApiNoContent<ExtendedTag>();
            }

            TagInfo? info = await _TagInfoRepository.GetByID(tagID);
            TagType? type = await _TagTypeRepository.GetByID(tag.TypeID);

            ExtendedTag et = new();
            et.ID = tag.ID;
            et.Name = tag.Name;
            et.TypeID = tag.TypeID;

            et.TypeName = type?.Name ?? $"<missing {tag.TypeID}>";
            et.HexColor = type?.HexColor ?? "000000";

            et.Uses = info?.Uses ?? 0;
            et.Description = info?.Description;

            return ApiOk(et);
        }

        /// <summary>
        ///     search for all tags that 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="limit"></param>
        /// <param name="sortBy"></param>
        /// <param name="sortAscending"></param>
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
