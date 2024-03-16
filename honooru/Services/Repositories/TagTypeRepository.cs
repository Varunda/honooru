using honooru.Models.App;
using honooru.Services.Db;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace honooru.Services.Repositories {

    public class TagTypeRepository {

        private readonly ILogger<TagTypeRepository> _Logger;

        private readonly TagTypeDb _TagTypeDb;

        public TagTypeRepository(ILogger<TagTypeRepository> logger,
            TagTypeDb tagTypeDb) {

            _Logger = logger;
            _TagTypeDb = tagTypeDb;
        }

        public Task<List<TagType>> GetAll() {
            return _TagTypeDb.GetAll();
        }

        public Task<TagType?> GetByID(ulong typeID) {
            return _TagTypeDb.GetByID(typeID);
        }

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

    }
}
