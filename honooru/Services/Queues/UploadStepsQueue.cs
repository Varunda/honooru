using honooru.Models.App.MediaUploadStep;
using honooru.Models.Queues;
using Microsoft.Extensions.Logging;

namespace honooru.Services.Queues {

    public class UploadStepsQueue : BaseQueue<UploadSteps> {

        public UploadStepsQueue(ILoggerFactory factory) : base(factory) { }

    }

}
