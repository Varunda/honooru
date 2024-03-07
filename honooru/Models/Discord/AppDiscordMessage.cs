﻿using DSharpPlus.Entities;
using System.Collections.Generic;

namespace honooru.Models.Discord {

    /// <summary>
    ///     Wrapper around whatever Discord library being used
    /// </summary>
    public class AppDiscordMessage {

        public enum TargetType {
            INVALID,

            CHANNEL,

            USER
        }

        /// <summary>
        ///     nullary ctor
        /// </summary>
        public AppDiscordMessage() {

        }

        /// <summary>
        ///     copy ctor
        /// </summary>
        /// <param name="other"></param>
        public AppDiscordMessage(AppDiscordMessage other) {
            GuildID = other.GuildID;
            ChannelID = other.ChannelID;
            TargetUserID = other.TargetUserID;
            Message = other.Message;
            Embeds = new List<DSharpPlus.Entities.DiscordEmbed>(other.Embeds);
            Mentions = new List<IMention>(other.Mentions);
            Components = new List<DiscordComponent>(other.Components);
        }

        public ulong? GuildID { get; set; }

        public ulong? ChannelID { get; set; }

        public ulong? TargetUserID { get; set; }

        /// <summary>
        ///     Message to be sent. If you want to send an embedded message instead, populate <see cref="Embeds"/>
        /// </summary>
        public string Message { get; set; } = "";

        /// <summary>
        ///     Any embeds to use in the message. Leave empty to instead send <see cref="Message"/> as plain text
        /// </summary>
        public List<DSharpPlus.Entities.DiscordEmbed> Embeds { get; set; } = new();

        /// <summary>
        ///     Get the mentions this message contains
        /// </summary>
        public List<IMention> Mentions { get; set; } = new();

        public List<DiscordComponent> Components { get; set; } = new();

        /// <summary>
        ///     Get the <see cref="TargetType"/> of this message
        /// </summary>
        public TargetType Type { 
            get {
                if (TargetUserID != null) {
                    return TargetType.USER;
                }
                if (GuildID != null && ChannelID != null) {
                    return TargetType.CHANNEL;
                }

                return TargetType.INVALID;
            }
        }

    }

}
