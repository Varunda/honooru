using Microsoft.Extensions.Logging;

namespace honooru.Services.Repositories {

    public class PostTagRepository {

        private readonly ILogger<PostTagRepository> _Logger;

        public PostTagRepository(ILogger<PostTagRepository> logger) {
            _Logger = logger;
        }

    }
}
