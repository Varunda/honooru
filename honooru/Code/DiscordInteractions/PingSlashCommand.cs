using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using honooru.Code.ExtensionMethods;

namespace honooru.Code.DiscordInteractions {

    public class PingSlashCommand : PermissionSlashCommand {

        public ILogger<PingSlashCommand> _Logger { set; private get; } = default!;

        [SlashCommand("ping", "Ping the Discord bot")]
        public async Task PingCommand(InteractionContext ctx) {
            await ctx.CreateImmediateText($"pong", true);
            _Logger.LogDebug($"ping!");
        }

    }
}
