using honooru.Models.App.MediaUploadStep;
using System.Threading;
using System.Threading.Tasks;

namespace honooru.Services.UploadStepHandler {

    public interface IUploadStepHandler<T> where T : IUploadStep {

        Task Run(T entry, CancellationToken cancel);

    }
}
