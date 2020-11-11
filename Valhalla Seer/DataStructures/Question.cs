using System;
using System.Collections.Generic;
using System.Text;

namespace Valhalla_Seer.DataStructures
{
    [System.Serializable]
    internal class Question
    {
        public string Title { get; private set; }
        public string Description { get; private set; }

        public Question(string Title, string Description)
        {
            this.Title = Title;
            this.Description = Description;
        }
    }
}
