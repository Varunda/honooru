using honooru.Code;
using honooru.Models;
using honooru.Models.App;
using honooru.Models.Internal;
using honooru.Services.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace honooru.Controllers.Api {

    [ApiController]
    [Route("/api/tag-type")]
    public class TagTypeApiController : ApiControllerBase {

        private readonly ILogger<TagTypeApiController> _Logger;
        private readonly TagTypeRepository _TagTypeRepository;

        public TagTypeApiController(ILogger<TagTypeApiController> logger,
            TagTypeRepository tagTypeRepository) {

            _Logger = logger;
            _TagTypeRepository = tagTypeRepository;
        }

        /// <summary>
        ///     get all <see cref="TagType"/>s 
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [PermissionNeeded(AppPermission.APP_VIEW)]
        public async Task<ApiResponse<List<TagType>>> GetAll() {
            return ApiOk(await _TagTypeRepository.GetAll());
        }

        /// <summary>
        ///     create a new <see cref="TagType"/>
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        [HttpPost]
        [PermissionNeeded(AppPermission.APP_TAG_EDIT)]
        public async Task<ApiResponse<TagType>> Create([FromBody] TagType type) {
            List<string> errors = new();
            if (string.IsNullOrWhiteSpace(type.Name)) { errors.Add($"{nameof(TagType.Name)} cannot be empty"); }
            if (string.IsNullOrWhiteSpace(type.Alias)) { errors.Add($"{nameof(TagType.Alias)} cannot be empty"); }
            if (type.Order == 0) { errors.Add($"{nameof(TagType.Order)} cannot be 0"); }
            if (errors.Count > 0) {
                return ApiBadRequest<TagType>($"validation errors: {string.Join(", ", errors)}");
            }

            TagType? matchingType = await _FindExisting(null, type);
            if (matchingType != null) {
                return ApiBadRequest<TagType>($"{nameof(TagType)} {matchingType.ID} overlaps in name or alias with input {nameof(TagType)}");
            }

            ulong typeID = await _TagTypeRepository.Insert(type);
            type.ID = typeID;

            return ApiOk(type);
        }

        /// <summary>
        ///     update an existist <see cref="TagType"/> with new info
        /// </summary>
        /// <param name="typeID">ID of hte <see cref="TagType"/> to update</param>
        /// <param name="type">parameters used to update the <see cref="TagType"/> with</param>
        /// <returns></returns>
        [HttpPost("{typeID}")]
        [PermissionNeeded(AppPermission.APP_TAG_EDIT)]
        public async Task<ApiResponse<TagType>> Update(ulong typeID, [FromBody] TagType type) {
            TagType? tagType = await _TagTypeRepository.GetByID(typeID);
            if (tagType == null) {
                return ApiNotFound<TagType>($"{nameof(TagType)} {typeID}");
            }

            List<string> errors = new();
            if (string.IsNullOrWhiteSpace(type.Name)) { errors.Add($"{nameof(TagType.Name)} cannot be empty"); }
            if (string.IsNullOrWhiteSpace(type.Alias)) { errors.Add($"{nameof(TagType.Alias)} cannot be empty"); }
            if (type.Order == 0) { errors.Add($"{nameof(TagType.Order)} cannot be 0"); }
            if (errors.Count > 0) {
                return ApiBadRequest<TagType>($"validation errors: {string.Join(", ", errors)}");
            }

            TagType? matchingType = await _FindExisting(typeID, type);
            if (matchingType != null) {
                return ApiBadRequest<TagType>($"{nameof(TagType)} {matchingType.ID} overlaps in name or alias with input {nameof(TagType)}");
            }

            await _TagTypeRepository.Update(typeID, type);

            return ApiOk(type);
        }

        private async Task<TagType?> _FindExisting(ulong? ignoreTypeID, TagType type) {
            TagType? etype = await _TagTypeRepository.GetByName(type.Name.Trim());
            if (etype != null && (ignoreTypeID == null || etype.ID != ignoreTypeID.Value)) {
                return etype;
            }

            etype = await _TagTypeRepository.GetByName(type.Alias.Trim());
            if (etype != null && (ignoreTypeID == null || etype.ID != ignoreTypeID.Value)) {
                return etype;
            }

            etype = await _TagTypeRepository.GetByAlias(type.Name.Trim());
            if (etype != null && (ignoreTypeID == null || etype.ID != ignoreTypeID.Value)) {
                return etype;
            }

            etype = await _TagTypeRepository.GetByAlias(type.Alias.Trim());
            if (etype != null && (ignoreTypeID == null || etype.ID != ignoreTypeID.Value)) {
                return etype;
            }

            etype = await _TagTypeRepository.GetByName(type.Alias.Trim());
            if (etype != null && (ignoreTypeID == null || etype.ID != ignoreTypeID.Value)) {
                return etype;
            }

            return null;
        }

    }
}
