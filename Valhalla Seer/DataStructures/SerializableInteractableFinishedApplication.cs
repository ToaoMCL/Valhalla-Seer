using System;
using System.Collections.Generic;
using System.Text;

namespace Valhalla_Seer.DataStructures
{
    [System.Serializable]
    class SerializableInteractableFinishedApplication
    {
        public ulong MessageId { get; private set; }
        public ulong ChannelId { get; private set; }
        public string[] Responses { get; private set; }

        public SerializableInteractableFinishedApplication(InteractableFinishedApplication interactableFinishedApplication)
        {
            this.MessageId = interactableFinishedApplication.Message.Id;
            this.ChannelId = interactableFinishedApplication.Message.ChannelId;
            this.Responses = interactableFinishedApplication.Responses;
        }
    }
}
