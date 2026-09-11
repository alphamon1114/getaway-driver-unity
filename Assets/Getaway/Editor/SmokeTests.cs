using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Getaway.Editor
{
    // Runs in a real Unity Play Mode session; no NUnit package download required.
    [InitializeOnLoad]
    public static class SmokeTests
    {
        const string Key = "GetawaySmokeTests";
        static IEnumerator routine;
        static double deadline;
        static readonly List<string> passed = new List<string>();
        static string oldProduct;
        static SmokeTests() { EditorApplication.playModeStateChanged += OnState; }
        public static void Run()
        {
            if (!File.Exists(ProjectSetup.ScenePath)) ProjectSetup.CreateDemo();
            oldProduct = PlayerSettings.productName;
            SessionState.SetString("GetawayOriginalProduct", oldProduct);
            // Do not overwrite a human player's progress during tests.
            PlayerSettings.productName = "GetawayDriver-AutomatedTests-" + Guid.NewGuid().ToString("N");
            SessionState.SetBool(Key, true);
            EditorSceneManager.OpenScene(ProjectSetup.ScenePath);
            EditorApplication.isPlaying = true;
        }
        static void OnState(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(Key, false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                passed.Clear(); deadline = EditorApplication.timeSinceStartup + 90;
                routine = Verify(); EditorApplication.update += Step;
            }
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                SessionState.SetBool(Key, false);
                PlayerSettings.productName = SessionState.GetString("GetawayOriginalProduct", "GetawayDriver");
                AssetDatabase.SaveAssets();
                if (Application.isBatchMode) EditorApplication.Exit(SessionState.GetInt("GetawayTestExit", 1));
            }
        }
        static void Step()
        {
            try
            {
                if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Play mode test timed out");
                if (routine.MoveNext()) return;
                Complete(0, "PASS\n" + string.Join("\n", passed));
            }
            catch (Exception e) { Complete(1, "FAIL\n" + string.Join("\n", passed) + "\n" + e); }
        }
        static void Complete(int exit, string report)
        {
            EditorApplication.update -= Step;
            Directory.CreateDirectory("Docs/Validation");
            File.WriteAllText("Docs/Validation/unity-smoke-tests.txt", DateTime.UtcNow.ToString("O") + "\nUnity " + Application.unityVersion + "\n" + report);
            Debug.Log(report);
            SessionState.SetInt("GetawayTestExit", exit);
            EditorApplication.isPlaying = false;
        }
        static void Check(bool condition, string label)
        {
            if (!condition) throw new Exception(label);
            passed.Add(label);
        }
        static void Teleport(ArcadeCar car, Vector3 pos)
        {
            car.Body.position = pos; car.transform.position = pos;
            car.Body.linearVelocity = Vector3.zero; Physics.SyncTransforms();
        }
        static void BoardNow(GameSession game)
        {
            Teleport(game.World.Player, game.World.Pickup);
            game.Tick(Mathf.Max(0.01f, game.Stage.crewExitTime - game.Elapsed + 0.01f));
            game.Tick(2.1f);
        }
        static IEnumerator Verify()
        {
            yield return null;
            var game = UnityEngine.Object.FindAnyObjectByType<GameSession>();
            Check(game != null && game.World.Player != null, "Scene starts and creates player");
            game.LoadStage(0); yield return null;
            Check(game.stages.Length == 3 && game.State == MissionState.Briefing, "Three stages and briefing");
            game.Begin(); game.Tick(1);
            Check(game.State == MissionState.Pickup && game.Boarding == 0, "Cannot board outside pickup zone");
            var car = game.World.Player;
            Teleport(car, game.World.Pickup);
            car.Body.linearVelocity = Vector3.forward * 5;
            game.Tick(2.1f);
            Check(game.State == MissionState.Pickup, "Cannot board while driving fast");
            car.Body.linearVelocity = Vector3.zero; game.Tick(0.1f);
            Check(!game.Appointment.CrewAvailable && game.Loot == 0 && game.World.Police.Count == 0, "No crew, loot or pursuit before scheduled bank exit");
            int earlyScore = game.ArrivalScore;
            BoardNow(game);
            Check(game.ArrivalScore == earlyScore, "Waiting at bank preserves first-arrival score");
            Check(game.State == MissionState.Chase && game.World.Police.Count == 2, "Boarding triggers pursuit");
            int cash = game.Loot;
            car.ApplyDamage(10, "wall");
            Check(game.Loot == cash, "Environment damage does not deduct money");
            car.ApplyDamage(10, "police", true);
            Check(game.Loot == cash - 1200 && game.LostLoot == 1200, "Police damage event deducts proportional money");
            Teleport(car, game.World.Destination); game.Tick(0.1f);
            Check(game.State == MissionState.Chase, "Destination blocked while wanted");
            Teleport(car, game.World.Pickup + Vector3.forward * 160); game.Tick(2);
            var cop = game.World.Police[0].GetComponent<ArcadeCar>();
            Teleport(cop, car.transform.position + Vector3.right * 10); game.Tick(0.1f);
            Check(game.EscapeProgress == 0, "Escape countdown resets when police approach");
            Teleport(cop, new Vector3(4, 0.8f, -5)); game.Tick(game.Stage.escapeSeconds + 0.1f);
            Check(game.State == MissionState.Escaped, "Sustained separation evades police");
            Teleport(car, game.World.Destination); car.Body.linearVelocity = Vector3.forward * 5; game.Tick(0.1f);
            Check(game.State == MissionState.Escaped, "Must stop at destination");
            car.Body.linearVelocity = Vector3.zero; game.Tick(0.1f);
            Check(game.State == MissionState.Won && ProgressStore.Load().highestUnlocked >= 1, "Delivery wins and persists unlock");
            long wallet = game.Progress.wallet;
            Check(wallet == game.Loot && !game.PendingPayout, "Escaped cash is banked once");
            game.RetryPayout(); game.Tick(1);
            Check(game.Progress.wallet == wallet, "Repeated result updates cannot duplicate payout");
            game.OpenShop(); game.BuyUpgrade(UpgradeKind.Engine); game.CloseShop();
            Check(game.Account.Selected.engine == 1 && ProgressStore.Load().vehicles[0].engine == 1, "Shop upgrade persists through Unity JSON serialization");
            game.LoadStage(1); yield return null; game.Begin();
            float remaining = game.Remaining; game.TogglePause(); game.Tick(10);
            Check(Mathf.Approximately(remaining, game.Remaining), "Pause freezes mission time");
            game.TogglePause(); game.Tick(game.Stage.timeLimit + 1);
            Check(game.State == MissionState.Lost, "Timeout loses mission");
            Check(game.Progress.wallet == wallet - 3000, "Failure leaves banked wallet intact");
            game.LoadStage(0); yield return null; game.Begin(); game.World.Player.ApplyDamage(100, "test"); game.Tick(0.1f);
            Check(game.State == MissionState.Lost, "Vehicle destruction loses mission");
            game.LoadStage(0); yield return null; game.Begin();
            BoardNow(game);
            Teleport(game.World.Police[0].GetComponent<ArcadeCar>(), game.World.Player.transform.position + Vector3.right * 4);
            game.Tick(4.1f); Check(game.State == MissionState.Lost, "Stationary surrounded player is arrested");
            game.LoadStage(0); yield return null; game.Begin();
            car = game.World.Player; car.playerControlled = false; car.Throttle = 1;
            float start = car.transform.position.z; double until = EditorApplication.timeSinceStartup + 2;
            while (EditorApplication.timeSinceStartup < until) yield return null;
            Check(car.transform.position.z > start + 5 && car.Speed > 3, "Rigidbody driving advances under real physics");
            car.Throttle = 0; car.Braking = true; until = EditorApplication.timeSinceStartup + 1.5;
            while (EditorApplication.timeSinceStartup < until) yield return null;
            Check(car.Speed < 2, "Brake stops moving car");
            game.LoadStage(2); yield return null; game.Begin();
            BoardNow(game);
            Check(game.World.Police.Count == 4, "Final stage spawns four police");
            float copStart = game.World.Police[0].transform.position.z;
            Teleport(game.World.Player, game.World.Pickup + Vector3.forward * 45);
            until = EditorApplication.timeSinceStartup + 2;
            while (EditorApplication.timeSinceStartup < until) yield return null;
            Check(game.World.Police[0].transform.position.z > copStart + 3, "Police AI pursues under real physics");
            game.Log("test_complete");
            string[] files = Directory.GetFiles(game.Logger.LogDirectory, "*.jsonl");
            Check(files.Length > 0, "JSONL logs are written");
        }
    }
}
