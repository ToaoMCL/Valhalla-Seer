using System.Collections.Generic;
using System.Threading.Tasks;
using Valhalla_Seer.Saving;
using System;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Newtonsoft.Json;
using System.IO;
using System.Text;

namespace Valhalla_Seer.DataStructures
{
    public class Application : ISavable
    {
        
        public string Title { get; private set; }
        public List<Question> Questions { get; private set; } = new List<Question>();
        public List<ulong> RoleIdList { get; private set; } = new List<ulong>();


        public Application(string title)
        {
            this.Title = title;
        }

        public void ChangeTitle(string title)
        {
            this.Title = RemovePrefix(title);
        }

        

        public Question AddQuestion()
        {
            Question newQuestion = new Question();
            Questions.Add(newQuestion);
            return newQuestion;
        }

        public Question GetQuestion(string title)
        {
            Question question = null;
            foreach(Question q in Questions)
            {
                if(title == q.Title)
                {
                    question = q;
                    break;
                }
            }

            return question;
        }

        public object CaptureState() { return new SerializableApplication(this); }

        public Task RestoreState(object state)
        {
            SerializableApplication serializableApplication = (SerializableApplication)state;

            this.Title = serializableApplication.Title;
            Questions = new List<Question>();
            foreach(Question question in serializableApplication.Questions) this.Questions.Add(question);
            foreach (ulong id in serializableApplication.RoleIds) this.RoleIdList.Add(id);
            return Task.CompletedTask;
        }

        internal void AddRole(CommandContext ctx, ulong roleId)
        {
            DiscordRole role = ctx.Guild.GetRole(roleId);
            if (role == null) return;
            RoleIdList.Add(roleId);
        }
        internal void RemoveRole(ulong roleId)
        {
            if (RoleIdList.Contains(roleId)) RoleIdList.Remove(roleId);
        }

        internal void RemoveQuestion(Question question)
        {
            Questions.Remove(question);
        }
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