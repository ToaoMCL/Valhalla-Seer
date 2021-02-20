using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Valhalla_Seer.Saving;
using Valhalla_Seer.Commands;

namespace Valhalla_Seer.DataStructures
{
    class ReactionPost : ISavable
    {
        public DiscordMessage DiscordMessage_ { get; private set; }
        public Application Application_ { get; private set; }


        public ReactionPost(DiscordMessage message, Application application)
        {
            DiscordMessage_ = message;
            Application_ = application;
        }

        public object CaptureState()
        {
            return new SerializableReactionPost(this);
        }

        public async Task RestoreState(object state)
        {
            SerializableReactionPost post = (SerializableReactionPost)state;

            DiscordGuild guild = await Bot.Client.GetGuildAsync(post.GuildId).ConfigureAwait(false);
            DiscordChannel channel = guild.GetChannel(post.ChannelId);
            DiscordMessage_ = await channel.GetMessageAsync(post.MessageId).ConfigureAwait(false);
            Application_ = ApplicationCommands.GetApplication(post.ApplicationTitle);
        }
    }
}
