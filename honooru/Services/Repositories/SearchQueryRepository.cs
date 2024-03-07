using honooru.Models.Api;
using honooru.Models.App;
using honooru.Models.Search;
using honooru.Services.Db;
using honooru.Services.Parsing;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace honooru.Services.Repositories {

    public class SearchQueryRepository {

        private readonly ILogger<SearchQueryRepository> _Logger;

        private readonly TagDb _TagDb;
        private readonly PostDb _PostDb;
        private readonly TagTypeDb _TagTypeDb;
        private readonly PostTagDb _PostTagDb;

        public SearchQueryRepository(ILogger<SearchQueryRepository> logger,
            SearchTokenizer tokenizer, AstBuilder astBuilder,
            TagDb tagDb, PostDb postDb,
            TagTypeDb tagTypeDb, PostTagDb postTagDb) {

            _Logger = logger;

            _TagDb = tagDb;
            _PostDb = postDb;
            _TagTypeDb = tagTypeDb;
            _PostTagDb = postTagDb;
        }

        public async Task<NpgsqlCommand> Compile(Ast ast) {
            throw new NotImplementedException();
        }

    }
}
