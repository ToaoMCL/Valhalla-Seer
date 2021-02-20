using System;
using System.Collections.Generic;
using System.Text;

namespace Valhalla_Seer.DataStructures
{
    [System.Serializable]
    class SerializableApplication
    {
        public string Title { get; private set; }
        public Question[] Questions { get; private set; }
        public ulong[] RoleIds { get; private set; }

        public SerializableApplication(Application application)
        {
            this.Title = application.Title;

            int i = 0;
            this.Questions = new Question[application.Questions.Count];
            foreach(Question question in application.Questions)
            {
                this.Questions[i] = question;
                i++;
            }
            i = 0;
            this.RoleIds = new ulong[application.RoleIdList.Count];
            foreach(ulong id in application.RoleIdList)
            {
                this.RoleIds[i] = id;
                i++;
            }
        }
    }
}
