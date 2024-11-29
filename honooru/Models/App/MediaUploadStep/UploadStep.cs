using honooru.Models.Config;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace honooru.Models.App.MediaUploadStep {

    public class UploadStep {

        public UploadStep(Guid assetID, StorageOptions options) {
            AssetID = assetID;
            StorageOptions = options;
        }

        public readonly Guid AssetID;

        public readonly StorageOptions StorageOptions;

    }

    public abstract class UploadStepWorker {

        public abstract Task<bool> Run(Guid assetID, Action<decimal> updateProgress, CancellationToken cancel);

    }

    public interface IUploadStepWorker { }

    public interface IUploadStep {

        string Name { get; }

    }

    public interface IUploadStepWorker<TOrder, TWorker> : IUploadStepWorker
        where TOrder : IUploadStep<TOrder, TWorker>
        where TWorker : IUploadStepWorker<TOrder, TWorker> {

        /// <summary>
        ///     run the worker with the parameters needed
        /// </summary>
        /// <param name="order">what <see cref="IUploadStep"/> to work on</param>
        /// <param name="asset">asset that is being worked on. can be modified within the step</param>
        /// <param name="updateProgress">useful for long running tasks (such as reencoding), use this callback to update the progress of the step</param>
        /// <param name="cancel">cancellation token</param>
        /// <returns>
        ///     true if the next <see cref="IUploadStep"/> is to be ran or not
        /// </returns>
        Task<bool> Run(TOrder order, MediaAsset asset, Action<decimal> updateProgress, CancellationToken cancel);
    
    }

    public interface IUploadStep<TOrder, TWorker> : IUploadStep
        where TOrder : IUploadStep<TOrder, TWorker>
        where TWorker : IUploadStepWorker<TOrder, TWorker> { }

}
