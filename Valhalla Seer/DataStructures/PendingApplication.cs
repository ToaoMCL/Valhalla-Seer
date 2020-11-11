using DSharpPlus.Entities;
using DSharpPlus.Net;
using DSharpPlus;
using System;
using System.Collections.Generic;
using System.Text;
using Valhalla_Seer.Saving;
using System.Threading.Tasks;

namespace Valhalla_Seer.DataStructures
{
    class PendingApplication : ISaving
    {
        public DiscordMessage Message { get; private set; }
        public DiscordMember Applicant { get; private set; }
        public string Title { get; private set; }
        public string ApplicantRespones { get; private set; }

        public PendingApplication(DiscordMessage message, DiscordMember applicant, ApplicationInProgress applicationInProgress, object saveData = null)
        {
            if (saveData != null) return;

            this.Message = message;
            this.Applicant = applicant;
            this.ApplicantRespones = applicationInProgress.GetApplicationResults();
            this.Title = applicationInProgress.Title;
        }

        public object CaptureState() { return new SerializablePendingApplication(this); }

        public async Task RestoreState(object state)
        {
            SerializablePendingApplication application = (SerializablePendingApplication)state;
            DiscordGuild guild = await Bot.Client.GetGuildAsync(application.GuildID).ConfigureAwait(false);
            DiscordChannel channel = guild.GetChannel(application.ChannelID);
            DiscordMessage message = await channel.GetMessageAsync(application.MessageID).ConfigureAwait(false);
            DiscordMember applicant = await guild.GetMemberAsync(application.ApplicatntID).ConfigureAwait(false);

            this.Message = message;
            this.Applicant = applicant;
            this.ApplicantRespones = application.ApplicantResponses;
            this.Title = application.Title;
        }
    }
}
