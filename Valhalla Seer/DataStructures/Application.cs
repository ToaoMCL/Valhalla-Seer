using System.Collections.Generic;
using System.Threading.Tasks;
using Valhalla_Seer.Saving;
namespace Valhalla_Seer.DataStructures
{
    internal class Application : ISaving
    {
        
        public string Title { get; private set; }
        public List<Question> Questions { get; private set; } = new List<Question>();


        public Application(string Title)
        {
            this.Title = Title;
        }

        public void AddQuestion(string Title, string Description) => Questions.Add(new Question(Title, Description));

        public object CaptureState()
        {
            SerializableApplication serializableApplication = new SerializableApplication(
                Title,
                Questions
                );

            return serializableApplication;
        }

        public async Task RestoreState(object state)
        {
            SerializableApplication serializableApplication = (SerializableApplication)state;
            Application application = serializableApplication.ToApplication();

            this.Title = application.Title;
            this.Questions = application.Questions;
        }

       

    }


}