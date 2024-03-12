using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using honooru.Code.Hubs.Implementations;
using honooru.Services;
using honooru.Services.Hosted;
using honooru.Services.Db;
using honooru.Services.Db.Implementations;
using honooru.Services.Repositories;
using honooru.Services.Db.Readers;
using System.Text.Json;
using honooru.Models;
using honooru.Services.Hosted.Startup;
using honooru.Code.Converters;
using Microsoft.OpenApi.Models;
using System;
using System.IO;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using honooru.Services.Queues;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using System.Net;
using honooru.Code.DiscordInteractions;
using honooru.Code;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using honooru.Models.Config;
using honooru.Services.Parsing;
using honooru.Services.Util;
using honooru.Services.UploadStepHandler;

namespace honooru {

    public class Startup {

        public Startup(IConfiguration configuration) {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services) {
            string stuff = ((IConfigurationRoot)Configuration).GetDebugView();
            Console.WriteLine(stuff);

            services.AddLogging(builder => {
                builder.AddFile("logs/app-{0:yyyy}-{0:MM}-{0:dd}.log", options => {
                    options.FormatLogFileName = fName => {
                        return string.Format(fName, DateTime.UtcNow);
                    };
                });
            });

            services.AddRouting();

            services.AddSignalR(options => {
                options.EnableDetailedErrors = true;
            }).AddJsonProtocol(options => {
                options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            });

            services.AddMvc(options => {

            }).AddJsonOptions(config => {
                config.JsonSerializerOptions.Converters.Add(new DateTimeJsonConverter());
            }).AddRazorRuntimeCompilation();

            services.AddSwaggerGen(doc => {
                doc.SwaggerDoc("api", new OpenApiInfo() { Title = "API", Version = "v0.1" });

                Console.Write("Including XML documentation in: ");
                foreach (string file in Directory.GetFiles(AppContext.BaseDirectory, "*.xml")) {
                    Console.Write($"{Path.GetFileName(file)} ");
                    doc.IncludeXmlComments(file);
                }
                Console.WriteLine("");
            });

            string gIDKey = "Authentication:Google:ClientId";
            string gSecretKey = "Authentication:Google:ClientSecret";

            string? googleClientID = Configuration[gIDKey];
            string? googleSecret = Configuration[gSecretKey];

            if (string.IsNullOrEmpty(googleClientID) == false && string.IsNullOrEmpty(googleSecret) == false) {
                services.AddAuthentication(options => {
                    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
                }).AddCookie(options => {
                    //options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                    //options.Cookie.SameSite = SameSiteMode.Lax;
                }).AddGoogle(options => {
                    options.ClientId = googleClientID;
                    options.ClientSecret = googleSecret;
                    options.CorrelationCookie.SameSite = SameSiteMode.Lax;
                });

                Console.WriteLine($"Added Google auth");
            } else {
                Console.WriteLine($"===============================================================");
                Console.WriteLine($"!!! GOOGLE AUTH NOT SETUP");
                Console.WriteLine($"!!! missing either '{gIDKey}' ({string.IsNullOrEmpty(googleClientID)}) or '{gSecretKey}' ({string.IsNullOrEmpty(googleSecret)})");
                Console.WriteLine($"!!! GOOGLE AUTH NOT SETUP");
                Console.WriteLine($"===============================================================");
            }

            services.AddRazorPages();
            services.AddMemoryCache();
            services.AddHttpContextAccessor();

            services.AddCors(o => o.AddDefaultPolicy(builder => {
                builder.AllowAnyOrigin();
                builder.AllowAnyHeader();
            }));

            services.Configure<DiscordOptions>(Configuration.GetSection("Discord"));
            services.Configure<InstanceOptions>(Configuration.GetSection("Instance"));
            services.Configure<HttpConfig>(Configuration.GetSection("Http"));
            services.Configure<StorageOptions>(Configuration.GetSection("Storage"));

            services.Configure<KestrelServerOptions>(options => {
                options.Limits.MaxRequestBodySize = int.MaxValue;
            });

            services.AddSingleton<ServiceHealthMonitor>();

            services.AddTransient<HttpUtilService>();
            services.AddSingleton<InstanceInfo>();

            services.AddTransient<IActionResultExecutor<ApiResponse>, ApiResponseExecutor>();
            services.AddSingleton<IDbHelper, DbHelper>();
            services.AddSingleton<IDbCreator, DefaultDbCreator>();

            services.AddSingleton<CommandBus, CommandBus>();

            services.AddUtilServices();
            services.AddAppStartupServices();
            services.AddAppQueueServices(); // queue services
            services.AddDatabasesServices(); // Db services
            services.AddAppDatabaseReadersServices(); // DB readers
            services.AddAppRepositoryServices(); // Repositories
            services.AddSearchServices();
            services.AddAppQueueProcessorServices();
            services.AddUploadStepHandlers();

            // Hosted services
            services.AddHostedService<DbCreatorStartupService>(); // Have first to ensure DBs exist

            // hosted background services
            services.AddHostedService<ExampleBackgroundService>();
            services.AddHostedService<ExampleStartupService>();

            if (Configuration.GetValue<bool>("Discord:Enabled") == true) {
                services.AddSingleton<DiscordWrapper>();
                services.AddHostedService<DiscordService>();
                services.AddAppDiscord();
            }

            services.AddTransient<AppCurrentAccount>();

            services.Configure<ForwardedHeadersOptions>(options => {
                // look for the x-forwarded-for headers to know the remote IP
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

                // needed for running on production, which is behind Nginx, will accept the Cookie for Google OAuth2 
                options.KnownProxies.Add(IPAddress.Parse("64.227.19.86"));
            });

            Console.WriteLine($"!!!!! ConfigureServices finished !!!!!");
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env,
            IHostApplicationLifetime lifetime, ILogger<Startup> logger) {

            app.UseForwardedHeaders();
            app.UseMiddleware<ExceptionHandlerMiddleware>();

            logger.LogInformation($"Environment: {env.EnvironmentName}");

            app.UseStaticFiles();
            app.UseRouting();

            app.UseSwagger(doc => { });
            app.UseSwaggerUI(doc => {
                doc.SwaggerEndpoint("/swagger/api/swagger.json", "api");
                doc.RoutePrefix = "api-doc";
                doc.DocumentTitle = "PS2-Honooru API documentation";
            });

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseCors();
            app.UseEndpoints(endpoints => {
                endpoints.MapControllerRoute(
                    name: "index",
                    pattern: "/{action}",
                    defaults: new { controller = "Home", action = "Index" }
                );

                endpoints.MapControllerRoute(
                    name: "accountmanagement",
                    pattern: "/accountmanagement/{*.}",
                    defaults: new { controller = "Home", action = "AccountManagement" }
                );

                endpoints.MapControllerRoute(
                    name: "posts",
                    pattern: "/posts/{*.}",
                    defaults: new { controller = "Home", action = "Posts" }
                );

                endpoints.MapControllerRoute(
                    name: "post",
                    pattern: "/post/{*.}",
                    defaults: new { controller = "Home", action = "Post" }
                );

                endpoints.MapControllerRoute(
                    name: "api",
                    pattern: "/api/{controller}/{action}"
                );

                endpoints.MapControllerRoute(
                    name: "media",
                    pattern: "/media/{*.}",
                    defaults: new { controller = "Media", action = "Get" }
                );

                endpoints.MapHub<MediaAssetUploadHub>("/ws/upload-progress");

                endpoints.MapSwagger();
            });
        }

    }
}
