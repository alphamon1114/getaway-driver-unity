using UnityEngine;

namespace Getaway
{
    public enum MissionState { Briefing, Pickup, Chase, Won, Lost }

    [RequireComponent(typeof(WorldBuilder), typeof(RunLogger))]
    public sealed class GameSession : MonoBehaviour
    {
        public StageDefinition[] stages;
        public MissionState State { get; private set; }
        public int StageIndex { get; private set; }
        public bool Paused { get; private set; }
        public float Remaining { get; private set; }
        public float Boarding => Appointment == null ? 0 : (float)Appointment.Boarding;
        public float ArrestProgress { get; private set; }
        public float NearestPolice { get; private set; }
        public string Result { get; private set; }
        public WorldBuilder World { get; private set; }
        public RunLogger Logger { get; private set; }
        public StageDefinition Stage => stages[StageIndex];
        public bool Active => State == MissionState.Pickup || State == MissionState.Chase;
        /// <summary>Radius of the city-limits zone that ends a successful run.</summary>
        public const float EscapeRadius = 10;
        public CareerAccount Account { get; private set; }
        public CareerData Progress => Account.Data;
        public PickupSchedule Appointment { get; private set; }
        public int ArrivalScore => Appointment == null ? 0 : Appointment.Score;
        public int Loot { get; private set; }
        public int LostLoot { get; private set; }
        public int BankedThisRun { get; private set; }
        public bool PendingPayout { get; private set; }
        public bool ShopOpen { get; private set; }
        public string ShopMessage { get; private set; }
        public float Elapsed => elapsed;
        string runId;
        int runSafeLevel;
        // The exit line sits off to one side, so steering the player at it from the far end of the
        // main road would read as "turn right" for the whole run. Aim at the junction until the
        // car is actually in the mouth of the escape road.
        public Vector3 Objective
        {
            get
            {
                if (World == null || World.Player == null) return Vector3.zero;
                if (State == MissionState.Pickup) return World.Pickup;
                float z = World.Player.transform.position.z;
                return z < World.JunctionZ - WorldBuilder.JunctionHalfWidth
                    ? new Vector3(0, World.Destination.y, World.JunctionZ)
                    : World.Destination;
            }
        }
        float elapsed, sampleTimer, recoveryCooldown;
        FollowCamera follow;

        void Start()
        {
            World = GetComponent<WorldBuilder>(); Logger = GetComponent<RunLogger>();
            Account = new CareerAccount(ProgressStore.Load(), ProgressStore.Save);
            ShopMessage = ProgressStore.Status;
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
            if (index < 0 || index >= stages.Length || PendingPayout) return;
            Time.timeScale = 1; Paused = false;
            StageIndex = index; elapsed = sampleTimer = recoveryCooldown = ArrestProgress = 0;
            Remaining = Stage.timeLimit; Result = ""; State = MissionState.Briefing;
            ShopOpen = false; Loot = LostLoot = BankedThisRun = 0;
            Appointment = new PickupSchedule(Stage.bankDeadline, Stage.arrivalParTime, Stage.maxArrivalScore);
            runId = System.Guid.NewGuid().ToString("N");
            World.Build(Stage, Account.Selected);
            World.Player.drivingEnabled = false;
            World.Player.Damaged += OnDamage;
            World.Player.PoliceImpact += OnPoliceImpact;
            follow.target = World.Player.transform;
            follow.transform.position = World.Player.transform.position + new Vector3(0, 6, -10);
            Log("stage_loaded", Stage.title);
        }
        public void Begin()
        {
            if (State != MissionState.Briefing || ShopOpen) return;
            runSafeLevel = Account.Selected.safe;
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
                bool arrived = Appointment.ArrivalTime >= 0;
                bool stopped = FlatDistance(player.transform.position, World.Pickup) < 6 && player.Speed < 1.5f;
                Appointment.Advance(dt, stopped);
                if (!arrived && Appointment.ArrivalTime >= 0) Log("bank_arrival", $"time={Appointment.ArrivalTime:F2}; score={ArrivalScore}");
                if (Appointment.Missed) { Finish(false, "Too slow. The crew was arrested at the bank."); return; }
                if (Appointment.Boarded)
                {
                    World.BoardCrew(); World.SpawnPolice(Stage); State = MissionState.Chase;
                    Loot = Mathf.Max(0, Stage.startingLoot);
                    Log("crew_boarded", Stage.crewCount.ToString()); Log("pursuit_started");
                }
                return;
            }
            NearestPolice = float.PositiveInfinity;
            foreach (var cop in World.Police)
                if (cop != null && cop.GetComponent<ArcadeCar>().health > 0)
                    NearestPolice = Mathf.Min(NearestPolice, FlatDistance(player.transform.position, cop.transform.position));
            // The patrols never give up. Getting out is about reaching the city limits intact,
            // so crossing the line at speed counts: no stopping next to a police car required.
            ArrestProgress = NearestPolice < 7 && player.Speed < 2 ? ArrestProgress + dt : Mathf.Max(0, ArrestProgress - dt * 2);
            if (ArrestProgress >= 4) { Finish(false, "Busted. Police boxed the vehicle in."); return; }
            if (FlatDistance(player.transform.position, World.Destination) < EscapeRadius)
                Finish(true, "Out of the city. The crew is clear.");
        }
        public static float FlatDistance(Vector3 a, Vector3 b) { a.y = b.y = 0; return Vector3.Distance(a, b); }
        void OnDamage(float amount, string source) { Log("vehicle_damage", source + ": " + amount.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)); }
        void OnPoliceImpact(float actualDamage)
        {
            if (State != MissionState.Chase) return;
            int loss = GarageCatalog.CashLoss(Loot, actualDamage, Stage.cashLossPerDamage, runSafeLevel);
            Loot -= loss; LostLoot += loss;
            Log("police_cash_loss", $"damage={actualDamage:F2}; loss={loss}; remaining={Loot}");
        }
        void Finish(bool won, string reason)
        {
            State = won ? MissionState.Won : MissionState.Lost; Result = reason;
            World.Player.drivingEnabled = false;
            foreach (var cop in World.Police) cop.GetComponent<ArcadeCar>().drivingEnabled = false;
            if (won)
            {
                PendingPayout = true;
                RetryPayout();
            }
            Log(won ? "stage_won" : "stage_lost", reason);
        }
        public void RetryPayout()
        {
            if (State != MissionState.Won || !PendingPayout) return;
            long previousWallet = Progress.wallet;
            bool saved = Account.Settle(runId, Loot, ArrivalScore, Mathf.Min(StageIndex + 1, stages.Length - 1), out string message);
            PendingPayout = !saved;
            if (saved) { BankedThisRun = (int)(Progress.wallet - previousWallet); Result = "Crew delivered. Earnings saved."; }
            else Result = "Could not save earnings. Keep this screen open and retry saving.";
            ShopMessage = message;
            Log(saved ? "payout_saved" : "payout_save_failed", $"run={runId}; loot={Loot}; banked={BankedThisRun}; wallet={Progress.wallet}; score={ArrivalScore}");
        }
        public void OpenShop()
        {
            if (Active || PendingPayout) return;
            ShopOpen = true; ShopMessage = ProgressStore.ReadOnly ? ProgressStore.Status : "Upgrades apply to the selected car. Purchases are saved immediately.";
        }
        public void CloseShop()
        {
            ShopOpen = false;
            if (State == MissionState.Briefing) LoadStage(StageIndex);
        }
        public void BuyVehicle(string id)
        {
            if (!ShopOpen || Active || PendingPayout) return;
            bool ok = Account.BuyVehicle(id, out string message); ShopMessage = message;
            Log(ok ? "vehicle_purchased" : "purchase_rejected", $"vehicle={id}; wallet={Progress.wallet}; {message}");
        }
        public void SelectVehicle(string id)
        {
            if (!ShopOpen || Active || PendingPayout) return;
            bool ok = Account.SelectVehicle(id, out string message); ShopMessage = message;
            Log(ok ? "vehicle_selected" : "selection_rejected", id);
        }
        public void BuyUpgrade(UpgradeKind kind)
        {
            if (!ShopOpen || Active || PendingPayout) return;
            bool ok = Account.BuyUpgrade(kind, out string message); ShopMessage = message;
            Log(ok ? "upgrade_purchased" : "purchase_rejected", $"vehicle={Progress.selectedVehicle}; upgrade={kind}; wallet={Progress.wallet}; {message}");
        }
        public void TogglePause()
        {
            if (!Active) return;
            Paused = !Paused; Time.timeScale = Paused ? 0 : 1; Log(Paused ? "paused" : "resumed");
        }
        public void Log(string type, string detail = "") { Logger.Write(type, StageIndex + 1, elapsed, World.Player, detail, runId, Loot, Progress.wallet, ArrivalScore); }
        void OnDestroy() { Time.timeScale = 1; }
    }
}
