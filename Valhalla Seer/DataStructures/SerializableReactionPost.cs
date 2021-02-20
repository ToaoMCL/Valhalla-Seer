using System;
using System.Collections.Generic;
using System.Text;

namespace Valhalla_Seer.DataStructures
{
    [System.Serializable]
    class SerializableReactionPost
    {
        public string ApplicationTitle { get; private set; }
        public ulong MessageId { get; private set; }
        public ulong ChannelId { get; private set; }
        public ulong GuildId { get; private set; }


        public SerializableReactionPost(ReactionPost reactionPost)
        {
            MessageId = reactionPost.DiscordMessage_.Id;
            ChannelId = reactionPost.DiscordMessage_.ChannelId;
            GuildId = reactionPost.DiscordMessage_.Channel.GuildId;
            ApplicationTitle = reactionPost.Application_.Title;
        }

    }
}
