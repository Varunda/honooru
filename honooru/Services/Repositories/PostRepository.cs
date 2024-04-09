using honooru.Models;
using honooru.Models.Api;
using honooru.Models.Config;
using honooru.Models.Db;
using honooru.Services.Db;
using honooru.Services.Hosted.Startup;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace honooru.Services.Repositories {

    public class PostRepository {

        private readonly ILogger<PostRepository> _Logger;
        private readonly IMemoryCache _Cache;

        private readonly PostDb _PostDb;
        private readonly IOptions<StorageOptions> _StorageOptions;

        public PostRepository(ILogger<PostRepository> logger,
            IMemoryCache cache, PostDb postDb,
            IOptions<StorageOptions> storageOptions) {

            _Logger = logger;

            _Cache = cache;
            _PostDb = postDb;
            _StorageOptions = storageOptions;
        }

        public Task<List<Post>> GetAll() {
            return _PostDb.GetAll();
        }

        public Task<Post?> GetByID(ulong postID) {
            return _PostDb.GetByID(postID);
        }

        public Task<Post?> GetByMD5(string md5) {
            return _PostDb.GetByMD5(md5);
        }

        public Task<List<Post>> Search(SearchQuery query, AppAccount user) {
            return _PostDb.Search(query, user);
        }

        public Task<ulong> Insert(Post post) {
            return _PostDb.Insert(post);
        }

        public Task Update(ulong postID, Post post) {
            return _PostDb.Update(postID, post);
        }

        public async Task Delete(ulong postID) {
            Post? post = await GetByID(postID);
            if (post == null) {
                return;
            }

            await _PostDb.UpdateStatus(postID, PostStatus.DELETED);
        }

        public async Task Earse(ulong postID) {
            Post? post = await GetByID(postID);
            if (post == null) {
                return;
            }

            await _PostDb.Delete(postID);

            _DeletePossiblePath(Path.Combine(_StorageOptions.Value.RootDirectory, "original", post.MD5 + "." + post.FileExtension));
        }

        private void _DeletePossiblePath(string path) {
            _Logger.LogTrace($"checking if deletion for file is needed [path={path}]");
            if (File.Exists(path) == true) {
                _Logger.LogDebug($"deleting file for media asset [path={path}]");
                File.Delete(path);
            }
        }

    }
}
