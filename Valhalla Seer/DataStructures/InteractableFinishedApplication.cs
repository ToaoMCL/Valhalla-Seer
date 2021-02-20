using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Valhalla_Seer.Saving;

namespace Valhalla_Seer.DataStructures
{
    class InteractableFinishedApplication : ISavable
    {
        public DiscordMessage Message { get; private set; }
        public string[] Responses { get; private set; }
        public InteractableFinishedApplication(DiscordMessage message, string[] responses)
        {
            this.Message = message;
            this.Responses = responses;
        }

        public object CaptureState()
        {
            return new SerializableInteractableFinishedApplication(this);
        }

        public async Task RestoreState(object state)
        {
            var saveData = (SerializableInteractableFinishedApplication)state;
            DiscordChannel channel = await Bot.Client.GetChannelAsync(saveData.ChannelId).ConfigureAwait(false);
            Message = await channel.GetMessageAsync(saveData.MessageId).ConfigureAwait(false);
            Responses = saveData.Responses;
        }
    }
}
