using System;
using System.IO;
using UnityEngine;

namespace Getaway
{
    public sealed class RunLogger : MonoBehaviour
    {
        [Serializable] sealed class Entry
        {
            public string utc, session, type, detail;
            public int stage;
            public float elapsed, x, z, speedKph, health;
            public string runId;
            public int loot, arrivalScore;
            public long wallet;
        }
        public string LogDirectory { get; private set; }
        string session;
        StreamWriter eventsFile, consoleFile;
        bool failed;
        void Awake()
        {
            session = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff") + "_" + Guid.NewGuid().ToString("N").Substring(0, 8);
            LogDirectory = Path.Combine(Application.persistentDataPath, "Logs");
            try
            {
                Directory.CreateDirectory(LogDirectory);
                eventsFile = new StreamWriter(Path.Combine(LogDirectory, session + ".jsonl")) { AutoFlush = true };
                consoleFile = new StreamWriter(Path.Combine(LogDirectory, session + "_console.txt")) { AutoFlush = true };
                Application.logMessageReceived += ConsoleMessage;
                Write("session_start", 0, 0, null, Application.unityVersion);
            }
            catch (Exception e) { DisableLogging(e); }
        }
        public void Write(string type, int stage, float elapsed, ArcadeCar car, string detail = "", string runId = "", int loot = 0, long wallet = 0, int arrivalScore = 0)
        {
            if (failed || eventsFile == null) return;
            var entry = new Entry { utc = DateTime.UtcNow.ToString("O"), session = session, type = type, stage = stage, elapsed = elapsed, detail = detail };
            entry.runId = runId; entry.loot = loot; entry.wallet = wallet; entry.arrivalScore = arrivalScore;
            if (car != null) { entry.x = car.transform.position.x; entry.z = car.transform.position.z; entry.speedKph = car.Kph; entry.health = car.health; }
            try { eventsFile.WriteLine(JsonUtility.ToJson(entry)); }
            catch (Exception e) { DisableLogging(e); }
        }
        void ConsoleMessage(string condition, string stack, LogType type)
        {
            if (failed || consoleFile == null) return;
            try { consoleFile.WriteLine($"{DateTime.UtcNow:O} [{type}] {condition}\n{stack}"); }
            catch (Exception e) { DisableLogging(e); }
        }
        void DisableLogging(Exception e)
        {
            failed = true;
            Application.logMessageReceived -= ConsoleMessage;
            Debug.LogWarning("Run logging unavailable: " + e.Message);
        }
        void OnDestroy()
        {
            Write("session_end", 0, 0, null);
            Application.logMessageReceived -= ConsoleMessage;
            try { eventsFile?.Dispose(); consoleFile?.Dispose(); }
            catch (IOException) { }
        }
    }
}
