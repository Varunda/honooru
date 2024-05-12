using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using FFMpegCore;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

using honooru.Code;
using honooru.Code.Tracking;
using honooru.Models;
using honooru.Services;
using System.IO;

namespace honooru {

    public class Program {

        // Is set in main
#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        private static IHost _Host;
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.

        public static async Task Main(string[] args) {
            Console.WriteLine($"starting at {DateTime.UtcNow:u} in {Directory.GetCurrentDirectory()} as {Environment.UserName}");

            GlobalFFOptions.Configure(new FFOptions() {
                TemporaryFilesFolder = "./ffmpeg/temp",
                //LogLevel = FFMpegCore.Enums.FFMpegLogLevel.Debug
            });

            bool hostBuilt = false;
            bool hostRunning = false;

            CancellationTokenSource stopSource = new();

            /*
            using TracerProvider? trace = Sdk.CreateTracerProviderBuilder()
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("npgsql"))
                .AddAspNetCoreInstrumentation(options => {
                    // only profile api calls
                    options.Filter = (c) => {
                        return c.Request.Path.StartsWithSegments("/api");
                    };
                })
                //.AddNpgsql()
                .AddOtlpExporter(config => {

                })
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(AppActivitySource.ActivitySourceName))
                .AddSource(AppActivitySource.ActivitySourceName)
                .Build();
            */

            // the app must be started in a background thread, as _Host.RunAsync will block until the whole server
            //      shuts down. If we were to await this Task, then it would be blocked until the server is done
            //      running, at which point then the command bus stuff would start
            //
            // That's not useful, because we want to be able to input commands while the server is running,
            //      not after the server is done running
            _ = Task.Run(async () => {
                ILogger<Program>? logger = null;
                try {
                    Stopwatch timer = Stopwatch.StartNew();

                    _Host = CreateHostBuilder(args).Build();
                    logger = _Host.Services.GetService(typeof(ILogger<Program>)) as ILogger<Program>;
                    hostBuilt = true;
                    Console.WriteLine($"took {timer.ElapsedMilliseconds}ms to build program");
                    timer.Stop();
                } catch (Exception ex) {
                    if (logger != null) {
                        logger.LogError(ex, "fatal error starting program");
                    } else {
                        Console.WriteLine($"fatal error starting program:\n{ex}");
                    }
                }

                try {
                    hostRunning = true;
                    await _Host.RunAsync(stopSource.Token);
                } catch (Exception ex) {
                    hostRunning = true;
                    if (logger != null) {
                        logger.LogError(ex, $"error while running program");
                    } else {
                        Console.WriteLine($"error while running program:\n{ex}");
                    }
                }
            });

            for (int i = 0; i < 10; ++i) {
                await Task.Delay(1000);
                if (hostBuilt == true) {
                    Console.WriteLine($"host has been built by Task, starting...");
                    break;
                }
            }

            if (_Host == null) {
                Console.Error.WriteLine($"FATAL> _Host was null after construction");
                return;
            }

            if (hostRunning == false) {
                return;
            }
            ILogger<Program> logger = _Host.Services.GetRequiredService<ILogger<Program>>();

            CommandBus? commands = _Host.Services.GetService(typeof(CommandBus)) as CommandBus;
            if (commands == null) {
                logger.LogError($"missing CommandBus");
                Console.Error.WriteLine($"Missing ICommandBus");
            }

            // print both incase the logger is misconfigured or something
            logger.LogInformation($"ran host");
            Console.WriteLine($"Ran host");

            string? line = "";
            bool fastStop = false;
            while (line != ".close") {
                line = Console.ReadLine();

                if (line == ".close" || line == ".closefast") {
                    if (line == ".closefast") {
                        fastStop = true;
                    }
                    break;
                } else {
                    if (commands == null) {
                        logger.LogError($"Missing {nameof(CommandBus)} from host, cannot execute '{line}'");
                        Console.Error.WriteLine($"Missing {nameof(CommandBus)} from host, cannot execute '{line}'");
                    }
                    if (line != null && commands != null) {
                        await commands.Execute(line);
                    }
                }
            }

            if (fastStop == true) {
                logger.LogInformation($"stopping from 1'000ms");
                Console.WriteLine($"stopping after 1'000ms");

                CancellationTokenSource cts = new();
                cts.CancelAfter(1000 * 1);
                await _Host.StopAsync(cts.Token);
                //stopSource.CancelAfter(1 * 1000);
            } else {
                Console.WriteLine($"stopping without a token");
                //stopSource.Cancel();
                await _Host.StopAsync();
            }

        }

        public static IHostBuilder CreateHostBuilder(string[] args) {
            IHostBuilder? host = Host.CreateDefaultBuilder(args)
                .ConfigureLogging(logging => {
                    // i don't like any of the provided default loggers
                    logging.AddConsole(options => options.FormatterName = "AppLogger")
                        .AddConsoleFormatter<AppLogger, AppFormatterOptions>(options => {

                        });
                })
                .ConfigureAppConfiguration(appConfig => {
                    appConfig.AddUserSecrets<Startup>();
                    appConfig.AddJsonFile("secrets.json");
                }).ConfigureWebHostDefaults(webBuilder => {
                    webBuilder.UseStartup<Startup>();
                });

            return host;
        }
    }
}
