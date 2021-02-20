using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Valhalla_Seer.DataStructures
{
    [System.Serializable]
    public class Question
    {
        public string Title { get; private set; }
        public string Description { get; private set; }
        public string Help { get; private set; }
        public bool ShortQuestion { get; private set; } = true;

        public Question()
        {
            this.Title = "No Title";
            this.Description = "No description.";
            this.Help = "No help provided.";
        }

        public void ChangeTitle(string newTitle) => this.Title = RemovePrefix(newTitle);
        public void ChangeDescription(string newDescription) => this.Description = newDescription;
        public void ChangeHelp(string newHelp) => this.Help = newHelp;
        public void ChangeQuestionType() => ShortQuestion = !ShortQuestion;
        
        private static string RemovePrefix(string title)
        {
            var json = string.Empty;
            using (var fs = File.OpenRead("config.json"))
            {
                using (var sr = new StreamReader(fs, new UTF8Encoding(false)))
                {
                    json = sr.ReadToEnd();
                }
            }
            var configJson = JsonConvert.DeserializeObject<ConfigJson>(json);
            return title.Replace(configJson.Prefix, "");
        }
    }
}
