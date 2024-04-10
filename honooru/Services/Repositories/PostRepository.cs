using honooru.Models;
using honooru.Models.Api;
using honooru.Models.App;
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
        private readonly PostTagRepository _PostTagRepository;

        private readonly PostDb _PostDb;
        private readonly IOptions<StorageOptions> _StorageOptions;

        public PostRepository(ILogger<PostRepository> logger,
            IMemoryCache cache, PostDb postDb,
            IOptions<StorageOptions> storageOptions, PostTagRepository postTagRepository) {

            _Logger = logger;

            _Cache = cache;
            _PostDb = postDb;
            _StorageOptions = storageOptions;
            _PostTagRepository = postTagRepository;
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
            await _PostDb.UpdateStatus(postID, PostStatus.DELETED);
        }

        public async Task Restore(ulong postID) {
            await _PostDb.UpdateStatus(postID, PostStatus.OK);
        }

        public async Task Erase(ulong postID) {
            Post? post = await GetByID(postID);
            if (post == null) {
                return;
            }

            _DeletePossiblePath(Path.Combine(_StorageOptions.Value.RootDirectory, "original", post.MD5 + "." + post.FileExtension));

            List<PostTag> tags = await _PostTagRepository.GetByPostID(postID);
            _Logger.LogDebug($"deleting tags from post (due to erase) [postID={postID}] [tags.Count={tags.Count}]");
            foreach (PostTag tag in tags) {
                await _PostTagRepository.Delete(tag);
            }

            await _PostDb.Delete(postID);
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
