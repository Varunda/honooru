using honooru.Models.Config;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace honooru.Models.App.MediaUploadStep {

    public class UploadStep {

        public UploadStep(MediaAsset asset, StorageOptions options) {
            Asset = asset;
            StorageOptions = options;
        }

        public readonly MediaAsset Asset;

        public readonly StorageOptions StorageOptions;

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
        /// <param name="order"></param>
        /// <param name="updateProgress"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        Task Run(TOrder order, Action<decimal> updateProgress, CancellationToken cancel);
    
    }

    public interface IUploadStep<TOrder, TWorker> : IUploadStep
        where TOrder : IUploadStep<TOrder, TWorker>
        where TWorker : IUploadStepWorker<TOrder, TWorker> { }

}
