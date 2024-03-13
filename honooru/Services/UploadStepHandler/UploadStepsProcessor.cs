using honooru.Code.Hubs;
using honooru.Code.Hubs.Implementations;
using honooru.Models.Api;
using honooru.Models.App;
using honooru.Models.App.MediaUploadStep;
using honooru.Services.Repositories;
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

        //private Dictionary<Guid, UploadStepEntry> _Progress = new();

        public UploadStepsProcessor(ILogger<UploadStepsProcessor> logger,
            IServiceProvider services, MediaAssetRepository mediaAssetRepository,
            IHubContext<MediaAssetUploadHub, IMediaAssetUploadHub> uploadHub,
            UploadStepProgressRepository uploadProgressRepository) {

            _Logger = logger;

            _Services = services;
            _MediaAssetRepository = mediaAssetRepository;
            _UploadHub = uploadHub;
            _UploadProgressRepository = uploadProgressRepository;
        }

        public async Task Run(UploadSteps steps, CancellationToken cancel) {
            int stepCount = steps.Steps.Count;

            _Logger.LogInformation($"processing upload steps [Guid={steps.Asset.Guid}] [steps.Count={steps.Steps.Count}]");

            UploadStepEntry entry = new();
            entry.MediaAssetID = steps.Asset.Guid;
            _UploadProgressRepository.Add(entry);

            steps.Asset.Status = MediaAssetStatus.PROCESSING;
            await _MediaAssetRepository.Upsert(steps.Asset);

            IMediaAssetUploadHub group = _UploadHub.Clients.Group($"MediaAsset.Upload.{steps.Asset.Guid}");
            int order = 0;
            foreach (IUploadStep step in steps.Steps) {
                entry.Progress.Add(step.Name, new UploadStepProgress() {
                    Name = step.Name,
                    Order = order++
                });
            }
            await group.UpdateProgress(entry);

            try {
                for (int i = 0; i < steps.Steps.Count; ++i) {
                    IUploadStep step = steps.Steps[i];
                    entry.Current = entry.Progress.GetValueOrDefault(step.Name);

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

                    if (workMethod.ReturnType != typeof(Task)) {
                        throw new ArgumentException($"return type of Run must be a Task");
                    }

                    Stopwatch timer = Stopwatch.StartNew();
                    // force is safe, we know the return type will be a Task, not a Task?
                    Task task = (Task) workMethod.Invoke(worker, new object[] {
                        step,
                        (decimal progress) => {
                            if (entry.Current != null) {
                                entry.Current.Percent = progress;
                            }
                        },
                        cancel
                    })!;

                    Thread t = new(async () => {
                        try {
                            while (task.IsCompleted == false) {
                                await group.UpdateProgress(entry);
                                await Task.Delay(TimeSpan.FromSeconds(5), cancel);
                            }
                        } catch (Exception ex) {
                            _Logger.LogError(ex, $"background updates will not occur");
                        }
                    });

                    t.Start();
                    _Logger.LogDebug($"starting update thread");

                    await task;
                    _Logger.LogDebug($"task completed, joining to update thread");
                    t.Join();

                    _Logger.LogInformation($"took {timer.ElapsedMilliseconds}ms to process {workerType.FullName}");
                    if (entry.Current != null) {
                        entry.Current.Finished = true;
                    }
                }
            } catch (Exception ex) {
                _Logger.LogError(ex, "failed to run upload steps");
            }

            _Logger.LogInformation($"completed upload steps [Guid={steps.Asset.Guid}]");
            MediaAsset asset = steps.Asset;
            asset.Status = MediaAssetStatus.DONE;
            await _MediaAssetRepository.Upsert(asset);

            await group.Finish(asset);
            _UploadProgressRepository.Remove(entry.MediaAssetID);
        }

    }
}
