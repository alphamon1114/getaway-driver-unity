using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Getaway.Editor
{
    public static class ProjectSetup
    {
        public const string ScenePath = "Assets/Getaway/Scenes/Getaway.unity";
        [MenuItem("Getaway/Create or rebuild demo scene")]
        public static void CreateDemo()
        {
            Directory.CreateDirectory("Assets/Getaway/Scenes");
            Directory.CreateDirectory("Assets/Getaway/Stages");
            Directory.CreateDirectory("Assets/Getaway/Art/Varco");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var light = new GameObject("Sun").AddComponent<Light>();
            light.type = LightType.Directional; light.intensity = 1.2f;
            light.transform.rotation = Quaternion.Euler(45, -30, 0);
            light.shadows = LightShadows.Soft;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.65f, 0.7f, 0.75f);
            RenderSettings.fog = true; RenderSettings.fogColor = new Color(0.48f, 0.60f, 0.67f); RenderSettings.fogDensity = 0.0025f;
            var root = new GameObject("Getaway Systems");
            root.AddComponent<WorldBuilder>(); root.AddComponent<RunLogger>();
            var session = root.AddComponent<GameSession>(); root.AddComponent<GameHud>();
            session.stages = new StageDefinition[3];
            string[] titles = { "The First Job", "Downtown Heat", "The Last Ride" };
            string[] briefs = {
                "You are the crew's driver. Collect three members outside the bank, lose the patrol cars, and bring everyone to the safehouse.",
                "The next job has drawn more attention. Four crew members need a ride through the downtown roadworks. Keep the car intact.",
                "One final getaway. The whole crew is counting on you. Break away from four patrol cars and reach the distant hideout."
            };
            for (int i = 0; i < 3; i++)
            {
                string path = $"Assets/Getaway/Stages/Stage{i + 1:00}.asset";
                var stage = AssetDatabase.LoadAssetAtPath<StageDefinition>(path);
                if (stage == null)
                {
                    stage = ScriptableObject.CreateInstance<StageDefinition>();
                    stage.title = titles[i]; stage.briefing = briefs[i]; stage.roadLength = 650 + i * 200;
                    stage.timeLimit = 100 + i * 20; stage.policeCount = 2 + i; stage.policeSpeed = 24 + i * 2;
                    stage.escapeDistance = 65 + i * 5; stage.escapeSeconds = 5 + i;
                    stage.crewCount = 3 + i; stage.seed = 17 + i * 11;
                    stage.bankDistance = 180 + i * 60; stage.crewExitTime = 14 + i * 4;
                    stage.startingLoot = 12000 + i * 6000; stage.cashLossPerDamage = 120 + i * 60;
                    AssetDatabase.CreateAsset(stage, path);
                }
                session.stages[i] = stage;
            }
            PlayerSettings.companyName = "GetawayPrototype";
            PlayerSettings.productName = "GetawayDriver";
            PlayerSettings.defaultScreenWidth = 1280; PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.runInBackground = true;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            EditorSettings.serializationMode = SerializationMode.ForceText;
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            Debug.Log("GETAWAY_SETUP_OK");
        }
        [MenuItem("Getaway/Build Windows")]
        public static void BuildWindows()
        {
            if (!File.Exists(ScenePath)) CreateDemo();
            Directory.CreateDirectory("Builds/Windows");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { ScenePath }, locationPathName = "Builds/Windows/GetawayDriver.exe",
                target = BuildTarget.StandaloneWindows64, options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new System.Exception("Windows build failed: " + report.summary.result);
            Debug.Log("GETAWAY_BUILD_OK " + report.summary.totalSize);
        }
    }
}
