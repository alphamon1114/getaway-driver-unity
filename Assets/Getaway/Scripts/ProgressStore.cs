using System;
using System.IO;
using UnityEngine;

namespace Getaway
{
    public static class ProgressStore
    {
        [Serializable] public sealed class Data { public int highestUnlocked; public int completedRuns; }
        public static string SavePath => Path.Combine(Application.persistentDataPath, "progress.json");
        public static Data Load()
        {
            try { return File.Exists(SavePath) ? JsonUtility.FromJson<Data>(File.ReadAllText(SavePath)) ?? new Data() : new Data(); }
            catch (Exception e) { Debug.LogWarning("Progress load failed: " + e.Message); return new Data(); }
        }
        public static bool Save(Data data)
        {
            try
            {
                Directory.CreateDirectory(Application.persistentDataPath);
                string temp = SavePath + ".tmp";
                File.WriteAllText(temp, JsonUtility.ToJson(data, true));
                if (File.Exists(SavePath)) File.Replace(temp, SavePath, SavePath + ".bak");
                else File.Move(temp, SavePath);
                return true;
            }
            catch (Exception e) { Debug.LogWarning("Progress save failed: " + e.Message); return false; }
        }
    }
}
