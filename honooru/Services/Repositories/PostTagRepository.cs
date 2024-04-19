using honooru.Models.App;
using honooru.Models.Db;
using honooru.Services.Db;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace honooru.Services.Repositories {

    public class PostTagRepository {

        private readonly ILogger<PostTagRepository> _Logger;

        private readonly PostTagDb _PostTagDb;

        private readonly IMemoryCache _Cache;

        private const string CACHE_KEY_POST = "PostTag.Post.{0}"; // {0} => post ID

        public PostTagRepository(ILogger<PostTagRepository> logger,
            PostTagDb postTagDb, IMemoryCache cache) {

            _Logger = logger;
            _PostTagDb = postTagDb;
            _Cache = cache;
        }

        /// <summary>
        ///     get the <see cref="PostTag"/>s of a <see cref="Post"/> (cached)
        /// </summary>
        /// <param name="postID">ID of the <see cref="Post"/> to get the <see cref="PostTag"/>s of</param>
        /// <returns>
        ///     all <see cref="PostTag"/>s with <see cref="PostTag.PostID"/> of <paramref name="postID"/>
        /// </returns>
        public async Task<List<PostTag>> GetByPostID(ulong postID) {
            string cacheKey = string.Format(CACHE_KEY_POST, postID);
            if (_Cache.TryGetValue(cacheKey, out List<PostTag>? tags) == true && tags != null) {
                return tags;
            }

            tags = await _PostTagDb.GetByPostID(postID);

            _Cache.Set(cacheKey, tags, new MemoryCacheEntryOptions() {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            });

            return tags;
        }

        public Task<List<PostTag>> GetByTagID(ulong tagID) {
            return _PostTagDb.GetByTagID(tagID);
        }

        public Task Insert(PostTag tag) {
            string cacheKey = string.Format(CACHE_KEY_POST, tag.PostID);
            _Cache.Remove(cacheKey);

            return _PostTagDb.Insert(tag);
        }

        public Task Delete(PostTag tag) {
            return Delete(tag.PostID, tag.TagID);
        }

        public Task Delete(ulong postID, ulong tagID) {
            string cacheKey = string.Format(CACHE_KEY_POST, postID);
            _Cache.Remove(cacheKey);

            return _PostTagDb.Delete(postID, tagID);
        }

    }
}
