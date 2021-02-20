using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Valhalla_Seer.Saving;
using Valhalla_Seer.Commands;

namespace Valhalla_Seer.DataStructures
{
    class ApplicationInProgress : ISavable
    {
        public string Title { get; private set; }
        public DiscordGuild Guild { get; private set; }
        public DiscordMember Applicant { get; private set; }
        public Application Application_ { get; private set; }
        public DiscordMessage LastQuestionMessage { get; private set; }
        public string[] Responses { get; private set; }
        public uint ResponseCount { get; private set; }


        public ApplicationInProgress(DiscordMember member, DiscordGuild guild, Application application)
        {
            if (member == null || guild == null || application == null) return;
            this.Title = member.Username + "'s Application for " + application.Title;
            this.Applicant = member;
            this.Application_ = application;
            this.ResponseCount = 0;
            this.Guild = guild;
            this.Responses = new string[application.Questions.Count];
        }

        public void AddResponse(Question question, string response) 
        {
            string formatedResponse = 
                question.Title + "\n" +
                question.Description + "\n" +
                "Response :" + response + "\n";
            Responses[ResponseCount] = formatedResponse;
            ResponseCount++;
        }

        public string[] GetApplicationResults()
        {
            List<string> results = new List<string>(); 
            string result = "";
            int i = 0;
            foreach (string response in Responses)
            {
                i++;
                // Discord Embed max char length
                if(response.Length > 2048)
                {
                    var a = response.Substring(0, 2048);
                    results.Add(a);
                    result = response.Substring(2048);
                    continue;
                }
                if(response.Length + result.Length > 2048)
                {
                    results.Add(result);
                    result = "";
                }
                result += "\n" + response;

                // Add to list if last response
                // if (i >= Responses.Length) 
            }
            results.Add(result);
            foreach (string a in results) Console.WriteLine("RESPONSE : " + a + "\n\n");
            return results.ToArray();
        }

        public void SetLastQuestion(DiscordMessage message)
        {
            LastQuestionMessage = message;
        }

        public object CaptureState()
        {
            return new SerializableApplicationInProgress(this);
        }
        public async Task RestoreState(object state)
        {
            SerializableApplicationInProgress saveData = (SerializableApplicationInProgress)state;

            this.Guild = await Bot.Client.GetGuildAsync(saveData.GuildId).ConfigureAwait(false);
            this.Applicant = await Guild.GetMemberAsync(saveData.ApplicantId).ConfigureAwait(false);
            this.ResponseCount = saveData.ResponseCount;
            this.Title = saveData.Title;
            this.Responses = saveData.Responses;
            this.Application_ = ApplicationCommands.GetApplication(saveData.ApplicationTitle);

        }


    }
}
