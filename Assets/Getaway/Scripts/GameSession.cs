using UnityEngine;

namespace Getaway
{
    public enum MissionState { Briefing, Pickup, Chase, Escaped, Won, Lost }

    [RequireComponent(typeof(WorldBuilder), typeof(RunLogger))]
    public sealed class GameSession : MonoBehaviour
    {
        public StageDefinition[] stages;
        public MissionState State { get; private set; }
        public int StageIndex { get; private set; }
        public bool Paused { get; private set; }
        public float Remaining { get; private set; }
        public float Boarding { get; private set; }
        public float EscapeProgress { get; private set; }
        public float ArrestProgress { get; private set; }
        public float NearestPolice { get; private set; }
        public string Result { get; private set; }
        public WorldBuilder World { get; private set; }
        public RunLogger Logger { get; private set; }
        public StageDefinition Stage => stages[StageIndex];
        public bool Active => State == MissionState.Pickup || State == MissionState.Chase || State == MissionState.Escaped;
        public ProgressStore.Data Progress { get; private set; }
        public Vector3 Objective => State == MissionState.Pickup ? World.Pickup : World.Destination;
        float elapsed, sampleTimer, recoveryCooldown;
        FollowCamera follow;

        void Start()
        {
            World = GetComponent<WorldBuilder>(); Logger = GetComponent<RunLogger>();
            Progress = ProgressStore.Load();
            if (stages == null || stages.Length == 0) { Debug.LogError("No stages configured."); enabled = false; return; }
            if (FindAnyObjectByType<Light>() == null)
            {
                var sun = new GameObject("Sun").AddComponent<Light>();
                sun.type = LightType.Directional; sun.intensity = 1.2f;
                sun.transform.rotation = Quaternion.Euler(45, -30, 0);
                sun.shadows = LightShadows.Soft;
            }
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.65f, 0.7f, 0.75f);
            RenderSettings.fog = true; RenderSettings.fogColor = new Color(0.48f, 0.60f, 0.67f); RenderSettings.fogDensity = 0.0025f;
            var camera = Camera.main;
            if (camera == null) { var obj = new GameObject("Main Camera"); obj.tag = "MainCamera"; camera = obj.AddComponent<Camera>(); obj.AddComponent<AudioListener>(); }
            camera.fieldOfView = 65; camera.farClipPlane = 600;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.48f, 0.60f, 0.67f);
            follow = camera.GetComponent<FollowCamera>() ?? camera.gameObject.AddComponent<FollowCamera>();
            LoadStage(Mathf.Clamp(Progress.highestUnlocked, 0, stages.Length - 1));
        }
        public void LoadStage(int index)
        {
            if (index < 0 || index >= stages.Length) return;
            Time.timeScale = 1; Paused = false;
            StageIndex = index; elapsed = sampleTimer = recoveryCooldown = Boarding = EscapeProgress = ArrestProgress = 0;
            Remaining = Stage.timeLimit; Result = ""; State = MissionState.Briefing;
            World.Build(Stage);
            World.Player.drivingEnabled = false;
            World.Player.Damaged += OnDamage;
            follow.target = World.Player.transform;
            follow.transform.position = World.Player.transform.position + new Vector3(0, 6, -10);
            Log("stage_loaded", Stage.title);
        }
        public void Begin()
        {
            if (State != MissionState.Briefing) return;
            State = MissionState.Pickup; World.Player.drivingEnabled = true; Log("stage_start");
        }
        void Update()
        {
            if (GameInput.PausePressed && Active) TogglePause();
            if (!Active || Paused) return;
            Tick(Time.deltaTime);
            if (Active && GameInput.ResetPressed && Time.time >= recoveryCooldown)
            {
                recoveryCooldown = Time.time + 3;
                float z = Mathf.Clamp(World.Player.transform.position.z, 5, Stage.roadLength - 30);
                World.Player.Recover(new Vector3(0, 0.8f, z));
                Log("vehicle_recovered", "8 health penalty");
            }
        }
        // Explicit timestep makes mission rules testable without keyboard automation.
        public void Tick(float dt)
        {
            if (!Active || Paused || dt <= 0) return;
            elapsed += dt; Remaining = Mathf.Max(0, Remaining - dt);
            sampleTimer += dt;
            if (sampleTimer >= 1) { sampleTimer %= 1; Log("telemetry", State.ToString()); }
            var player = World.Player;
            if (player.health <= 0) { Finish(false, "Vehicle destroyed. The crew could not escape."); return; }
            if (Remaining <= 0) { Finish(false, "Time ran out."); return; }
            if (player.transform.position.y < -8) { Finish(false, "Vehicle lost."); return; }
            if (State == MissionState.Pickup)
            {
                Boarding = FlatDistance(player.transform.position, World.Pickup) < 6 && player.Speed < 1.5f ? Boarding + dt : 0;
                if (Boarding >= 2)
                {
                    World.BoardCrew(); World.SpawnPolice(Stage); State = MissionState.Chase;
                    Log("crew_boarded", Stage.crewCount.ToString()); Log("pursuit_started");
                }
                return;
            }
            NearestPolice = float.PositiveInfinity;
            foreach (var cop in World.Police)
                if (cop != null && cop.GetComponent<ArcadeCar>().health > 0)
                    NearestPolice = Mathf.Min(NearestPolice, FlatDistance(player.transform.position, cop.transform.position));
            if (State == MissionState.Chase)
            {
                EscapeProgress = NearestPolice > Stage.escapeDistance ? EscapeProgress + dt : 0;
                ArrestProgress = NearestPolice < 7 && player.Speed < 2 ? ArrestProgress + dt : Mathf.Max(0, ArrestProgress - dt * 2);
                if (ArrestProgress >= 4) { Finish(false, "Busted. Police surrounded the vehicle."); return; }
                if (EscapeProgress >= Stage.escapeSeconds)
                {
                    State = MissionState.Escaped;
                    foreach (var cop in World.Police) cop.searching = true;
                    Log("pursuit_evaded");
                }
            }
            // Escaped is latched for this arcade prototype; further waves are a future extension.
            if (State == MissionState.Escaped && FlatDistance(player.transform.position, World.Destination) < 7 && player.Speed < 2)
                Finish(true, "Crew delivered safely.");
        }
        public static float FlatDistance(Vector3 a, Vector3 b) { a.y = b.y = 0; return Vector3.Distance(a, b); }
        void OnDamage(float amount, string source) { Log("vehicle_damage", source + ": " + amount.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)); }
        void Finish(bool won, string reason)
        {
            State = won ? MissionState.Won : MissionState.Lost; Result = reason;
            World.Player.drivingEnabled = false;
            foreach (var cop in World.Police) cop.GetComponent<ArcadeCar>().drivingEnabled = false;
            if (won)
            {
                Progress.highestUnlocked = Mathf.Max(Progress.highestUnlocked, Mathf.Min(StageIndex + 1, stages.Length - 1));
                Progress.completedRuns++;
                bool saved = ProgressStore.Save(Progress); Log(saved ? "progress_saved" : "progress_save_failed");
            }
            Log(won ? "stage_won" : "stage_lost", reason);
        }
        public void TogglePause()
        {
            if (!Active) return;
            Paused = !Paused; Time.timeScale = Paused ? 0 : 1; Log(Paused ? "paused" : "resumed");
        }
        public void Log(string type, string detail = "") { Logger.Write(type, StageIndex + 1, elapsed, World.Player, detail); }
        void OnDestroy() { Time.timeScale = 1; }
    }
}
