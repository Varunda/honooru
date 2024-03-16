using honooru.Models;
using honooru.Models.App;
using honooru.Services.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace honooru.Controllers.Api {

    [Route("/api/tag-alias")]
    [ApiController]
    public class TagAliasApiController : ApiControllerBase {

        private readonly ILogger<TagAliasApiController> _Logger;
        private readonly TagAliasRepository _TagAliasRepository;
        private readonly TagRepository _TagRepository;

        public TagAliasApiController(ILogger<TagAliasApiController> logger,
            TagAliasRepository tagAliasRepository, TagRepository tagRepository) {

            _Logger = logger;

            _TagAliasRepository = tagAliasRepository;
            _TagRepository = tagRepository;
        }

        /// <summary>
        ///     get the <see cref="TagAlias"/>es of a specific <see cref="Tag"/>
        /// </summary>
        /// <param name="tagID">ID of the <see cref="Tag"/> to get the aliases of</param>
        /// <response code="200">
        ///     the response will contain a list of <see cref="TagAlias"/> with <see cref="TagAlias.TagID"/>
        ///     of <paramref name="tagID"/>
        /// </response>
        [HttpGet("{tagID}")]
        public async Task<ApiResponse<List<TagAlias>>> GetByTagID(ulong tagID) {
            List<TagAlias> aliases = await _TagAliasRepository.GetByTagID(tagID);

            return ApiOk(aliases);
        }

        [HttpPost("{tagID}")]
        public async Task<ApiResponse<TagAlias>> Create([FromQuery] string alias, ulong tagID) {
            alias = alias.Trim().ToLower();

            TagNameValidationResult valid = _TagRepository.ValidateTagName(alias);
            if (valid.Valid == false) {
                return ApiBadRequest<TagAlias>($"the alias '{alias}' is invalid: {valid.Reason}");
            }

            Tag? existingTag = await _TagRepository.GetByName(alias);
            if (existingTag != null) {
                return ApiBadRequest<TagAlias>($"tag {existingTag.ID} already exists with a name of {alias}");
            }

            Tag? targetTag = await _TagRepository.GetByID(tagID);
            if (targetTag == null) {
                return ApiNotFound<TagAlias>($"{nameof(Tag)} {tagID}");
            }

            TagAlias a = new();
            a.Alias = alias;
            a.TagID = tagID;

            await _TagAliasRepository.Insert(a);

            return ApiOk(a);
        }

        [HttpDelete]
        public async Task<ApiResponse> Delete([FromQuery] string alias) {
            alias = alias.Trim().ToLower();

            TagAlias? ta = await _TagAliasRepository.GetByAlias(alias);
            if (ta == null) {
                return ApiNotFound($"{nameof(TagAlias)} {alias}");
            }

            await _TagAliasRepository.Delete(ta);

            return ApiOk();
        }


    }
    
}
