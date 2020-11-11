using System;
using System.Collections.Generic;
using System.Text;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;

namespace Valhalla_Seer.DataStructures
{
    class ApplicationInProgress
    {
        public string Title { get; }

        readonly List<string> responses = new List<string>();

        public ApplicationInProgress(CommandContext ctx, Application application)
        {
            Title = ctx.User.Username + "'s Application for " + application.Title;
        }

        public void AddResponse(string response) => responses.Add(response);

        public string GetApplicationResults()
        {
            string result = "";
            foreach(string response in responses) result += Environment.NewLine + response;
            return result;
        }
    }
}
