using honooru.Models.App;
using honooru.Services.Db;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace honooru.Services.Repositories {

    public class TagTypeRepository {

        private readonly ILogger<TagTypeRepository> _Logger;

        private readonly TagTypeDb _TagTypeDb;

        private readonly IMemoryCache _Cache;

        private const string CACHE_KEY_MAP = "TagType.All.Dictionary";

        public TagTypeRepository(ILogger<TagTypeRepository> logger,
            TagTypeDb tagTypeDb, IMemoryCache cache) {

            _Logger = logger;
            _TagTypeDb = tagTypeDb;
            _Cache = cache;
        }

        /// <summary>
        ///     get all <see cref="TagType"/>s
        /// </summary>
        /// <returns>
        ///     a list of <see cref="TagType"/>s
        /// </returns>
        public async Task<List<TagType>> GetAll() {
            return (await _GetDictionary()).Values.ToList();
        }

        private async Task<Dictionary<ulong, TagType>> _GetDictionary() {
            if (_Cache.TryGetValue(CACHE_KEY_MAP, out Dictionary<ulong, TagType>? dict) == false || dict == null) {
                List<TagType> types = await _TagTypeDb.GetAll();

                dict = types.ToDictionary(iter => iter.ID);

                _Cache.Set(CACHE_KEY_MAP, dict, new MemoryCacheEntryOptions() {
                    SlidingExpiration = TimeSpan.FromMinutes(10)
                });
            }

            return dict;
        }

        /// <summary>
        ///     get the <see cref="TagType"/> with <see cref="TagType.ID"/> of <paramref name="typeID"/>
        /// </summary>
        /// <param name="typeID">ID of the <see cref="TagType"/> to get</param>
        /// <returns>
        ///     the <see cref="TagType"/> with <see cref="TagType.ID"/> of <paramref name="typeID"/>,
        ///     or <c>null</c> if it does not exist
        /// </returns>
        public async Task<TagType?> GetByID(ulong typeID) {
            return (await _GetDictionary()).GetValueOrDefault(typeID);
        }

        /// <summary>
        ///     get a list of <see cref="TagType"/>s based on <see cref="TagType.ID"/>
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public async Task<List<TagType>> GetByIDs(IEnumerable<ulong> ids) {
            List<TagType> types = new();

            foreach (ulong id in ids) {
                TagType? type = await GetByID(id);
                if (type != null) {
                    types.Add(type);
                }
            }

            return types;
        }

        /// <summary>
        ///     get a <see cref="TagType"/> by its <see cref="TagType.Name"/> or <see cref="TagType.Alias"/>
        /// </summary>
        /// <param name="name">name or alias of the <see cref="TagType"/> to get</param>
        /// <returns>
        ///     the <see cref="TagType"/> with either <see cref="TagType.Name"/> or <see cref="TagType.Alias"/>
        ///     of <paramref name="name"/>, or <c>null</c> if it does not exist
        /// </returns>
        public async Task<TagType?> GetByNameOrAlias(string name) {
            List<TagType> types = await GetAll();

            foreach (TagType type in types) {
                if (type.Name.ToLower() == name.ToLower()) {
                    return type;
                }

                if (type.Alias.ToLower() == name.ToLower()) {
                    return type;
                }
            }

            return null;
        }

        /// <summary>
        ///     get a <see cref="TagType"/> by its <see cref="TagType.Name"/>
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public async Task<TagType?> GetByName(string name) {
            List<TagType> types = await GetAll();

            foreach (TagType type in types) {
                if (type.Name.ToLower() == name.ToLower()) {
                    return type;
                }
            }

            return null;
        }

        /// <summary>
        ///     get a <see cref="TagType"/> by its <see cref="TagType.Alias"/>
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public async Task<TagType?> GetByAlias(string name) {
            List<TagType> types = await GetAll();

            foreach (TagType type in types) {
                if (type.Alias.ToLower() == name.ToLower()) {
                    return type;
                }
            }

            return null;
        }

        /// <summary>
        ///     insert a new <see cref="TagType"/>
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public Task<ulong> Insert(TagType type) {
            _Cache.Remove(CACHE_KEY_MAP);
            return _TagTypeDb.Insert(type);
        }

        /// <summary>
        ///     update an existing <see cref="TagType"/>
        /// </summary>
        /// <param name="typeID"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public Task Update(ulong typeID, TagType type) {
            _Cache.Remove(CACHE_KEY_MAP);
            return _TagTypeDb.Update(typeID, type);
        }

    }
}
