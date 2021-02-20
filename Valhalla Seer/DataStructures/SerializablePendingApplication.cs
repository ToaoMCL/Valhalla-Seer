using System;
using System.Collections.Generic;
using System.Text;

namespace Valhalla_Seer.DataStructures
{
    [System.Serializable]
    class SerializablePendingApplication
    {
        public string Title { get; private set; }
        public ulong GuildID { get; private set; }
        public ulong ChannelID { get; private set; }
        public ulong MessageID { get; private set; }
        public ulong ApplicatntID { get; private set; }
        public string[] ApplicantResponses { get; private set; }
        public SerializableApplicationInProgress serializableApplicationInProgress { get; private set; }

        public SerializablePendingApplication(PendingApplication pendingApplication, ApplicationInProgress applicationInProgress)
        {
            Title = pendingApplication.Title;
            GuildID = pendingApplication.Message.Channel.GuildId;
            ChannelID = pendingApplication.Message.ChannelId;
            MessageID = pendingApplication.Message.Id;
            ApplicatntID = pendingApplication.Applicant.Id;
            ApplicantResponses = pendingApplication.ApplicantRespones;
            serializableApplicationInProgress = new SerializableApplicationInProgress(applicationInProgress);
        }
    }
}
