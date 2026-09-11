using System.IO;
using UnityEngine;

namespace Getaway
{
    public static class ProgressStore
    {
        public static string SavePath => Path.Combine(Application.persistentDataPath, "progress.json");
        static CareerStorage storage;
        static string storagePath;
        static CareerStorage Storage
        {
            get
            {
                if (storage == null || storagePath != SavePath)
                {
                    storagePath = SavePath;
                    storage = new CareerStorage(storagePath, data => JsonUtility.ToJson(data, true), json => JsonUtility.FromJson<CareerData>(json));
                }
                return storage;
            }
        }
        public static bool ReadOnly => Storage.ReadOnly;
        public static string Status => Storage.Status;
        public static CareerData Load()
        {
            var data = Storage.Load();
            if (!string.IsNullOrEmpty(Status)) Debug.LogWarning(Status);
            return data;
        }
        public static bool Save(CareerData data)
        {
            bool ok = Storage.Save(data);
            if (!ok) Debug.LogWarning(Status);
            return ok;
        }
    }
}
