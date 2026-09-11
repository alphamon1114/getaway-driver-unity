using System;
using System.IO;

namespace Getaway
{
    // JSON is supplied by the host (Unity JsonUtility in-game). IO and recovery are shared with tests.
    public sealed class CareerStorage
    {
        readonly string path;
        readonly Func<CareerData, string> encode;
        readonly Func<string, CareerData> decode;
        bool recoveredPrimary;
        public bool ReadOnly { get; private set; }
        public string Status { get; private set; } = "";
        public CareerStorage(string savePath, Func<CareerData, string> serialize, Func<string, CareerData> deserialize)
        { path = savePath; encode = serialize; decode = deserialize; }
        public CareerData Load()
        {
            ReadOnly = false; recoveredPrimary = false; Status = "";
            try
            {
                if (File.Exists(path)) return Read(path);
                if (!File.Exists(path + ".bak")) return NewProfile();
            }
            catch (NotSupportedException e) { return Block(e.Message); }
            catch (Exception e) { Status = e.Message; }
            try
            {
                var recovered = Read(path + ".bak");
                if (File.Exists(path)) File.Copy(path, path + ".corrupt-" + Guid.NewGuid().ToString("N"), false);
                recoveredPrimary = true; Status = "Recovered career from backup.";
                return recovered;
            }
            catch (Exception e) { return Block("Cannot read career save. Existing files preserved. " + e.Message); }
        }
        CareerData Block(string reason) { ReadOnly = true; Status = reason; return NewProfile(); }
        static CareerData NewProfile() { var data = new CareerData(); GarageCatalog.Normalize(data); return data; }
        CareerData Read(string source)
        {
            string json = File.ReadAllText(source);
            if (!json.Contains("\"highestUnlocked\"") || !json.Contains("\"completedRuns\"")) throw new IOException("Incomplete career file.");
            var data = decode(json);
            if (data != null && data.schemaVersion > 2) throw new NotSupportedException("Newer save version. Update the game before using this profile.");
            GarageCatalog.Normalize(data); return data;
        }
        public bool Save(CareerData data)
        {
            if (ReadOnly) return false;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                string temp = path + ".tmp";
                using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    byte[] bytes = System.Text.Encoding.UTF8.GetBytes(encode(data));
                    stream.Write(bytes, 0, bytes.Length); stream.Flush(true);
                }
                Read(temp);
                if (File.Exists(path))
                {
                    string backup = recoveredPrimary ? path + ".replaced-" + Guid.NewGuid().ToString("N") : path + ".bak";
                    // Content replacement remains atomic; optional metadata merging must not
                    // prevent saving in environments that restrict ACL/attribute operations.
                    File.Replace(temp, path, backup, true);
                }
                else File.Move(temp, path);
                recoveredPrimary = false; Status = "Saved."; return true;
            }
            catch (Exception e) { Status = "Save failed: " + e.Message; return false; }
        }
    }
}
