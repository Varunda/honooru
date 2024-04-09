using honooru.Models.App;
using honooru.Services.Db;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace honooru.Services.Repositories {

    public class PostTagRepository {

        private readonly ILogger<PostTagRepository> _Logger;

        private readonly PostTagDb _PostTagDb;

        public PostTagRepository(ILogger<PostTagRepository> logger,
            PostTagDb postTagDb) {

            _Logger = logger;
            _PostTagDb = postTagDb;
        }

        public Task<List<PostTag>> GetByPostID(ulong postID) {
            return _PostTagDb.GetByPostID(postID);
        }

        public Task<List<PostTag>> GetByTagID(ulong tagID) {
            return _PostTagDb.GetByTagID(tagID);
        }

        public Task Insert(PostTag tag) {
            return _PostTagDb.Insert(tag);
        }

        public Task Delete(PostTag tag) {
            return Delete(tag.PostID, tag.TagID);
        }

        public Task Delete(ulong postID, ulong tagID) {
            return _PostTagDb.Delete(postID, tagID);
        }

    }
}
