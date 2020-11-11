using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Valhalla_Seer.Saving
{
    internal static class SavingSystem
    {
        public static void Save(string fileName, List<object> objects)
        {
            Dictionary<string, object> state = Load(fileName);
            CaptureState(state, objects);
            SaveFile(fileName, state);
        }

        public static Dictionary<string, object> Load(string saveFile)
        {
            string path = GetPathFromSaveFile(saveFile);
            if (!File.Exists(path))
            {
                return new Dictionary<string, object>();
            }
            using (FileStream stream = File.Open(path, FileMode.Open))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                return (Dictionary<string, object>)formatter.Deserialize(stream);
            }
        }
        private static void SaveFile(string saveFile, object state)
        {
            string path = GetPathFromSaveFile(saveFile);
            Console.WriteLine("Saving to " + path);
            using (FileStream stream = File.Open(path, FileMode.Create))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(stream, state);
            }
        }

        private static void CaptureState(Dictionary<string, object> state, List<object> objects)
        {
            int i = 0;
            foreach (object saveable in objects)
            {
                state[i.ToString()] = saveable;
                i++;
            }
        }


        private static string GetPathFromSaveFile(string saveFile) { return Path.Combine(Environment.CurrentDirectory, saveFile); }
        public static void Delete(string saveFile) { File.Delete(GetPathFromSaveFile(saveFile)); }
    }
}
