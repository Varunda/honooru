using honooru.Models.Db;
using honooru.Services.Db;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace honooru.Services.Repositories {

    public class PostRepository {

        private readonly ILogger<PostRepository> _Logger;
        private readonly IMemoryCache _Cache;
        private readonly PostDb _PostDb;

        public PostRepository(ILogger<PostRepository> logger,
            IMemoryCache cache, PostDb postDb) {

            _Logger = logger;

            _Cache = cache;
            _PostDb = postDb;
        }

        public Task<Post?> GetByID(ulong postID) {
            return _PostDb.GetByID(postID);
        }

        public Task<Post?> GetByMD5(string md5) {
            return _PostDb.GetByMD5(md5);
        }

        public Task<ulong> Insert(Post post) {
            return _PostDb.Insert(post);
        }

        public Task Update(ulong postID, Post post) {
            return _PostDb.Update(postID, post);
        }

    }
}
