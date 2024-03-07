using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using honooru.Models;
using Microsoft.AspNetCore.Mvc;
using honooru.Services;

namespace honooru.Services {

    /// <summary>
    ///     example background service that performs a task periodically
    /// </summary>
    public class ExampleBackgroundService : BackgroundService {

        private const int _RunDelay = 5; // seconds

        private const string SERVICE_NAME = "data_builder";

        private readonly ILogger<ExampleBackgroundService> _Logger;
        private readonly ServiceHealthMonitor _ServiceHealthMonitor;

        public ExampleBackgroundService(ILogger<ExampleBackgroundService> logger,
            ServiceHealthMonitor healthMon) {

            _Logger = logger;
            _ServiceHealthMonitor = healthMon ?? throw new ArgumentNullException(nameof(healthMon));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            _Logger.LogInformation($"Started {SERVICE_NAME}");

            while (!stoppingToken.IsCancellationRequested) {
                try {
                    Stopwatch timer = Stopwatch.StartNew();

                    ServiceHealthEntry? entry = _ServiceHealthMonitor.Get(SERVICE_NAME);
                    if (entry == null) {
                        entry = new ServiceHealthEntry() {
                            Name = SERVICE_NAME
                        };
                    }

                    // Useful for debugging on my laptop which can't handle running the queries and run vscode at the same time
                    if (entry.Enabled == false) {
                        await Task.Delay(1000, stoppingToken);
                        continue;
                    }

                    string msg = "example background service";

                    long elapsedTime = timer.ElapsedMilliseconds;

                    entry.RunDuration = elapsedTime;
                    entry.LastRan = DateTime.UtcNow;
                    entry.Message = msg;
                    _ServiceHealthMonitor.Set(SERVICE_NAME, entry);

                    long timeToHold = (_RunDelay * 1000) - elapsedTime;

                    // Don't constantly run the data building, not useful, but if it does take awhile start building again so the data is recent
                    if (timeToHold > 5) {
                        await Task.Delay((int)timeToHold, stoppingToken);
                    }
                } catch (Exception) when (stoppingToken.IsCancellationRequested == true) {
                    _Logger.LogInformation($"Stopped data builder service");
                } catch (Exception ex) {
                    _Logger.LogError(ex, "Exception in DataBuilderService");
                }
            }
        }

    }
}
