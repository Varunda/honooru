using honooru.Code.Hubs;
using honooru.Code.Hubs.Implementations;
using honooru.Models.Api;
using honooru.Models.App;
using honooru.Models.App.MediaUploadStep;
using honooru.Models.Db;
using honooru.Services.Repositories;
using honooru.Services.Util;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace honooru.Services.UploadStepHandler {

    public class UploadStepsProcessor {

        private readonly ILogger<UploadStepsProcessor> _Logger;
        private readonly IServiceProvider _Services;

        private readonly IHubContext<MediaAssetUploadHub, IMediaAssetUploadHub> _UploadHub;

        private readonly MediaAssetRepository _MediaAssetRepository;
        private readonly UploadStepProgressRepository _UploadProgressRepository;
        private readonly PostRepository _PostRepository;
        private readonly FileExtensionService _FileExtensionUtil;

        //private Dictionary<Guid, UploadStepEntry> _Progress = new();

        public UploadStepsProcessor(ILogger<UploadStepsProcessor> logger,
            IServiceProvider services, MediaAssetRepository mediaAssetRepository,
            IHubContext<MediaAssetUploadHub, IMediaAssetUploadHub> uploadHub,
            UploadStepProgressRepository uploadProgressRepository, PostRepository postRepository,
            FileExtensionService fileExtensionUtil) {

            _Logger = logger;

            _Services = services;
            _MediaAssetRepository = mediaAssetRepository;
            _UploadHub = uploadHub;
            _UploadProgressRepository = uploadProgressRepository;
            _PostRepository = postRepository;
            _FileExtensionUtil = fileExtensionUtil;
        }

        public async Task Run(UploadSteps steps, CancellationToken cancel) {
            int stepCount = steps.Steps.Count;

            _Logger.LogInformation($"processing upload steps [Guid={steps.Asset.Guid}] [steps.Count={steps.Steps.Count}]");

            UploadStepEntry entry = new();
            entry.MediaAssetID = steps.Asset.Guid;
            _UploadProgressRepository.Add(entry);

            steps.Asset.Status = MediaAssetStatus.PROCESSING;
            await _MediaAssetRepository.Upsert(steps.Asset);

            // add each step 
            IMediaAssetUploadHub group = _UploadHub.Clients.Group($"MediaAsset.Upload.{steps.Asset.Guid}");
            int order = 0;
            foreach (IUploadStep step in steps.Steps) {
                entry.Progress.Add(step.Name, new UploadStepProgress() {
                    Name = step.Name,
                    Order = order++
                });
            }
            await group.UpdateProgress(entry);

            // perform each step
            try {
                for (int i = 0; i < steps.Steps.Count; ++i) {
                    cancel.ThrowIfCancellationRequested();

                    IUploadStep step = steps.Steps[i];
                    entry.Current = entry.Progress.GetValueOrDefault(step.Name);
                    if (entry.Current != null) {
                        entry.Current.StartedAt = DateTime.UtcNow;
                    }

                    _Logger.LogInformation($"performing step {i + 1}/{stepCount} [step.Name={step.Name}]");

                    using IServiceScope scope = _Services.CreateScope();

                    Type workerType = step.GetType()
                        .GetInterfaces().First(i => i.IsConstructedGenericType && i.GetGenericTypeDefinition() == typeof(IUploadStep<,>))
                        .GetGenericArguments().Last();

                    _Logger.LogDebug($"worker type {workerType.FullName}");

                    object worker = scope.ServiceProvider.GetRequiredService(workerType);

                    MethodInfo? workMethod = workerType.GetMethod("Run");
                    if (workMethod == null) {
                        throw new MissingMethodException(workerType.FullName, "Run");
                    }

                    if (workMethod.ReturnType != typeof(Task<bool>)) {
                        throw new ArgumentException($"return type of Run must be a Task<bool>, got {workMethod.ReturnType.FullName} instead");
                    }

                    Stopwatch timer = Stopwatch.StartNew();
                    // force is safe, we know the return type will be a Task<bool>, not a |Task<bool>?|
                    Task<bool> task = (Task<bool>) workMethod.Invoke(worker, [
                        step,
                        (decimal progress) => {
                            if (entry.Current != null) {
                                entry.Current.Percent = progress;
                            }
                        },
                        cancel
                    ])!;

                    bool result = await task;
                    // not needed! read for more:
                    // https://devblogs.microsoft.com/pfxteam/do-i-need-to-dispose-of-tasks/
                    //task.Dispose();

                    _Logger.LogInformation($"took {timer.ElapsedMilliseconds}ms to process {workerType.FullName}");
                    if (entry.Current != null) {
                        entry.Current.Finished = true;
                    }

                    await group.UpdateProgress(entry);

                    Post? existingPost = await _PostRepository.GetByMD5(steps.Asset.MD5);
                    _Logger.LogDebug($"checking if existing post by MD5 exists [md5={steps.Asset.MD5}]");
                    if (existingPost != null) {
                        _Logger.LogInformation($"found existing post after uploading asset [postID={existingPost.ID}] [md5={existingPost.MD5}] [MediaAssetID={steps.Asset.Guid}]");
                        steps.Asset.PostID = existingPost.ID;
                        break;
                    }

                    if (result == false) {
                        _Logger.LogInformation($"upload step said to stop processing now, stopping [name={step.Name}]");
                        break;
                    }

                    cancel.ThrowIfCancellationRequested();
                }

                MediaAsset asset = steps.Asset;

                _Logger.LogDebug($"updating asset [asset.ID={asset.Guid}]");
                asset.FileType = _FileExtensionUtil.GetFileType(asset.FileExtension) ?? "";
                asset.Status = MediaAssetStatus.DONE;
                await _MediaAssetRepository.Upsert(asset);
                await group.Finish(asset);
                _UploadProgressRepository.Remove(entry.MediaAssetID);

                _Logger.LogInformation($"completed upload steps [Guid={steps.Asset.Guid}] [FileType={asset.FileType}]");
            } catch (Exception ex) {
                entry.Error = new ExceptionInfo(ex);
                steps.Asset.Status = MediaAssetStatus.ERRORED;
                await _MediaAssetRepository.Upsert(steps.Asset);

                if (cancel.IsCancellationRequested == true) {
                    _Logger.LogWarning($"timeout occured when uploading {steps.Asset.Guid}: {ex.Message}");
                } else {
                    _Logger.LogError(ex, "failed to run upload steps");
                    throw;
                }
            }

        }

    }
}
