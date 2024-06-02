using Emzi0767.Utilities;
using honooru.Models.Api;
using honooru.Models.App;
using honooru.Services.Db;
using honooru.Services.Util;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace honooru.Services.Repositories {

    /// <summary>
    ///     repository service for managing <see cref="Tag"/>s
    /// </summary>
    public class TagRepository {

        private readonly ILogger<TagRepository> _Logger;
        private readonly IMemoryCache _Cache;

        private const string CACHE_KEY_SEARCH = "Tag.Search.{0}"; // {0} => name
        private const string CACHE_KEY_ALL_DICT = "Tag.All.Dict";

        // when a tag is updated, the tag search results must be evicted
        private readonly HashSet<string> _CachedKeys = new();

        private readonly TagDb _TagDb;
        private readonly TagInfoRepository _TagInfoRepository;
        private readonly TagAliasRepository _TagAliasRepository;
        private readonly TagTypeRepository _TagTypeRepository;
        private readonly TagValidationService _TagValidation;

        public TagRepository(ILogger<TagRepository> logger, IMemoryCache cache,
            TagDb tagDb, TagInfoRepository tagInfoRepository,
            TagAliasRepository tagAliasRepository, TagTypeRepository tagTypeRepository,
            TagValidationService tagValidation) {

            _Logger = logger;
            _Cache = cache;

            _TagDb = tagDb;
            _TagInfoRepository = tagInfoRepository;
            _TagAliasRepository = tagAliasRepository;
            _TagTypeRepository = tagTypeRepository;
            _TagValidation = tagValidation;
        }

        /// <summary>
        ///     get a list of all <see cref="Tag"/>s
        /// </summary>
        /// <param name="cancel">cancellation token</param>
        /// <returns></returns>
        public async Task<List<Tag>> GetAll(CancellationToken cancel) {
            return (await _GetDictionary()).Values.ToList();
        }

        /// <summary>
        ///     get a list of all <see cref="Tag"/>s
        /// </summary>
        /// <returns></returns>
        public Task<List<Tag>> GetAll() {
            return GetAll(CancellationToken.None);
        }

        /// <summary>
        ///     get the list of <see cref="Tag"/>s but as a dictionary
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private async Task<Dictionary<ulong, Tag>> _GetDictionary() {
            if (_Cache.TryGetValue(CACHE_KEY_ALL_DICT, out Dictionary<ulong, Tag>? dict) == false || dict == null) {
                List<Tag> tags = await _TagDb.GetAll();

                dict = tags.ToDictionary(iter => iter.ID);
                _Cache.Set(CACHE_KEY_ALL_DICT, dict, new MemoryCacheEntryOptions() {
                    SlidingExpiration = TimeSpan.FromMinutes(30)
                });
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

        /// <summary>
        ///     get an existing <see cref="Tag"/> by its <see cref="Tag.Name"/>,
        ///     or <c>null</c> if it does not exist
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public async Task<Tag?> GetByName(string name) {
            TagAlias? alias = await _TagAliasRepository.GetByAlias(name);

            if (alias != null) {
                Tag? aliasedTag = await GetByID(alias.TagID);
                if (aliasedTag == null) {
                    _Logger.LogWarning($"missing aliased tag [alias={name}] [tagID={alias.TagID}]");
                } else {
                    return aliasedTag;
                }
            }

            return await _TagDb.GetByName(name);
        }

        /// <summary>
        ///     get an existing <see cref="Tag"/> by <see cref="Tag.Name"/>,
        ///     and optionally update the <see cref="Tag.TypeID"/> to match <paramref name="type"/>,
        ///     or create a new one with a <see cref="Tag.TypeID"/> from <paramref name="type"/>, (default to 1)
        ///     if it does not exist
        /// </summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public async Task<Tag> GetOrCreateByName(string name, TagType? type) {
            name = name.ToLower().Trim();

            Tag? tag = await GetByName(name);
            if (tag == null) {
                tag = new Tag() {
                    Name = name,
                    TypeID = type?.ID ?? 1
                };

                _Logger.LogTrace($"creating new tag [name={name}] [typeID={tag.TypeID}]");
                tag.ID = await Insert(tag);
            } else if (type != null && tag.TypeID != type.ID) {
                _Logger.LogTrace($"updating tag type [name={name}] [ID={tag.ID}] [type.Name={type.Name}]");
                tag.TypeID = type.ID;
                await Update(tag);
            }

            return tag;
        }

        /// <summary>
        ///     perform a search for tags based on name. searching is based on 
        ///     if the tag name contains <paramref name="name"/>, or the levenshtein dist
        /// </summary>
        /// <param name="name"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        public async Task<List<TagSearchResult>> SearchByName(string name, CancellationToken cancel) {
            string cacheKey = string.Format(CACHE_KEY_SEARCH, name);

            if (_Cache.TryGetValue(cacheKey, out List<TagSearchResult>? ret) == false || ret == null) {
                _Logger.LogDebug($"search results not cached, performing search [name={name}]");
                ret = new List<TagSearchResult>();

                List<Tag> tags = await GetAll(cancel);

                Stopwatch timer = Stopwatch.StartNew();

                foreach (Tag tag in tags) {
                    cancel.ThrowIfCancellationRequested();

                    if (TagNameCloseEnough(name, tag.Name)) {
                        ret.Add(new TagSearchResult(tag));
                    }
                }

                HashSet<ulong> aliasTagIds = new();
                List<TagAlias> foundAliases = new();
                List<TagAlias> aliases = await _TagAliasRepository.GetAll();
                foreach (TagAlias a in aliases) {
                    cancel.ThrowIfCancellationRequested();

                    if (TagNameCloseEnough(name, a.Alias)) {
                        if (aliasTagIds.Contains(a.TagID) == false) {
                            aliasTagIds.Add(a.TagID);
                            foundAliases.Add(a);
                        }
                    }
                }

                if (aliasTagIds.Count > 0) {
                    Dictionary<ulong, Tag> aliasedTags = (await GetByIDs(aliasTagIds)).ToDictionary(iter => iter.ID);
                    HashSet<ulong> alreadyInList = new(ret.Select(iter => iter.Tag.ID));

                    foreach (TagAlias t in foundAliases) {
                        if (alreadyInList.Contains(t.TagID) == false) {
                            Tag? aliasedTag = aliasedTags.GetValueOrDefault(t.TagID);
                            if (aliasedTag == null) {
                                _Logger.LogError($"failed to find {nameof(Tag)} {t.TagID} from alias {t.Alias}");
                            } else {
                                ret.Add(new TagSearchResult(aliasedTag, t));
                            }
                        }
                    }
                }

                _Logger.LogDebug($"search results completed [timeMs={timer.ElapsedMilliseconds}ms] [results={ret.Count}] [name={name}]");

                _Cache.Set(cacheKey, ret, new MemoryCacheEntryOptions() {
                    SlidingExpiration = TimeSpan.FromMinutes(10)
                });

                _CachedKeys.Add(cacheKey);
            }

            return ret;
        }

        private bool TagNameCloseEnough(string input, string tagName) {
            if (tagName.Length > input.Length && tagName.Contains(input)) {
                return true;
            }

            // if the search term has 2 more characters than the tag name, then the
            // distance will always be greater than 2, while we only care about distances <= 1
            if (input.Length > tagName.Length + 2) {
                return false;
            }

            // if the tag name is longer than the input name, use a substring starting from the start to check
            // while this isn't perfect (we could instead get all possible substrings and compare those)
            // using stringe distance is more meant as a fallback
            if (tagName.Length > input.Length + 2) {
                tagName = tagName[0..(input.Length + 2)];
            }

            int distance = DamerauLevenshteinDistance(input, tagName, 2);
            if (distance <= 1) {
                return true;
            }

            return false;
        }

        /// <summary>
        ///     perform a validation on a name
        /// </summary>
        /// <param name="tagName"></param>
        /// <returns></returns>
        public TagNameValidationResult ValidateTagName(string tagName) {
            return _TagValidation.ValidateTagName(tagName);
        }

        /// <summary>
        ///     create <see cref="ExtendedTag"/>s from <see cref="Tag"/>s
        /// </summary>
        /// <param name="tags">list of <see cref="Tag"/>s to convert into a list of <see cref="ExtendedTag"/>s</param>
        /// <returns></returns>
        public async Task<List<ExtendedTag>> CreateExtended(IEnumerable<Tag> tags) {
            List<ExtendedTag> ex = new();

            foreach (Tag tag in tags) {
                ex.Add(await CreateExtended(tag));
            }

            return ex;
        }

        /// <summary>
        ///     create an <see cref="ExtendedTag"/> from a <see cref="Tag"/>
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        public async Task<ExtendedTag> CreateExtended(Tag tag) {
            TagInfo? info = await _TagInfoRepository.GetByID(tag.ID);
            TagType? type = await _TagTypeRepository.GetByID(tag.TypeID);

            ExtendedTag et = new();
            et.ID = tag.ID;
            et.Name = tag.Name;
            et.TypeID = tag.TypeID;

            et.TypeName = type?.Name ?? $"<missing {tag.TypeID}>";
            et.HexColor = type?.HexColor ?? "000000";
            et.TypeOrder = type?.Order ?? 0;

            et.Uses = info?.Uses ?? 0;
            et.Description = info?.Description;

            return et;
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

        /// <summary>
        ///     delete a <see cref="Tag"/>
        /// </summary>
        /// <param name="tagID">ID of the <see cref="Tag"/> to delete</param>
        /// <returns></returns>
        public Task Delete(ulong tagID) {
            _ClearCache();
            return _TagDb.Delete(tagID);
        }

        private void _ClearCache() {
            _Cache.Remove(CACHE_KEY_ALL_DICT);

            _Logger.LogTrace($"removing cached tag name searches as well [CachedKeys.Count={_CachedKeys.Count}]");
            foreach (string key in _CachedKeys) {
                _Cache.Remove(key);
            }
            _CachedKeys.Clear();

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
