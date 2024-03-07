using DSharpPlus.ButtonCommands;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using honooru.Code.ExtensionMethods;
using honooru.Services;

namespace honooru.Code.DiscordInteractions {

    public class ExampleSlashCommands : PermissionSlashCommand {

        public ILogger<ExampleSlashCommands> _Logger { set; private get; } = default!;
        public ServerStatusInteractions _Interactions { set; private get; } = default!;

        /// <summary>
        ///     Get some basic information about a server
        /// </summary>
        /// <param name="ctx">provided context</param>
        /// <param name="max">max value</param>
        [SlashCommand("rng", "Get a random number")]
        public async Task ServerStatus(InteractionContext ctx,
            [Option("max", "Max")] long max) {

            await ctx.CreateDeferred(false);

            DiscordWebhookBuilder builder = new();
            builder.AddEmbed(await _Interactions.GeneralStatus(max));

            builder.AddComponents(ServerStatusButtonCommands.REFRESH_RANDOM(max));

            DiscordMessage msg = await ctx.EditResponseAsync(builder);
            _Logger.LogDebug($"message created: {msg.Id}");
        }

    }

    /// <summary>
    ///     Interactions for buttons on messages
    /// </summary>
    public class ServerStatusButtonCommands : ButtonCommandModule {

        public ILogger<ServerStatusButtonCommands> _Logger { set; private get; } = default!;
        public ServerStatusInteractions _Interactions { set; private get; } = default!;

        /// <summary>
        ///     Button to refresh the general world status in a message
        /// </summary>
        /// <param name="previousValue">value to not allow as the new random value</param>
        public static DiscordButtonComponent REFRESH_RANDOM(long previousValue) => new(DSharpPlus.ButtonStyle.Secondary, $"@refresh-random.{previousValue}", "Refresh");

        /// <summary>
        ///     Refresh a message with updated world information
        /// </summary>
        /// <param name="ctx">Provided context</param>
        /// <param name="previousValue">what the new random value cannot be</param>
        [ButtonCommand("refresh-random")]
        public async Task RefreshWorld(ButtonContext ctx, long previousValue) {
            await ctx.Interaction.CreateComponentDeferred(true);

            DiscordEmbed response = await _Interactions.GeneralStatus(previousValue);

            if (ctx.Message != null) {
                _Logger.LogDebug($"putting refreshed message into id {ctx.Message.Id}");
                await ctx.Message.ModifyAsync(Optional.FromValue(response));
            } else {
                await ctx.Interaction.EditResponseErrorEmbed($"message provided in context was null?");
            }
        }

    }

    /// <summary>
    ///     Backing interactions used by both slash commands and button commands
    /// </summary>
    public class ServerStatusInteractions {

        private readonly ILogger<ServerStatusInteractions> _Logger;
        private readonly InstanceInfo _Instance;

        public ServerStatusInteractions(ILogger<ServerStatusInteractions> logger,
            InstanceInfo instance) {

            _Logger = logger;
            _Instance = instance;
        }

        /// <summary>
        ///     example embed
        /// </summary>
        /// <param name="worldID">ID of the world</param>
        public Task<DiscordEmbed> GeneralStatus(long previousValue) {
            DiscordEmbedBuilder builder = new();
            builder.Title = $"howdy!";
            builder.Url = $"https://{_Instance.GetHost()}/";
            builder.Description = $"example description";

            builder.Timestamp = DateTimeOffset.UtcNow;

            return Task.FromResult(builder.Build());
        }

    }

}
