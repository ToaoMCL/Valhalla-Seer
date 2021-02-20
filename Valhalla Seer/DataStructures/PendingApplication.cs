using DSharpPlus.Entities;
using System.Collections.Generic;
using Valhalla_Seer.Saving;
using System.Threading.Tasks;

namespace Valhalla_Seer.DataStructures
{
    class PendingApplication : ISavable
    {
        public DiscordMessage Message { get; private set; }
        public DiscordMember Applicant { get; private set; }
        public string Title { get; private set; }
        public string[] ApplicantRespones { get; private set; }

        ApplicationInProgress applicationInProgress;
        

        public PendingApplication(DiscordMessage message, DiscordMember applicant, ApplicationInProgress applicationInProgress, object saveData = null)
        {
            if (saveData != null) return;
            this.Message = message;
            this.Applicant = applicant;
            this.ApplicantRespones = applicationInProgress.GetApplicationResults();
            this.Title = applicationInProgress.Title;
            this.applicationInProgress = applicationInProgress;
        }

        public object CaptureState() { return new SerializablePendingApplication(this, applicationInProgress); }

        public async Task RestoreState(object state)
        {
            SerializablePendingApplication saveData = (SerializablePendingApplication)state;
            DiscordGuild guild = await Bot.Client.GetGuildAsync(saveData.GuildID).ConfigureAwait(false);
            DiscordChannel channel = guild.GetChannel(saveData.ChannelID);
            DiscordMessage message = await channel.GetMessageAsync(saveData.MessageID).ConfigureAwait(false);
            DiscordMember applicant = await guild.GetMemberAsync(saveData.ApplicatntID).ConfigureAwait(false);

            this.Message = message;
            this.Applicant = applicant;
            this.ApplicantRespones = saveData.ApplicantResponses;
            this.Title = saveData.Title;
            this.applicationInProgress = new ApplicationInProgress(null, null, null);
            await this.applicationInProgress.RestoreState(saveData.serializableApplicationInProgress).ConfigureAwait(false);
        }

        internal async Task GrantRoles()
        {
            List<DiscordRole> authorisedRoles = new List<DiscordRole>();
            foreach (ulong id in applicationInProgress.Application_.RoleIdList) authorisedRoles.Add(applicationInProgress.Guild.GetRole(id));
            foreach(DiscordRole role in authorisedRoles) await Applicant.GrantRoleAsync(role).ConfigureAwait(false);

        }
    }
}
