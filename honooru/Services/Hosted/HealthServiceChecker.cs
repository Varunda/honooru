using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace honooru.Services.Hosted {

    public class HealthServiceChecker : BackgroundService {

        private readonly ILogger<HealthServiceChecker> _Logger;

        private readonly ServiceHealthMonitor _ServiceHealthMonitor;
        private readonly IqdbClient _IqdbClient;

        public HealthServiceChecker(ILogger<HealthServiceChecker> logger,
            ServiceHealthMonitor serviceHealthMonitor, IqdbClient iqdbClient) {

            _Logger = logger;

            _ServiceHealthMonitor = serviceHealthMonitor;
            _IqdbClient = iqdbClient;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            _Logger.LogInformation($"starting health service checker");

            while (stoppingToken.IsCancellationRequested == false) {
                _Logger.LogInformation($"starting health service check");
                try {
                    _Logger.LogDebug($"checking health of IQDB");
                    await _IqdbClient.UpdateHealth();

                } catch (Exception ex) {
                    _Logger.LogError(ex, $"failed to update health services");
                }

                await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);
            }

            _Logger.LogInformation($"stopping health service checker");
        }

    }
}
