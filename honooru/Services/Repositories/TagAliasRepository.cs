using honooru.Models.App;
using honooru.Services.Db;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace honooru.Services.Repositories {

    public class TagAliasRepository {

        private readonly ILogger<TagAliasRepository> _Logger;

        private const string CACHE_KEY_ALL = "TagAlias.All";
        private readonly IMemoryCache _Cache;

        private readonly TagAliasDb _TagAliasDb;

        public TagAliasRepository(ILogger<TagAliasRepository> logger,
            IMemoryCache cache, TagAliasDb tagAliasDb) {

            _Logger = logger;

            _Cache = cache;
            _TagAliasDb = tagAliasDb;
        }

        public async Task<List<TagAlias>> GetAll() {
            if (_Cache.TryGetValue(CACHE_KEY_ALL, out List<TagAlias>? aliases) == false || aliases == null) {
                aliases = await _TagAliasDb.GetAll();

                _Cache.Set(CACHE_KEY_ALL, aliases, new MemoryCacheEntryOptions() {
                    SlidingExpiration = TimeSpan.FromMinutes(15)
                });
            }

            return aliases;
        }

        public Task<TagAlias?> GetByAlias(string alias) {
            return _TagAliasDb.GetByAlias(alias);
        }

        public Task<List<TagAlias>> GetByTagID(ulong tagID) {
            return _TagAliasDb.GetByTagID(tagID);
        }

        public Task Insert(TagAlias alias) {
            return _TagAliasDb.Insert(alias);
        }

        public Task Delete(TagAlias alias) {
            return _TagAliasDb.Delete(alias.Alias);
        }


    }
}
