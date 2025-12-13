using honooru.Models;
using honooru.Models.Api;
using honooru.Models.App;
using honooru.Models.Config;
using honooru.Models.Db;
using honooru.Services.Db;
using honooru.Services.Hosted.Startup;
using honooru.Services.Util;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace honooru.Services.Repositories {

    public class PostRepository {

        private readonly ILogger<PostRepository> _Logger;
        private readonly PostTagRepository _PostTagRepository;
        private readonly PostDb _PostDb;
        private readonly UserSearchCache _SearchCache;

        private readonly IOptions<StorageOptions> _StorageOptions;

        private readonly IMemoryCache _Cache;

        private const string CACHE_KEY_SEARCH = "Post.Search.{0}.{1}"; // {0} => user ID, {1} => hash key

        private readonly HashSet<string> _CachedKeys = new();

        public PostRepository(ILogger<PostRepository> logger,
            IMemoryCache cache, PostDb postDb,
            IOptions<StorageOptions> storageOptions, PostTagRepository postTagRepository,
            UserSearchCache searchCache) {

            _Logger = logger;

            _Cache = cache;
            _PostDb = postDb;
            _StorageOptions = storageOptions;
            _PostTagRepository = postTagRepository;
            _SearchCache = searchCache;
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

        /// <summary>
        ///     perform a post search based on a query given
        /// </summary>
        /// <param name="query">query being performed</param>
        /// <param name="user">user that is performing the query. used to know if hiding unsafe or explicit posts is needed</param>
        /// <returns></returns>
        public async Task<List<Post>> Search(SearchQuery query, AppAccount user) {
            // the whole result of a DB search is cached, and the offset//limit are taken after
            if (_SearchCache.TryGet(user.ID, query.HashKey, out List<Post>? posts) == false || posts == null) {
                _Logger.LogDebug($"performing DB search as results are not cached [user={user.ID}/{user.Name}]");
                // not cached, do the search
                posts = await _PostDb.Search(query, user);

                _SearchCache.Add(user.ID, query.HashKey, posts);
            }

            posts = posts.Skip((int)query.Offset)
                .Take((int)query.Limit).ToList();

            return posts;
        }

        public Task<ulong> Insert(Post post) {
            _SearchCache.Clear();
            return _PostDb.Insert(post);
        }

        public Task Update(ulong postID, Post post) {
            _SearchCache.Clear();
            return _PostDb.Update(postID, post);
        }

        public async Task Delete(ulong postID) {
            await _PostDb.UpdateStatus(postID, PostStatus.DELETED);
            _SearchCache.Clear();
        }

        public async Task Restore(ulong postID) {
            await _PostDb.UpdateStatus(postID, PostStatus.OK);
            _SearchCache.Clear();
        }

        public async Task Erase(ulong postID) {
            Post? post = await GetByID(postID);
            if (post == null) {
                return;
            }

            _SearchCache.Clear();

            _DeletePossiblePath(Path.Combine(_StorageOptions.Value.RootDirectory, "original", post.FileLocation));

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
