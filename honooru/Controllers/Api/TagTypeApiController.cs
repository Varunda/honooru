using honooru.Models;
using honooru.Models.App;
using honooru.Services.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
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

        [HttpGet]
        public async Task<ApiResponse<List<TagType>>> GetAll() {
            return ApiOk(await _TagTypeRepository.GetAll());
        }


    }
}
