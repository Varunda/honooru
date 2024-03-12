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
    [Route("/api/tag")]
    public class TagApiController : ApiControllerBase {

        private readonly ILogger<TagApiController> _Logger;

        private readonly TagRepository _TagRepository;
        private readonly TagTypeRepository _TagTypeRepository;
        private readonly TagInfoRepository _TagInfoRepository;

        public TagApiController(ILogger<TagApiController> logger,
            TagRepository tagRepository, TagTypeRepository tagTypeRepository,
            TagInfoRepository tagInfoRepository) {

            _Logger = logger;

            _TagRepository = tagRepository;
            _TagTypeRepository = tagTypeRepository;
            _TagInfoRepository = tagInfoRepository;
        }

        [HttpGet("{tagID}")]
        public async Task<ApiResponse<Tag>> GetByID(ulong tagID) {
            Tag? tag = await _TagRepository.GetByID(tagID);
            if (tag == null) {
                return ApiNoContent<Tag>();
            }

            return ApiOk(tag);
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

    }
}
