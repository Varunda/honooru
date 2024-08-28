using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using honooru.Models;
using honooru.Services.Queues;
using DSharpPlus.SlashCommands;
using honooru.Code.DiscordInteractions;
using DSharpPlus.SlashCommands.EventArgs;
using honooru.Code.ExtensionMethods;
using honooru.Models.Discord;
using static honooru.Models.Discord.AppDiscordMessage;
using DSharpPlus.Exceptions;
using DSharpPlus.ButtonCommands.Extensions;
using DSharpPlus.ButtonCommands;
using DSharpPlus.ButtonCommands.EventArgs;
using System.Diagnostics;
using honooru.Services.UrlMediaExtrator;
using honooru.Models.App;
using honooru.Services.Repositories;
using System.Security.Cryptography;
using System.Linq;
using honooru.Models.App.MediaUploadStep;
using honooru.Models.Config;
using honooru.Services.UploadStepHandler;
using FFMpegCore.Enums;

namespace honooru.Services.Hosted {

    public class DiscordService : BackgroundService {

        private readonly ILogger<DiscordService> _Logger;

        private readonly DiscordMessageQueue _MessageQueue;

        private readonly IOptions<DiscordOptions> _DiscordOptions;
        private readonly IOptions<StorageOptions> _StorageOptions;
        private readonly InstanceInfo _InstanceInfo;

        //public DiscordClient _Discord;
        private readonly DiscordWrapper _Discord;
        private readonly SlashCommandsExtension _SlashCommands;
        private readonly ButtonCommandsExtension _ButtonCommands;
        private readonly AppCurrentAccount _CurrentUser;
        private readonly UrlMediaExtractorHandler _UrlExtractor;
        private readonly MediaAssetRepository _MediaAssetRepository;
        private readonly UploadStepsProcessor _UploadStepsHandler;
        private readonly BaseQueue<UploadSteps> _UploadStepsQueue;

        private bool _IsConnected = false;
        private const string SERVICE_NAME = "discord";

        private Dictionary<ulong, ulong> _CachedMembership = new();

        public DiscordService(ILogger<DiscordService> logger, ILoggerFactory loggerFactory,
            DiscordMessageQueue msgQueue, IOptions<DiscordOptions> discordOptions,
            IServiceProvider services, DiscordWrapper discord,
            AppCurrentAccount currentUser, UrlMediaExtractorHandler urlExtractor,
            MediaAssetRepository mediaAssetRepository, UploadStepsProcessor uploadStepsHandler,
            IOptions<StorageOptions> storageOptions, BaseQueue<UploadSteps> uploadStepsQueue, InstanceInfo instanceInfo) {

            _Logger = logger;
            _MessageQueue = msgQueue ?? throw new ArgumentNullException(nameof(msgQueue));
            _CurrentUser = currentUser;
            _UrlExtractor = urlExtractor;
            _MediaAssetRepository = mediaAssetRepository;
            _UploadStepsHandler = uploadStepsHandler;
            _UploadStepsQueue = uploadStepsQueue;

            _StorageOptions = storageOptions;
            _DiscordOptions = discordOptions;

            _Discord = discord;

            _Discord.Get().Ready += Client_Ready;
            _Discord.Get().InteractionCreated += Generic_Interaction_Created;
            _Discord.Get().ContextMenuInteractionCreated += Generic_Interaction_Created;
            _Discord.Get().GuildAvailable += Guild_Available;
            _Discord.Get().MessageCreated += Message_Created;

            _SlashCommands = _Discord.Get().UseSlashCommands(new SlashCommandsConfiguration() {
                Services = services
            });

            _SlashCommands.SlashCommandErrored += Slash_Command_Errored;
            _SlashCommands.ContextMenuErrored += Context_Menu_Errored;

            // these commands can only be used in the "home guild", for live it's currently PSB
            //_SlashCommands.RegisterCommands<PingSlashCommand>(_DiscordOptions.Value.GuildId);
            //_SlashCommands.RegisterCommands<AppAccountSlashCommand>(_DiscordOptions.Value.GuildId);

            // these commands are global when ran in live, but to test them locally
            //      they are setup in the home server as well (quicker to update)
            if (_DiscordOptions.Value.RegisterGlobalCommands == true) {
                _SlashCommands.RegisterCommands<ExampleSlashCommands>();
            } else {
                //_SlashCommands.RegisterCommands<ExampleSlashCommands>(_DiscordOptions.Value.GuildId);
            }

            _ButtonCommands = _Discord.Get().UseButtonCommands(new ButtonCommandsConfiguration() {
                Services = services
            });
            _ButtonCommands.RegisterButtons<ServerStatusButtonCommands>();

            _ButtonCommands.ButtonCommandExecuted += Button_Command_Executed;
            _ButtonCommands.ButtonCommandErrored += Button_Command_Error;
            _InstanceInfo = instanceInfo;
        }

        public async override Task StartAsync(CancellationToken cancellationToken) {
            try {
                await _Discord.Get().ConnectAsync(activity: null, status: UserStatus.Online);
                await base.StartAsync(cancellationToken);
            } catch (Exception ex) {
                _Logger.LogError(ex, "Error in start up of DiscordService");
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            _Logger.LogInformation($"Started {SERVICE_NAME}");

            while (stoppingToken.IsCancellationRequested == false) {
                try {
                    if (_IsConnected == false) {
                        await Task.Delay(1000, stoppingToken);
                        continue;
                    }

                    AppDiscordMessage msg = await _MessageQueue.Dequeue(stoppingToken);
                    TargetType targetType = msg.Type;

                    if (targetType == TargetType.INVALID) {
                        _Logger.LogError($"Invalid TargetType [ChannelID {msg.ChannelID}] [GuildID {msg.GuildID}] [TargetUserID {msg.TargetUserID}]");
                        continue;
                    }

                    Stopwatch timer = Stopwatch.StartNew();

                    DiscordMessageBuilder builder = new();

                    // the contents is ignored if there is any embeds
                    if (msg.Embeds.Count > 0) {
                        foreach (DSharpPlus.Entities.DiscordEmbed embed in msg.Embeds) {
                            builder.AddEmbed(embed);
                        }
                    } else {
                        builder.Content = msg.Message;
                    }

                    foreach (IMention mention in msg.Mentions) {
                        builder.WithAllowedMention(mention);
                    }

                    if (msg.Components.Count > 0) {
                        builder.AddComponents(msg.Components);
                    }

                    if (targetType == TargetType.CHANNEL) {
                        DiscordGuild? guild = await _Discord.Get().TryGetGuild(msg.GuildID!.Value);
                        if (guild == null) {
                            _Logger.LogError($"Failed to get guild {msg.GuildID} (null)");
                            continue;
                        }

                        DiscordChannel? channel = guild.GetChannel(msg.ChannelID!.Value);
                        if (channel == null) {
                            _Logger.LogError($"Failed to get channel {msg.ChannelID} in guild {msg.GuildID}");
                            continue;
                        }

                        await channel.SendMessageAsync(builder);
                    } else if (targetType == TargetType.USER) {
                        DiscordMember? member = await GetDiscordMember(msg.TargetUserID!.Value);
                        if (member == null) {
                            _Logger.LogWarning($"Failed to find {msg.TargetUserID}");
                            continue;
                        }

                        await member.SendMessageAsync(builder);
                    }

                    _MessageQueue.AddProcessTime(timer.ElapsedMilliseconds);
                } catch (Exception ex) when (stoppingToken.IsCancellationRequested == false) {
                    _Logger.LogError(ex, "error sending message");
                } catch (Exception) when (stoppingToken.IsCancellationRequested == true) {
                    _Logger.LogInformation($"Stopping {SERVICE_NAME} with {_MessageQueue.Count()} left");
                }
            }
        }

        /// <summary>
        ///     Get a <see cref="DiscordMember"/> from an ID
        /// </summary>
        /// <param name="memberID">ID of the Discord member to get</param>
        /// <returns>
        ///     The <see cref="DiscordMember"/> with the corresponding ID, or <c>null</c>
        ///     if the user could not be found in any guild the bot is a part of
        /// </returns>
        private async Task<DiscordMember?> GetDiscordMember(ulong memberID) {
            // check if cached
            if (_CachedMembership.TryGetValue(memberID, out ulong guildID) == true) {
                DiscordGuild? guild = await _Discord.Get().TryGetGuild(guildID);
                if (guild == null) {
                    _Logger.LogWarning($"Failed to get guild {guildID} from cached membership for member {memberID}");
                } else {
                    DiscordMember? member = await guild.TryGetMember(memberID);
                    // if the member is null, and was cached, then cache is bad
                    if (member == null) {
                        _Logger.LogWarning($"Failed to get member {memberID} from guild {guildID}");
                        _CachedMembership.Remove(memberID);
                    } else {
                        _Logger.LogDebug($"Found member {memberID} from guild {guildID} (cached)");
                        return member;
                    }
                }
            }

            // check each guild and see if it contains the target member
            foreach (KeyValuePair<ulong, DiscordGuild> entry in _Discord.Get().Guilds) {
                DiscordMember? member = await entry.Value.TryGetMember(memberID);

                if (member != null) {
                    _Logger.LogDebug($"Found member {memberID} from guild {entry.Value.Id}");
                    _CachedMembership[memberID] = entry.Value.Id;
                    return member;
                }
            }

            _Logger.LogWarning($"Cannot get member {memberID}, not cached and not in any guilds");

            return null;
        }

        /// <summary>
        ///     Event handler for when the client is ready
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private Task Client_Ready(DiscordClient sender, ReadyEventArgs args) {
            _Logger.LogInformation($"Discord client connected [username={sender.CurrentUser.Username}] [id={sender.CurrentUser.Id}]");

            _IsConnected = true;

            return Task.CompletedTask;
        }

        private Task Guild_Available(DiscordClient sender, GuildCreateEventArgs args) {
            DiscordGuild? guild = args.Guild;
            if (guild == null) {
                _Logger.LogDebug($"no guild?");
                return Task.CompletedTask;
            }

            _Logger.LogDebug($"guild available: {guild.Id} / {guild.Name}");
            return Task.CompletedTask;
        }

        /// <summary>
        ///     whenever a message is sent in a DM that contains a link or attachments, upload those into honooru
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private async Task Message_Created(DiscordClient sender, MessageCreateEventArgs args) {
            if (args.Channel.IsPrivate == false) { // ignore non-private DMs
                return;
            }

            if (args.Author.IsBot == true) { // ignore bot messages
                return;
            }

            AppAccount? account = await _CurrentUser.GetByDiscordID(args.Author.Id);
            if (account == null) {
                _Logger.LogDebug($"message from {args.Author.Username}/{args.Author.Id} is not a user, ignoring");
                return;
            }

            _Logger.LogTrace($"discord message recieved [msg={args.Message}]");

            string msg = args.Message.Content;

            int breakout = 10;
            while (true) {
                if (--breakout == 0) {
                    _Logger.LogWarning($"something went wrong in message [msg={msg}]");
                    break;
                }

                int linkIndex = msg.IndexOf("http");
                if (linkIndex == -1) {
                    break;
                }

                int endIndex = msg[linkIndex..].IndexOf(" ");
                if (endIndex == -1) { // if the message is only a link, then there will be no space, so consume the rest of the string
                    endIndex = msg.Length;
                }

                string url = msg[linkIndex..endIndex];
                _Logger.LogDebug($"found url [url={url}]");
                msg = msg.Replace(url, "");
                _Logger.LogDebug($"url from message removed [msg={msg}]");
                await _HandleUrl(url, args.Author.Id);
            }

            foreach (DiscordAttachment attachment in args.Message.Attachments) {
                string url = attachment.Url;
                await _HandleUrl(url, args.Author.Id);
            }
        }

        private async Task _HandleUrl(string url, ulong targetUserID) {
            _Logger.LogDebug($"handling URL from Discord DM [url={url}]");

            if (_UrlExtractor.CanHandle(url) == false) {
                _MessageQueue.Queue(new AppDiscordMessage() {
                    TargetUserID = targetUserID,
                    Message = $"url `{url}` cannot be handled"
                });
            } else {
                MediaAsset asset = new();
                asset.Guid = Guid.NewGuid();
                asset.Source = url;
                asset.Timestamp = DateTime.UtcNow;

                // since no md5 currently exists due to the file not being extracted, get one based on the GUID
                byte[] md5 = MD5.HashData(asset.Guid.ToByteArray());
                string md5Str = string.Join("", md5.Select(iter => iter.ToString("x2"))); // turn that md5 into a string
                asset.MD5 = md5Str;

                await _MediaAssetRepository.Upsert(asset);

                asset.Status = MediaAssetStatus.EXTRACTING;
                await _MediaAssetRepository.Upsert(asset);

                UploadSteps steps = new(asset, _StorageOptions.Value);
                steps.AddExtractStep(url);
                steps.AddReencodeStep(VideoCodec.LibX264, AudioCodec.Aac);
                steps.AddFinalMoveStep();
                steps.AddImageHashStep();

                _UploadStepsQueue.Queue(steps);

                _MessageQueue.Queue(new AppDiscordMessage() {
                    TargetUserID = targetUserID,
                    Message = $"URL is being uploaded to Honooru\n`{url}`\n\nhttps://{_InstanceInfo.GetHost()}/upload?m={asset.Guid}"
                });
            }
        }

        /// <summary>
        ///     Event handler for both types of interaction (slash commands and context menu)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private Task Generic_Interaction_Created(DiscordClient sender, InteractionCreateEventArgs args) {
            DiscordInteraction interaction = args.Interaction;
            string user = interaction.User.GetDisplay();

            string interactionMethod = "slash";

            DiscordUser? targetMember = null;
            DiscordMessage? targetMessage = null;

            if (args is ContextMenuInteractionCreateEventArgs contextArgs) {
                targetMember = contextArgs.TargetUser;
                targetMessage = contextArgs.TargetMessage;
                interactionMethod = "context menu";
            }

            string feedback = $"{user} used '{interaction.Data.Name}' (a {interaction.Type}) as a {interactionMethod}: ";

            if (targetMember != null) {
                feedback += $"[target member: (user) {targetMember.GetDisplay()}]";
            }
            if (targetMessage != null) {
                feedback += $"[target message: (channel) {targetMessage.Id}] [author: (user) {targetMessage.Author.GetDisplay()}]";
            }

            if (targetMessage == null && targetMember == null) {
                feedback += $"{interaction.Data.Name} {GetCommandString(interaction.Data.Options)}";
            }

            _Logger.LogDebug(feedback);

            return Task.CompletedTask;
        }

        /// <summary>
        ///     Event handler for when a button command is executed
        /// </summary>
        /// <param name="ext"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private Task Button_Command_Executed(ButtonCommandsExtension ext, ButtonCommandExecutionEventArgs args) {
            _Logger.LogDebug($"{args.Context.User.GetDisplay()} used '{args.CommandName}': {args.ButtonId}");
            return Task.CompletedTask;
        }

        /// <summary>
        ///     Event handler for when an exception is thrown during the execution of a button command
        /// </summary>
        /// <param name="ext"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private async Task Button_Command_Error(ButtonCommandsExtension ext, ButtonCommandErrorEventArgs args) {
            _Logger.LogError($"Error executing button command {args.CommandName}: {args.Exception} :: {args.Exception.InnerException?.Message}");

            try {
                // if the response has already started, this won't be null, indicating to instead update the response
                DiscordMessage? msg = await args.Context.Interaction.GetOriginalResponseAsync();

                string error = $"Error executing button command `{args.CommandName}`: {args.Exception.GetType().Name} - {args.Exception.Message}";
                if (args.Exception.InnerException != null) {
                    error += $". Caused by: {args.Exception.InnerException.GetType().Name} - {args.Exception.InnerException.Message}";
                }

                if (msg == null) {
                    // if it is null, then no respons has been started, so one is created
                    // if you attempt to create a response for one that already exists, then a 400 is thrown
                    await args.Context.Interaction.CreateImmediateText(error, true);
                } else {
                    await args.Context.Interaction.EditResponseText(error);
                }
            } catch (Exception ex) {
                _Logger.LogError(ex, $"error updating interaction response with error");
            }
        }

        /// <summary>
        ///     Event handler for when a slash command fails
        /// </summary>
        /// <param name="ext"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private async Task Slash_Command_Errored(SlashCommandsExtension ext, SlashCommandErrorEventArgs args) {
            if (args.Exception is SlashExecutionChecksFailedException failedCheck) {
                string feedback = "Check failed:\n";

                foreach (SlashCheckBaseAttribute check in failedCheck.FailedChecks) {
                    feedback += $"Unchecked check type: {check.GetType()}";
                    _Logger.LogError($"Unchecked check type: {check.GetType()}");
                }

                await args.Context.CreateImmediateText(feedback, true);

                return;
            }

            _Logger.LogError(args.Exception, $"error executing slash command: {args.Context.CommandName}");

            if (args.Exception is BadRequestException badRequest) {
                _Logger.LogError($"errors in request [url={badRequest.WebRequest.Url}] [errors={badRequest.Errors}]");
            }

            try {
                // if the response has already started, this won't be null, indicating to instead update the response
                DiscordMessage? msg = null;
                try {
                    msg = await args.Context.GetOriginalResponseAsync();
                } catch (NotFoundException) {
                    msg = null;
                }

                if (msg == null) {
                    // if it is null, then no respons has been started, so one is created
                    // if you attempt to create a response for one that already exists, then a 400 is thrown
                    await args.Context.CreateImmediateText($"Error executing slash command: {args.Exception.Message}", true);
                } else {
                    await args.Context.EditResponseText($"Error executing slash command: {args.Exception.Message}");
                }
            } catch (Exception ex) {
                _Logger.LogError(ex, $"error sending error message to Discord");
            }
        }

        /// <summary>
        ///     Event handler for when a context menu command fails
        /// </summary>
        /// <param name="ext"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private async Task Context_Menu_Errored(SlashCommandsExtension ext, ContextMenuErrorEventArgs args) {
            if (args.Exception is ContextMenuExecutionChecksFailedException failedCheck) {
                string feedback = "Check failed:\n";

                foreach (ContextMenuCheckBaseAttribute check in failedCheck.FailedChecks) {
                    if (check is RequiredAppPermissionContextAttribute role) {
                        _Logger.LogWarning($"{args.Context.User.GetDisplay()} attempted to use {args.Context.CommandName},"
                            + $" but lacks the Discord roles: {string.Join(", ", role.Permissions)}");

                        feedback += $"You lack a required role: {string.Join(", ", role.Permissions)}";
                    } else {
                        feedback += $"Unchecked check type: {check.GetType()}";
                        _Logger.LogError($"Unchecked check type: {check.GetType()}");
                    }
                }

                await args.Context.CreateImmediateText(feedback);

                return;
            }

            _Logger.LogError(args.Exception, $"error executing context command: {args.Context.CommandName}");
            try {
                // if the response has already started, this won't be null, indicating to instead update the response
                DiscordMessage? msg = await args.Context.GetOriginalResponseAsync();

                if (msg == null) {
                    // if it is null, then no respons has been started, so one is created
                    // if you attempt to create a response for one that already exists, then a 400 is thrown
                    await args.Context.CreateImmediateText($"Error executing context command: {args.Exception.Message}");
                } else {
                    await args.Context.EditResponseText($"Error executing context command: {args.Exception.Message}");
                }
            } catch (Exception ex) {
                _Logger.LogError(ex, $"error sending error message to Discord");
            }
        }

        /// <summary>
        ///     Convert the app wrapper for a discord mention into whatever library we're using
        /// </summary>
        /// <param name="mention"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">If the <paramref name="mention"/>'s <see cref="DiscordMention.MentionType"/> was unhandled</exception>
        private IMention ConvertMentionable(DiscordMention mention) {
            if (mention.MentionType == DiscordMention.DiscordMentionType.NONE) {
                throw new ArgumentException($"{nameof(DiscordMention)} has {nameof(DiscordMention.MentionType)} {nameof(DiscordMention.DiscordMentionType.NONE)}");
            } else if (mention.MentionType == DiscordMention.DiscordMentionType.ROLE) {
                return new RoleMention(((RoleDiscordMention)mention).RoleID);
            }

            throw new ArgumentException($"Unchecked {nameof(DiscordMention.MentionType)}: {mention.MentionType}");
        }

        /// <summary>
        ///     Transform the options used in an interaction into a string that can be viewed
        /// </summary>
        /// <param name="options"></param>
        private string GetCommandString(IEnumerable<DiscordInteractionDataOption>? options) {
            if (options == null) {
                options = new List<DiscordInteractionDataOption>();
            }

            string s = "";

            foreach (DiscordInteractionDataOption opt in options) {
                s += $"[{opt.Name}=";

                if (opt.Type == ApplicationCommandOptionType.Attachment) {
                    s += $"(Attachment)";
                } else if (opt.Type == ApplicationCommandOptionType.Boolean) {
                    s += $"(bool) {opt.Value}";
                } else if (opt.Type == ApplicationCommandOptionType.Channel) {
                    s += $"(channel) {opt.Value}";
                } else if (opt.Type == ApplicationCommandOptionType.Integer) {
                    s += $"(int) {opt.Value}";
                } else if (opt.Type == ApplicationCommandOptionType.Mentionable) {
                    s += $"(mentionable) {opt.Value}";
                } else if (opt.Type == ApplicationCommandOptionType.Number) {
                    s += $"(number) {opt.Value}";
                } else if (opt.Type == ApplicationCommandOptionType.Role) {
                    s += $"(role) {opt.Value}";
                } else if (opt.Type == ApplicationCommandOptionType.String) {
                    s += $"(string) '{opt.Value}'";
                } else if (opt.Type == ApplicationCommandOptionType.SubCommand) {
                    s += GetCommandString(opt.Options);
                } else if (opt.Type == ApplicationCommandOptionType.SubCommandGroup) {
                    s += GetCommandString(opt.Options);
                } else if (opt.Type == ApplicationCommandOptionType.User) {
                    s += $"(user) {opt.Value}";
                } else {
                    _Logger.LogError($"Unchecked {nameof(DiscordInteractionDataOption)}.{nameof(DiscordInteractionDataOption.Type)}: {opt.Type}, value={opt.Value}");
                    s += $"[{opt.Name}=(UNKNOWN {opt.Type}) {opt.Value}]";
                }

                s += "]";
            }

            return s;
        }

    }
}
