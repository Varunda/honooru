using honooru.Models.Config;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace honooru.Services.Hosted.Startup {

    public class StorageOptionCheckStartupService : IHostedService {

        private readonly ILogger<StorageOptionCheckStartupService> _Logger;
        private readonly IOptions<StorageOptions> _StorageOptions;

        public StorageOptionCheckStartupService(ILogger<StorageOptionCheckStartupService> logger,
            IOptions<StorageOptions> options) {

            _Logger = logger;
            _StorageOptions = options;
        }

        public Task StartAsync(CancellationToken cancellationToken) {
            _Logger.LogInformation($"write check incomplete, checking");
            string dir = _StorageOptions.Value.RootDirectory;
            if (string.IsNullOrWhiteSpace(dir)) {
                throw new ArgumentException($"option StorageOptions:RootDirectory is null or empty");
            }

            string fullPath = Path.GetFullPath(dir);
            _Logger.LogInformation($"expanding RootDirectory and checking permissions [dir={dir}] [fullPath={fullPath}]");

            if (Directory.Exists(fullPath) == false) {
                throw new ArgumentException($"RootDirectory {fullPath} does not exist");
            }

            string testFile = Path.Combine(fullPath, "test");
            if (File.Exists(testFile) == true) {
                _Logger.LogInformation($"test file existing, deleting [testFile={testFile}]");
                File.Delete(testFile);
            }

            _Logger.LogInformation($"writing test file");
            File.Create(testFile);

            _Logger.LogInformation($"test file created!");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) {
            return Task.CompletedTask;
        }

    }
}
