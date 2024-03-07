using honooru.Models.Queues;
using Microsoft.Extensions.Logging;

namespace honooru.Services.Queues {

    public class ExampleQueue : BaseQueue<ExampleQueueEntry> {

        public ExampleQueue(ILoggerFactory factory)
            : base(factory) { }

    }
}
