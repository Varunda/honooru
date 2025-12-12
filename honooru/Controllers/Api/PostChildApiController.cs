using honooru.Models;
using honooru.Models.App;
using honooru.Models.Db;
using honooru.Services.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace honooru.Controllers.Api {

    [Route("/api/post-child")]
    [ApiController]
    public class PostChildApiController : ApiControllerBase {

        private readonly ILogger<PostChildApiController> _Logger;

        private readonly PostChildRepository _PostChildRepository;
        private readonly PostRepository _PostRepository;

        public PostChildApiController(ILogger<PostChildApiController> logger,
            PostChildRepository postChildRepository,
            PostRepository postRepository) {

            _Logger = logger;
            _PostChildRepository = postChildRepository;
            _PostRepository = postRepository;
        }

        [HttpGet("{parentID}/parent")]
        public async Task<ApiResponse<List<ExtendedPostChild>>> GetByParentID(ulong parentID) {
            Post? post = await _PostRepository.GetByID(parentID);
            if (post == null) {
                return ApiNotFound<List<ExtendedPostChild>>($"{nameof(Post)} {parentID}");
            }

            List<PostChild> children = await _PostChildRepository.GetByParentID(parentID);
            List<ExtendedPostChild> ex = await _CreateExteneded(children);

            return ApiOk(ex);
        }

        [HttpGet("{childID}/child")]
        public async Task<ApiResponse<List<ExtendedPostChild>>> GetByChildID(ulong childID) {
            Post? post = await _PostRepository.GetByID(childID);
            if (post == null) {
                return ApiNotFound<List<ExtendedPostChild>>($"{nameof(Post)} {childID}");
            }

            List<PostChild> children = await _PostChildRepository.GetByChildID(childID);
            List<ExtendedPostChild> ex = await _CreateExteneded(children);

            return ApiOk(ex);
        }

        [HttpPost]
        public async Task<ApiResponse<PostChild>> Create([FromQuery] ulong parentID, [FromQuery] ulong childID) {
            List<string> errors = new();
            if (parentID == 0) { errors.Add($"{nameof(parentID)} cannot be 0"); }
            if (childID == 0) { errors.Add($"{nameof(childID)} cannot be 0"); }
            if (parentID == childID) { errors.Add($"{nameof(parentID)} cannot equal {nameof(childID)} ({parentID}={childID})"); }
            if (errors.Count > 0) {
                return ApiBadRequest<PostChild>($"{string.Join("\n", errors)}");
            }

            Post? parent = await _PostRepository.GetByID(parentID);
            if (parent == null) {
                return ApiNotFound<PostChild>($"{nameof(Post)} {parentID}");
            }

            Post? child = await _PostRepository.GetByID(childID);
            if (child == null) {
                return ApiNotFound<PostChild>($"{nameof(Post)} {childID}");
            }

            List<PostChild> parentChildren = await _PostChildRepository.GetByParentID(parentID);
            if (parentChildren.Find(iter => iter.ChildPostID == childID) != null) {
                return ApiBadRequest<PostChild>($"{parentID} already has {childID} as a child");
            }

            PostChild rel = new();
            rel.ParentPostID = parent.ID;
            rel.ChildPostID = child.ID;

            await _PostChildRepository.Insert(rel);

            return ApiOk(rel);
        }

        [HttpDelete]
        public async Task<ApiResponse> Remove([FromQuery] ulong parentID, [FromQuery] ulong childID) {
            List<string> errors = new();
            if (parentID == 0) { errors.Add($"{nameof(parentID)} cannot be 0"); }
            if (childID == 0) { errors.Add($"{nameof(childID)} cannot be 0"); }
            if (parentID == childID) { errors.Add($"{nameof(parentID)} cannot equal {nameof(childID)} ({parentID}={childID})"); }
            if (errors.Count > 0) {
                return ApiBadRequest($"{string.Join("\n", errors)}");
            }

            Post? parent = await _PostRepository.GetByID(parentID);
            if (parent == null) {
                return ApiNotFound($"{nameof(Post)} {parentID}");
            }

            Post? child = await _PostRepository.GetByID(childID);
            if (child == null) {
                return ApiNotFound($"{nameof(Post)} {childID}");
            }

            List<PostChild> parentChildren = await _PostChildRepository.GetByParentID(parentID);
            if (parentChildren.Find(iter => iter.ChildPostID == childID) == null) {
                return ApiBadRequest($"{childID} is not a parent of {parentID}");
            }

            PostChild rel = new();
            rel.ParentPostID = parent.ID;
            rel.ChildPostID = child.ID;

            await _PostChildRepository.Remove(rel);

            return ApiOk();
        }

        private async Task<List<ExtendedPostChild>> _CreateExteneded(List<PostChild> rels) {
            List<ExtendedPostChild> ex = new();

            foreach (PostChild pc in rels) {
                ExtendedPostChild et = new();
                et.PostChild = pc;
                et.Parent = await _PostRepository.GetByID(pc.ParentPostID);
                et.Child = await _PostRepository.GetByID(pc.ChildPostID);

                ex.Add(et);
            }

            return ex;
        }

    }
}
