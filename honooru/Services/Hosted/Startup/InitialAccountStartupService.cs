using honooru.Models;
using honooru.Services.Db;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace honooru.Services.Hosted.Startup {

    /// <summary>
    ///     create an account named system with an ID of 
    /// </summary>
    public class InitialAccountStartupService : IHostedService {

        private readonly ILogger<InitialAccountStartupService> _Logger;
        private readonly AppAccountDbStore _AccountDb;

        public InitialAccountStartupService(ILogger<InitialAccountStartupService> logger,
            AppAccountDbStore accountDb) {

            _Logger = logger;
            _AccountDb = accountDb;
        }

        public async Task StartAsync(CancellationToken cancellationToken) {
            AppAccount? account = await _AccountDb.GetByID(1, cancellationToken);
            if (account != null) {
                _Logger.LogInformation($"initial system account exists [account.Name={account.Name}]");
                return;
            }

            account = new AppAccount();
            account.Name = "System";
            account.DiscordID = "0";

            _Logger.LogDebug($"inserting new system account into DB");
            await _AccountDb.Insert(account, cancellationToken);
            _Logger.LogInformation($"created initial system account");
        }

        public Task StopAsync(CancellationToken cancellationToken) {
            return Task.CompletedTask;
        }

    }
}
