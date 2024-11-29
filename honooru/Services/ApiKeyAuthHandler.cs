using honooru.Models;
using honooru.Models.App;
using honooru.Models.Config;
using honooru.Services.Repositories;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System;
using System.Buffers.Text;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace honooru.Services {

    public class ApiKeyAuthHandler : AuthenticationHandler<ApiKeyAuthOptions> {

        private readonly ILogger<ApiKeyAuthHandler> _Logger;
        private readonly ApiKeyRepository _ApiKeyRepository;
        private readonly AppAccountRepository _AccountRepository;

        public ApiKeyAuthHandler(IOptionsMonitor<ApiKeyAuthOptions> options, ILoggerFactory logger,
            UrlEncoder encoder, ApiKeyRepository apiKeyRepository, 
            AppAccountRepository accountRepository)
        : base(options, logger, encoder) {

            _Logger = logger.CreateLogger<ApiKeyAuthHandler>();

            _ApiKeyRepository = apiKeyRepository;
            _AccountRepository = accountRepository;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync() {
            StringValues auth = Context.Request.Headers.Authorization;
            if (auth.Count == 0) {
                return AuthenticateResult.NoResult();
            }

            foreach (string? iter in auth) {
                if (iter == null) {
                    continue;
                }

                //_Logger.LogTrace($"iteration value [iter={iter}]");

                string raw;
                try {
                    raw = Encoding.UTF8.GetString(System.Convert.FromBase64String(iter));
                } catch (Exception ex) {
                    if (ex is FormatException) {
                        _Logger.LogWarning($"bad base64 string [iter={iter}]");
                    } else {
                        _Logger.LogError(ex, "failed to parse base64 string into user id and client secret pair");
                    }

                    return AuthenticateResult.NoResult();
                }
                string[] parts = raw.Split(":");
                if (parts.Length != 2) {
                    _Logger.LogTrace($"failed to split auth header into 2 parts [iter={iter}] [raw={raw}]");
                    continue;
                }

                string userIDstr = parts[0];
                string clientSecret = parts[1];

                if (ulong.TryParse(userIDstr, out ulong userID) == false) {
                    _Logger.LogWarning($"failed to convert auth header part 0 into a ulong [iter={iter}] [raw={raw}] [parts[0]={parts[0]}]");
                    continue;
                }

                ApiKey? key = await _ApiKeyRepository.GetByUserID(userID);
                if (key == null) {
                    _Logger.LogTrace($"user has no api key [userID={userID}]");
                    continue;
                }

                if (clientSecret == key.ClientSecret) {
                    AppAccount? acc = await _AccountRepository.GetByID(userID);
                    if (acc == null) {
                        _Logger.LogWarning($"no account for api key exists [userID={userID}]");
                        continue;
                    }

                    Claim[] claims = [
                        new Claim(ClaimTypes.Name, acc.Name),
                        new Claim(ClaimTypes.NameIdentifier, acc.DiscordID) // this is the claim that really matters
                    ];

                    ClaimsIdentity ident = new(claims, "honooru-api-key");
                    ClaimsPrincipal princ = new(ident);
                    AuthenticationTicket ticket = new(princ, "honooru-api-key");
                    return AuthenticateResult.Success(ticket);
                } else {
                    return AuthenticateResult.Fail("invalid client secret");
                }
            }

            return AuthenticateResult.NoResult();
        }

    }
}
