using honooru_common.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using snail.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace snail {

    public class App : IHostedService {

        private readonly ILogger<App> _Logger;

        private readonly JobApi _JobApi;
        private readonly YoutubeExtractor _Youtube;

        public App(ILogger<App> logger,
            JobApi jobApi, YoutubeExtractor youtube) {

            _Logger = logger;

            _JobApi = jobApi;
            _Youtube = youtube;
        }

        public async Task StartAsync(CancellationToken cancel) {
            _Logger.LogInformation($"app starting");

            if (Directory.Exists("./downloads/") == false) {
                try {
                    Directory.CreateDirectory("./downloads/");
                } catch (Exception ex) {
                    _Logger.LogError(ex, "failed to create downloads folder");
                    return;
                }
            }

            List<DistributedJob> currentJobs = await _JobApi.GetClaimed();
            foreach (DistributedJob job in currentJobs) {
                _Logger.LogInformation($"uploading pending job [job.ID={job.ID}]");
                await _JobApi.Upload(job);
            }

            while (cancel.IsCancellationRequested == false) {
                try {
                    await Loop(cancel);

                } catch (Exception ex) {
                    _Logger.LogError(ex, "failed to execute loop of snail");
                }

                // delay a random amount of time to ensure not all snails are attempting to get at the same time
                try {
                    await Task.Delay(TimeSpan.FromSeconds(15) + TimeSpan.FromSeconds(Random.Shared.NextInt64(1, 15)), cancel);
                } catch (TaskCanceledException) {
                    _Logger.LogInformation($"quitting");
                    return;
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) {
            _Logger.LogInformation($"app stopping");
            return Task.CompletedTask;
        }

        private async Task Loop(CancellationToken cancel) {
            List<DistributedJob> jobs = await _JobApi.GetPendingJobs();
            _Logger.LogDebug($"got unclaimed jobs [jobs.Count={jobs.Count}]");

            if (jobs.Count == 0) {
                return;
            }

            DistributedJob first = jobs[0];
            _Logger.LogInformation($"claiming job [ID={first.ID}]");

            await _JobApi.ClaimJob(first.ID);

            Directory.CreateDirectory($"./downloads/{first.ID}/");

            if (first.Type == DistributedJobType.YOUTUBE) {
                await GetYoutube(first);
            } else {
                _Logger.LogError($"unchecked type of job [type={first.Type}] [ID={first.ID}]");
            }
        }

        private async Task GetYoutube(DistributedJob job) {
            if (job.Type != DistributedJobType.YOUTUBE) {
                throw new Exception($"job is not a youtube job");
            }

            string? url = job.Values.GetValueOrDefault("url");
            if (url == null) {
                throw new Exception($"job is missing url");
            }

            await _Youtube.Download(job);
            await _JobApi.Upload(job);
        }


    }
}
