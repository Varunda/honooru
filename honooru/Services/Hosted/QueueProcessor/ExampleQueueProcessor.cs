using honooru.Models.Queues;
using honooru.Services.Queues;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace watchtower.Services.Hosted.QueueProcessor {

    public class ExampleQueueProcessor : BackgroundService {

        private readonly ILogger<ExampleQueueProcessor> _Logger;

        private readonly ExampleQueue _Queue;

        public ExampleQueueProcessor(ILogger<ExampleQueueProcessor> logger,
            ExampleQueue queue) {

            _Logger = logger;
            _Queue = queue;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            while (stoppingToken.IsCancellationRequested == false) {

                ExampleQueueEntry entry = await _Queue.Dequeue(stoppingToken);

                _Logger.LogDebug($"pulled entry {entry.ID} from {entry.Timestamp:u} off of queue");
            }
        }

    }
}
