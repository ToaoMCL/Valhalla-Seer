using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Valhalla_Seer.Saving
{
    internal static class SavingSystem
    {
        const string SAVE_FOLDER = "Data";
        static bool saving = false;

        /// <summary>
        /// Saves a collection of serializable object states in a dictonary where dictionary key is the file name
        /// </summary>
        /// <param name="FolderName"> name of the folder that the dictionary will be saved to</param>
        /// <param name="objects"> the collection of objects to save</param>
        public static void Save(string FolderName, Dictionary<string, object> objects)
        {
            if (saving) return;
            saving = true;   
            foreach(KeyValuePair<string, object> obj in objects)
            {
                //object state = LoadFile(fileName);
                // object state = obj;
                // CaptureState(state, objects);
                SaveFile(FolderName, obj.Key, obj.Value);
            }
            saving = false;
        }

        public static object LoadFile(string saveFolder, string saveFile)
        {
            string path = GetPathFromSaveFile(saveFolder, saveFile);
            if (!File.Exists(path)) return null;

            using (FileStream stream = File.Open(path, FileMode.Open))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                return formatter.Deserialize(stream);
            }
        }

        public static List<object> LoadFileTypes(string folder, string fileType)
        {
            string directory = GetDirectoryFromSaveFile(folder);
            //string path = GetPathFromSaveFile(folder, fileType);
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

            string[] filePaths = Directory.GetFiles(directory, "*" + fileType);
            BinaryFormatter formatter = new BinaryFormatter();
            List<object> objectsFound = new List<object>();
            foreach (string path in filePaths)
            {
                using (FileStream stream = File.Open(path, FileMode.Open))
                {
                    objectsFound.Add(formatter.Deserialize(stream));
                }
            }
            return objectsFound;
        }

        private static void SaveFile(string saveFolder, string saveFile, object state)
        {
            string directory = GetDirectoryFromSaveFile(saveFolder);
            string path = GetPathFromSaveFile(saveFolder, saveFile);
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
            Console.WriteLine("Saving to " + path);
            using FileStream stream = File.Open(path, FileMode.Create);
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(stream, state);
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

        private static string GetDirectoryFromSaveFile(string saveFile) { return Path.Combine(Environment.CurrentDirectory, SAVE_FOLDER, saveFile); }
        private static string GetPathFromSaveFile(string saveFolder, string saveFile) { return Path.Combine(Environment.CurrentDirectory, SAVE_FOLDER, saveFolder, saveFile); }
        public static void Delete(string saveFolder, string saveFile) { File.Delete(GetPathFromSaveFile(saveFolder, saveFile)); }
    }
}
