using honooru.Models.App;
using honooru.Services.Db;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace honooru.Services.Repositories {

    public class TagRepository {

        private readonly ILogger<TagRepository> _Logger;
        private readonly IMemoryCache _Cache;

        private const string CACHE_KEY_SEARCH = "tag.search.{0}"; // {0} => name
        private const string CACHE_KEY_ALL_LIST = "tag.all.list";
        private const string CACHE_KEY_ALL_DICT = "tag.all.dict";

        private readonly TagDb _TagDb;
        private readonly TagInfoRepository _TagInfoRepository;

        public TagRepository(ILogger<TagRepository> logger, IMemoryCache cache,
            TagDb tagDb, TagInfoRepository tagInfoRepository) {

            _Logger = logger;
            _Cache = cache;

            _TagDb = tagDb;
            _TagInfoRepository = tagInfoRepository;
        }

        public async Task<List<Tag>> GetAll(CancellationToken cancel) {
            if (_Cache.TryGetValue(CACHE_KEY_ALL_LIST, out List<Tag> tags) == false) {
                _Cache.Remove(CACHE_KEY_ALL_DICT); // invalidate this cache as well

                tags = await _TagDb.GetAll(cancel);

                _Cache.Set(CACHE_KEY_ALL_LIST, tags, new MemoryCacheEntryOptions() {
                    SlidingExpiration = TimeSpan.FromMinutes(30)
                });

                Dictionary<ulong, Tag> dict = tags.ToDictionary(iter => iter.ID);
                _Cache.Set(CACHE_KEY_ALL_DICT, dict, new MemoryCacheEntryOptions() {
                    SlidingExpiration = TimeSpan.FromMinutes(30)
                });
            }

            return tags;
        }

        public Task<List<Tag>> GetAll() {
            return GetAll(CancellationToken.None);
        }

        /// <summary>
        ///     get the list of <see cref="Tag"/>s but as a dictionary
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private async Task<Dictionary<ulong, Tag>> _GetDictionary() {
            if (_Cache.TryGetValue(CACHE_KEY_ALL_DICT, out Dictionary<ulong, Tag> dict) == false) {
                await GetAll();
            }

            if (_Cache.TryGetValue(CACHE_KEY_ALL_DICT, out dict) == false) {
                throw new Exception($"calling {nameof(GetAll)} did NOT cache the dictionary");
            }

            return dict;
        }

        /// <summary>
        ///     get a <see cref="Tag"/> by its <see cref="Tag.ID"/>
        /// </summary>
        /// <param name="tagID">ID of the <see cref="Tag"/> to get</param>
        /// <returns>
        ///     the <see cref="Tag"/> with <see cref="Tag.ID"/> of <paramref name="tagID"/>,
        ///     or <c>null</c> if it does not exist
        /// </returns>
        public async Task<Tag?> GetByID(ulong tagID) {
            Dictionary<ulong, Tag> dict = await _GetDictionary();

            return dict.GetValueOrDefault(tagID);
        }

        /// <summary>
        ///     get all <see cref="Tag"/>s with an <see cref="Tag.ID"/> in <paramref name="tagIds"/>
        /// </summary>
        /// <param name="tagIds"></param>
        /// <returns></returns>
        public async Task<List<Tag>> GetByIDs(IEnumerable<ulong> tagIds) {
            List<Tag> tags = new(tagIds.Count());

            foreach (ulong tagId in tagIds) {
                Tag? t = await GetByID(tagId);
                if (t != null) {
                    tags.Add(t);
                }
            }

            return tags;
        }

        public Task<Tag?> GetByName(string name) {
            return _TagDb.GetByName(name);
        }

        /// <summary>
        ///     perform a search for tags based on name. searching is based on 
        ///     if the tag name contains <paramref name="name"/>, or the levenshtein dist
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public async Task<List<Tag>> SearchByName(string name, CancellationToken cancel) {
            string cacheKey = string.Format(CACHE_KEY_SEARCH, name);

            if (_Cache.TryGetValue(cacheKey, out List<Tag> ret) == false) {
                _Logger.LogDebug($"search results not cached, performing search [name={name}]");
                ret = new List<Tag>();

                List<Tag> tags = await GetAll(cancel);

                Stopwatch timer = Stopwatch.StartNew();

                foreach (Tag tag in tags) {
                    cancel.ThrowIfCancellationRequested();

                    if (tag.Name.Length > name.Length && tag.Name.Contains(name)) {
                        ret.Add(tag);
                        continue;
                    }

                    // if the search term has 2 more characters than the tag name, then the
                    // distance will always be greater than 2, while we only care about distances <= 1
                    if (name.Length > tag.Name.Length + 2) {
                        continue;
                    }

                    // if the tag name is longer than the input name, use a substring starting from the start to check
                    // while this isn't perfect (we could instead get all possible substrings and compare those)
                    string tagName = tag.Name;
                    if (tagName.Length > name.Length + 2) {
                        tagName = tag.Name[0..(name.Length + 2)];
                    }

                    int distance = DamerauLevenshteinDistance(name, tagName, 2);
                    if (distance <= 1) {
                        ret.Add(tag);
                    }
                }

                _Logger.LogDebug($"search results completed [timeMs={timer.ElapsedMilliseconds}ms] [results={ret.Count}] [name={name}]");

                _Cache.Set(cacheKey, ret, new MemoryCacheEntryOptions() {
                    SlidingExpiration = TimeSpan.FromMinutes(10)
                });
            }

            return ret;
        }

        /// <summary>
        ///     perform a validation on a name
        /// </summary>
        /// <param name="tagName"></param>
        /// <returns></returns>
        public TagNameValidationResult ValidateTagName(string tagName) {
            tagName = tagName.ToLower();

            TagNameValidationResult res = new();
            res.Input = tagName;

            if (string.IsNullOrWhiteSpace(tagName)) {
                res.Valid = false;
                res.Reason = "name cannot be empty or only whitespace";
                return res;
            }

            for (int i = 0; i < tagName.Length; ++i) {
                char c = tagName[i];

                // don't allow empty paren tags such as name_()
                if (c == '(' && (i + 1 < tagName.Length) && (tagName[i + 1] == ')')) {
                    res.Valid = false;
                    res.Reason = $"cannot have an empty string within a pair of parenthesis (at index {i})";
                    return res;
                }

                // only valid characters in a tag
                if (
                    (c == '(' || c == ')')
                    || (c >= 'a' && c <= 'z')
                    || (c >= '0' && c <= '9')
                    || c == '_'
                    || c == '\''
                    || c == '.'
                    || c == '!'
                    ) {
                    continue;
                }

                res.Valid = false;
                res.Reason = $"invalid character '{c}'";
                return res;
            }

            res.Valid = true;
            return res;
        }

        /// <summary>
        ///     insert a new <see cref="Tag"/>
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        public async Task<ulong> Insert(Tag tag) {
            ulong tagID = await _TagDb.Insert(tag);
            _Logger.LogInformation($"created new tag [name={tag.Name}] [typeID={tag.TypeID}] [ID={tagID}]");

            TagInfo info = new();
            info.ID = tagID;
            info.Uses = 0;
            info.Description = null;
            await _TagInfoRepository.Upsert(info);

            _ClearCache();

            return tagID;
        }

        /// <summary>
        ///     update an existing <see cref="Tag"/>
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        public async Task<Tag> Update(Tag tag) {
            Tag t = await _TagDb.Update(tag);

            _ClearCache();

            return t;
        }

        private void _ClearCache() {
            _Cache.Remove(CACHE_KEY_ALL_LIST);
            _Cache.Remove(CACHE_KEY_ALL_DICT);
        }

        /// <summary>
        /// Computes the Damerau-Levenshtein Distance between two strings, represented as arrays of
        /// integers, where each integer represents the code point of a character in the source string.
        /// Includes an optional threshhold which can be used to indicate the maximum allowable distance.
        /// </summary>
        /// <remarks>
        ///     copied from https://stackoverflow.com/questions/9453731/how-to-calculate-distance-similarity-measure-of-given-2-strings/9454016#9454016 
        /// </remarks>
        /// <param name="source">An array of the code points of the first string</param>
        /// <param name="target">An array of the code points of the second string</param>
        /// <param name="threshold">Maximum allowable distance</param>
        /// <returns>Int.MaxValue if threshhold exceeded; otherwise the Damerau-Leveshteim distance between the strings</returns>
        public static int DamerauLevenshteinDistance(string source, string target, int threshold) {

            int length1 = source.Length;
            int length2 = target.Length;

            // Return trivial case - difference in string lengths exceeds threshhold
            if (Math.Abs(length1 - length2) > threshold) { return int.MaxValue; }

            // Ensure arrays [i] / length1 use shorter length 
            if (length1 > length2) {
                Swap(ref target, ref source);
                Swap(ref length1, ref length2);
            }

            int maxi = length1;
            int maxj = length2;

            int[] dCurrent = new int[maxi + 1];
            int[] dMinus1 = new int[maxi + 1];
            int[] dMinus2 = new int[maxi + 1];
            int[] dSwap;

            for (int i = 0; i <= maxi; i++) { dCurrent[i] = i; }

            int jm1 = 0, im1 = 0, im2 = -1;

            for (int j = 1; j <= maxj; j++) {

                // Rotate
                dSwap = dMinus2;
                dMinus2 = dMinus1;
                dMinus1 = dCurrent;
                dCurrent = dSwap;

                // Initialize
                int minDistance = int.MaxValue;
                dCurrent[0] = j;
                im1 = 0;
                im2 = -1;

                for (int i = 1; i <= maxi; i++) {

                    int cost = source[im1] == target[jm1] ? 0 : 1;

                    int del = dCurrent[im1] + 1;
                    int ins = dMinus1[i] + 1;
                    int sub = dMinus1[im1] + cost;

                    //Fastest execution for min value of 3 integers
                    int min = (del > ins) ? (ins > sub ? sub : ins) : (del > sub ? sub : del);

                    if (i > 1 && j > 1 && source[im2] == target[jm1] && source[im1] == target[j - 2])
                        min = Math.Min(min, dMinus2[im2] + cost);

                    dCurrent[i] = min;
                    if (min < minDistance) { minDistance = min; }
                    im1++;
                    im2++;
                }
                jm1++;
                if (minDistance > threshold) { return int.MaxValue; }
            }

            int result = dCurrent[maxi];
            return (result > threshold) ? int.MaxValue : result;
        }

        static void Swap<T>(ref T arg1,ref T arg2) {
            T temp = arg1;
            arg1 = arg2;
            arg2 = temp;
        }

    }
}
