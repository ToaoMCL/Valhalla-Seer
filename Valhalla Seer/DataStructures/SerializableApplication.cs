using System;
using System.Collections.Generic;
using System.Text;

namespace Valhalla_Seer.DataStructures
{
    [System.Serializable]
    class SerializableApplication
    {
        string title;
        Question[] questions;
        

        public SerializableApplication(string title, List<Question> questions)
        {
            this.title = title;


            int i = 0;
            this.questions = new Question[questions.Count];
            foreach(Question question in questions)
            {
                this.questions[i] = question;
                i++;
            }
        }

        public Application ToApplication() 
        {
            Application application = new Application(title);
            foreach(Question question in questions)
            {
                application.AddQuestion(question.Title, question.Description);
            }
            return application; 
        }
    }
}
