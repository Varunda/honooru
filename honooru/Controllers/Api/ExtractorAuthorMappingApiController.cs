using honooru.Models;
using honooru.Models.App;
using honooru.Services.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace honooru.Controllers.Api {

    [ApiController]
    [Route("/api/extractor-author-mapping")]
    public class ExtractorAuthorMappingApiController : ApiControllerBase {

        private readonly ILogger<ExtractorAuthorMappingApiController> _Logger;
        private readonly ExtractorAuthorMappingRepository _AuthorMappingRepository;

        public ExtractorAuthorMappingApiController(ILogger<ExtractorAuthorMappingApiController> logger,
            ExtractorAuthorMappingRepository authorMappingRepository) {

            _Logger = logger;
            _AuthorMappingRepository = authorMappingRepository;
        }

        [HttpGet]
        public async Task<ApiResponse<List<ExtractorAuthorMapping>>> GetAll() {
            List<ExtractorAuthorMapping> mappings = await _AuthorMappingRepository.GetAll();

            return ApiOk(mappings);
        }

        [HttpPost]
        public async Task<ApiResponse> Create([FromBody] ExtractorAuthorMapping mapping) {
            if (string.IsNullOrWhiteSpace(mapping.Site)) {
                return ApiBadRequest($"missing site");
            }
            if (string.IsNullOrWhiteSpace(mapping.Author)) {
                return ApiBadRequest($"missing author");
            }
            if (mapping.TagID == 0) {
                return ApiBadRequest($"missing tag_id");
            }

            ExtractorAuthorMapping? existingMapping = await _AuthorMappingRepository.GetMapping(mapping.Site, mapping.Author);
            if (existingMapping != null) {
                return ApiBadRequest($"author mapping to {mapping.Author} on site {mapping.Site} already maps to tag {existingMapping.TagID}");
            }

            await _AuthorMappingRepository.Upsert(mapping);

            return ApiOk();
        }

    }
}
