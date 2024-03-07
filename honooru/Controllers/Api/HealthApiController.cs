using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using honooru.Models;
using honooru.Models.Api;
using honooru.Models.Health;
using honooru.Services.Queues;

namespace honooru.Controllers.Api {

    [ApiController]
    [Route("api/health")]
    public class HealthApiController : ApiControllerBase {

        private readonly ILogger<HealthApiController> _Logger;
        private readonly IMemoryCache _Cache;

        private readonly DiscordMessageQueue _DiscordQueue;

        public HealthApiController(ILogger<HealthApiController> logger, IMemoryCache cache,
            DiscordMessageQueue discordQueue) {

            _Logger = logger;
            _Cache = cache;

            _DiscordQueue = discordQueue;
        }

        /// <summary>
        ///     Get an object that indicates how healthy the app is in various metrics
        /// </summary>
        /// <remarks>
        ///     Feel free to hammer this endpoint as much as you'd like. The results are cached for 800ms, and it only takes like 2ms to
        ///     get all the data, so hitting this endpoint is not a burden
        /// </remarks>
        /// <response code="200">
        ///     The response will contain a <see cref="AppHealth"/> that represents the health of the app at the time of being called
        /// </response>
        [HttpGet]
        public ApiResponse<AppHealth> GetRealtimeHealth() {
            if (_Cache.TryGetValue("App.Health", out AppHealth health) == false) {
                health = new AppHealth();
                health.Timestamp = DateTime.UtcNow;

                ServiceQueueCount discord = _MakeCount("discord_message_queue", _DiscordQueue);

                health.Queues = new List<ServiceQueueCount>() {
                    discord
                };

                _Cache.Set("App.Health", health, new MemoryCacheEntryOptions() {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMilliseconds(800)
                });
            }

            return ApiOk(health);
        }

        private ServiceQueueCount _MakeCount(string name, IProcessQueue queue) {
            ServiceQueueCount c = new() {
                QueueName = name,
                Count = queue.Count() ,
                Processed = queue.Processed()
            };

            List<long> times = queue.GetProcessTime();
            if (times.Count > 0) {
                c.Average = times.Average();
                c.Min = times.Min();
                c.Max = times.Max();

                List<long> sorted = times.OrderBy(i => i).ToList();
                int mid = sorted.Count / 2;
                if (sorted.Count % 2 == 0) {
                    c.Median = (sorted.ElementAt(mid - 1) + sorted.ElementAt(mid)) / 2;
                } else {
                    c.Median = sorted.ElementAt(mid);
                }
            }

            return c;
        }

    }
}
