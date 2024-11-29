using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using honooru.Commands;
using honooru.Models;
using honooru.Services.Db;
using honooru.Models.App;
using honooru.Services.Repositories;

namespace honooru.Code.Commands {

    [Command]
    public class AccountCommand {

        private readonly ILogger<AccountCommand> _Logger;
        private readonly AppAccountDbStore _AccountDb;
        private readonly ApiKeyRepository _ApiKeyRepository;

        public AccountCommand(IServiceProvider services) {
            _Logger = services.GetRequiredService<ILogger<AccountCommand>>();
            _AccountDb = services.GetRequiredService<AppAccountDbStore>();
            _ApiKeyRepository = services.GetRequiredService<ApiKeyRepository>();
        }

        public async Task Create(string name, string discordID) {
            AppAccount account = new AppAccount() {
                Name = name,
                DiscordID = discordID
            };

            long ID = await _AccountDb.Insert(account, CancellationToken.None);
            _Logger.LogInformation($"Created new account {ID} for {name}");
        }

        public async Task ApiKey(ulong userID) {
            AppAccount? acc = await _AccountDb.GetByID(userID);
            if (acc == null) {
                _Logger.LogInformation($"no account with ID of {userID} exists");
                return;
            }

            if ((await _ApiKeyRepository.GetByUserID(userID)) != null) {
                _Logger.LogInformation($"deleting existing API key for {acc.ID}/{acc.Name}");
                await _ApiKeyRepository.Delete(userID);
            }

            _Logger.LogInformation($"generating API key for {acc.ID}/{acc.Name}");
            await _ApiKeyRepository.Create(userID);
            ApiKey? key = await _ApiKeyRepository.GetByUserID(userID);
            if (key == null) {
                _Logger.LogError($"failed to generate api key for user {userID}");
            } else {
                _Logger.LogInformation($"api key generated, see DB for what it is (not writing it to logs lol)");
            }
        }

    }
}
