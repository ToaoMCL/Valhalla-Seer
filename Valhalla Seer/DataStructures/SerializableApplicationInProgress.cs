using System;
using System.Collections.Generic;
using System.Text;

namespace Valhalla_Seer.DataStructures
{
    [System.Serializable]
    class SerializableApplicationInProgress
    {
        public readonly ulong GuildId;
        public readonly ulong ApplicantId;
        public readonly uint ResponseCount;
        public readonly string Title;
        public readonly string[] Responses;
        public readonly string ApplicationTitle;

        public SerializableApplicationInProgress(ApplicationInProgress applicationInProgress)
        {
            this.GuildId = applicationInProgress.Guild.Id;
            this.ApplicantId = applicationInProgress.Applicant.Id;
            this.ResponseCount = applicationInProgress.ResponseCount;
            this.Title = applicationInProgress.Title;        
            this.Responses = applicationInProgress.Responses;
            this.ApplicationTitle = applicationInProgress.Application_.Title;
        }
    }
}
