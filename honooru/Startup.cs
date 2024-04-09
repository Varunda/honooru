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
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using AspNet.Security.OAuth.Discord;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using honooru.Services.UrlMediaExtrator;
using System.Text.Json.Serialization;

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
            services.AddRequestTimeouts();

            services.AddSignalR(options => {
                options.EnableDetailedErrors = true;
            }).AddJsonProtocol(options => {
                options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            });

            services.AddMvc(options => {

            }).AddJsonOptions(config => {
                config.JsonSerializerOptions.Converters.Add(new DateTimeJsonConverter());
                config.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
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

            services.Configure<DiscordOptions>(Configuration.GetSection("Discord"));
            services.Configure<InstanceOptions>(Configuration.GetSection("Instance"));
            services.Configure<HttpConfig>(Configuration.GetSection("Http"));
            services.Configure<StorageOptions>(Configuration.GetSection("Storage"));
            services.Configure<IqdbOptions>(Configuration.GetSection("Iqdb"));

            // require all endpoints to be authorized unless another policy is defined
            services.AddAuthorization(options => {
                options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
            });

            services.AddAuthentication(options => {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = DiscordAuthenticationDefaults.AuthenticationScheme;
            }).AddCookie(options => {
                options.Cookie.Name = "honooru-auth";
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.Cookie.SameSite = SameSiteMode.Lax;
            }).AddDiscord(options => {
                DiscordOptions? dOpts = Configuration.GetSection("Discord").Get<DiscordOptions>();
                if (dOpts == null) {
                    throw new InvalidOperationException($"no discord configuration in the Discord: section configured");
                }

                if (string.IsNullOrWhiteSpace(dOpts.ClientId)) {
                    throw new InvalidOperationException($"missing ClientId. did you set Discord:ClientId?");
                }
                if (string.IsNullOrWhiteSpace(dOpts.ClientSecret)) {
                    throw new InvalidOperationException($"missing ClientSecret. did you set Discord:ClientSecret?");
                }

                options.ClientId = dOpts.ClientId;
                options.ClientSecret = dOpts.ClientSecret;

                options.CallbackPath = "/auth/callback"; // configured callback

                options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.SaveTokens = true;

                // map the returned JSON from Discord to auth claims
                options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
                options.ClaimActions.MapJsonKey(ClaimTypes.Name, "username");
                options.ClaimActions.MapCustomJson("urn:discord:avatar:url",
                    user => string.Format(CultureInfo.InvariantCulture, "https://cdn.discordapp.com/avatars/{0}/{1}.{2}",
                    user.GetString("id"),
                    user.GetString("avatar"),
                    user.GetString("avatar")!.StartsWith("a_") ? "gif" : "png")
                );

                options.Scope.Add("identify");
            });

            services.AddRazorPages();
            services.AddMemoryCache();
            services.AddHttpContextAccessor();

            services.AddCors(o => {
                o.AddDefaultPolicy(builder => {
                    builder.AllowAnyOrigin();
                    builder.AllowAnyHeader();
                });
            });

            services.Configure<KestrelServerOptions>(options => {
                options.Limits.MaxRequestBodySize = long.MaxValue;
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
            services.AddAppHostedServices();
            services.AddUrlExtractors();

            // Hosted services
            services.AddHostedService<DbCreatorStartupService>(); // Have first to ensure DBs exist

            if (Configuration.GetValue<bool>("Discord:Enabled") == true) {
                services.AddSingleton<DiscordWrapper>();
                services.AddHostedService<DiscordService>();
                services.AddAppDiscord();
            }

            services.AddSingleton<IqdbClient>();

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
            app.UseRequestTimeouts();

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
                    name: "unauth",
                    pattern: "/unauthorized/{*.}",
                    defaults: new { controller = "Unauthorized", action = "Index"}
                );

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
                    name: "tag",
                    pattern: "/tag/{*.}",
                    defaults: new { controller = "Home", action = "Tag" }
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
