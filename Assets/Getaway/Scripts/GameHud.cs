using UnityEngine;

namespace Getaway
{
    [RequireComponent(typeof(GameSession))]
    public sealed class GameHud : MonoBehaviour
    {
        GameSession game;
        GUIStyle title, text, small, button;
        void Awake() { game = GetComponent<GameSession>(); }
        void OnGUI()
        {
            if (game.World == null || game.World.Player == null) return;
            if (title == null)
            {
                title = new GUIStyle(GUI.skin.label) { fontSize = 30, fontStyle = FontStyle.Bold };
                title.normal.textColor = new Color(1, 0.74f, 0.34f);
                text = new GUIStyle(GUI.skin.label) { fontSize = 20, wordWrap = true };
                small = new GUIStyle(text) { fontSize = 16 };
                button = new GUIStyle(GUI.skin.button) { fontSize = 17, wordWrap = true };
            }
            Matrix4x4 previous = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(Screen.width / 1280f, Screen.height / 720f, 1));
            Panel(new Rect(20, 15, 1240, 135));
            Label(40, 23, 610, 40, "GETAWAY / " + (game.StageIndex + 1).ToString("00") + "  " + game.State.ToString().ToUpperInvariant(), title);
            Label(40, 67, 650, 30, game.Stage.title + " / " + GarageCatalog.Find(game.Progress.selectedVehicle).Name, text);
            Label(40, 108, 660, 30, "WASD drive   Space brake/drift   R recover (-8 hull)   Esc pause", small);
            Label(710, 28, 530, 30, $"WALLET ${game.Progress.wallet:N0}     CARRIED ${game.Loot:N0}", text);
            float hull = game.World.Player.health / Mathf.Max(1, game.World.Player.maxHealth) * 100;
            Label(710, 69, 530, 30, $"{game.World.Player.Kph:000} KM/H    HULL {hull:000}%    {game.Remaining:000}s", text);
            Label(710, 110, 530, 25, $"Arrival score: {game.ArrivalScore}    Cash lost: ${game.LostLoot:N0}", small);

            if (game.ShopOpen) DrawShop();
            else if (game.Active && !game.Paused) DrawMission();
            else DrawMenu();
            GUI.matrix = previous;
        }
        void DrawMission()
        {
            Panel(new Rect(20, 555, 970, 145));
            if (game.State == MissionState.Pickup)
            {
                var a = game.Appointment;
                Label(40, 568, 930, 30, $"Beat the roadblocks to the BLUE bank zone and board:  {a.Remaining:0.0}s left of {a.Deadline:0}s", text);
                string score = a.ArrivalTime < 0
                    ? $"Arrive by {a.ParTime:0}s for the full {a.MaxScore} points. Every second after that is worth less."
                    : $"Arrived {a.ArrivalTime:0.00}s  —  {a.Score} / {a.MaxScore} points (locked)";
                Label(40, 609, 930, 30, score, small);
                Label(40, 650, 930, 30, $"Hold still in the zone to load the crew: {game.Boarding:0.0} / 2.0s", small);
            }
            else
            {
                Label(40, 568, 930, 60, "Run for the GREEN city limits on the exit road. Reach it with the car intact and the cash is yours.", text);
                Label(40, 630, 930, 50, $"Nearest patrol {(float.IsInfinity(game.NearestPolice) ? 0 : game.NearestPolice):0}m   Boxed in {game.ArrestProgress:0.0} / 4s   Every police hit costs money.", small);
            }
            Vector3 local = game.World.Player.transform.InverseTransformPoint(game.Objective);
            string direction = local.z < 0 ? "TURN BACK" : Mathf.Abs(local.x) < 5 ? "AHEAD" : local.x < 0 ? "LEFT" : "RIGHT";
            Panel(new Rect(1010, 555, 250, 145));
            Label(1030, 578, 220, 42, direction, title);
            Label(1030, 635, 220, 40, $"{GameSession.FlatDistance(game.World.Player.transform.position, game.Objective):0} m to target", text);
        }
        void DrawMenu()
        {
            Panel(new Rect(290, 172, 700, 515));
            string heading = game.Paused ? "PAUSED" : game.State == MissionState.Briefing ? "BANK APPOINTMENT" : game.State == MissionState.Won ? "JOB COMPLETE" : "JOB FAILED";
            Label(320, 190, 640, 45, heading, title);
            if (game.Paused)
            {
                Label(320, 255, 640, 60, "Mission clock is paused.", text);
                if (Button(320, 350, 640, 44, "Resume")) game.TogglePause();
                return;
            }
            if (game.State == MissionState.Briefing)
            {
                Label(320, 242, 640, 60, game.Stage.briefing, text);
                Label(320, 315, 640, 62, $"Board at the bank within {game.Stage.bankDeadline:0}s; under {game.Stage.arrivalParTime:0}s pays the full {game.Stage.maxArrivalScore} points.\nHaul ${game.Stage.startingLoot:N0}, then reach the city limits before the patrols wreck you.", small);
                if (Button(320, 390, 640, 44, "Start job")) game.Begin();
            }
            else
            {
                Label(320, 242, 640, 70, game.Result, text);
                Label(320, 315, 640, 60, $"Timing score {game.ArrivalScore}   |   Lost ${game.LostLoot:N0}\nBanked ${game.BankedThisRun:N0}   |   Wallet ${game.Progress.wallet:N0}", text);
                if (game.PendingPayout)
                {
                    Label(320, 388, 640, 55, "Your reward is pending. Retry saving before leaving this result.", small);
                    if (Button(320, 460, 640, 44, "Retry saving earnings")) game.RetryPayout();
                    return;
                }
                if (Button(320, 390, 310, 44, "Retry job")) game.LoadStage(game.StageIndex);
                if (game.State == MissionState.Won && game.StageIndex + 1 < game.stages.Length)
                    if (Button(650, 390, 310, 44, "Next stage")) game.LoadStage(game.StageIndex + 1);
            }
            if (Button(320, 450, 640, 44, "Garage / Shop")) game.OpenShop();
            Label(320, 510, 640, 28, "REPLAY UNLOCKED STAGES", small);
            float width = Mathf.Min(200, 640f / game.stages.Length - 8);
            for (int i = 0; i < game.stages.Length; i++)
            {
                bool wasEnabled = GUI.enabled; GUI.enabled = i <= game.Progress.highestUnlocked;
                if (Button(320 + i * (width + 8), 546, width, 42, $"Stage {i + 1}")) game.LoadStage(i);
                GUI.enabled = wasEnabled;
            }
            Label(320, 603, 640, 60, $"Best timing: {game.Progress.bestArrivalScore}  |  Successful jobs: {game.Progress.completedRuns}\nOnly a successful getaway adds money to your wallet.", small);
        }
        void DrawShop()
        {
            Panel(new Rect(150, 165, 980, 535));
            Label(175, 178, 750, 42, "GARAGE / SHOP", title);
            if (Button(955, 179, 150, 38, "Back to job")) { game.CloseShop(); return; }
            Label(175, 222, 925, 28, $"Wallet ${game.Progress.wallet:N0}  /  Equipped: {GarageCatalog.Find(game.Progress.selectedVehicle).Name}", text);
            int i = 0;
            foreach (var spec in GarageCatalog.Vehicles)
            {
                float y = 259 + i++ * 49;
                bool owned = game.Progress.vehicles.Exists(v => v.id == spec.Id);
                bool equipped = game.Progress.selectedVehicle == spec.Id;
                Label(175, y, 675, 45, $"{spec.Name}   {spec.Speed * 3.6f:0} km/h cap   Hull {spec.Health:0}" + (owned ? "  [OWNED]" : $"  ${spec.Price:N0}"), small);
                bool old = GUI.enabled; GUI.enabled = !equipped && !ProgressStore.ReadOnly && (owned || game.Progress.wallet >= spec.Price);
                if (Button(875, y, 230, 37, equipped ? "Equipped" : owned ? "Equip" : "Buy vehicle"))
                { if (owned) game.SelectVehicle(spec.Id); else game.BuyVehicle(spec.Id); }
                GUI.enabled = old;
            }
            Label(175, 408, 920, 25, "UPGRADE THE EQUIPPED CAR (levels are separate for each vehicle)", small);
            for (int n = 0; n < 4; n++)
            {
                var kind = (UpgradeKind)n; var selected = game.Account.Selected;
                int level = GarageCatalog.Level(selected, kind), price = GarageCatalog.UpgradePrice(selected, kind);
                float y = 439 + n * 45;
                Label(175, y, 685, 42, $"{kind} {level}/3   {GarageCatalog.UpgradeDescription(kind)}", small);
                bool old = GUI.enabled; GUI.enabled = !ProgressStore.ReadOnly && level < 3 && game.Progress.wallet >= price;
                if (Button(875, y, 230, 36, level >= 3 ? "Max level" : $"Upgrade ${price:N0}")) game.BuyUpgrade(kind);
                GUI.enabled = old;
            }
            Label(175, 628, 925, 62, game.ShopMessage, small);
        }
        void Label(float x, float y, float w, float h, string value, GUIStyle style) { GUI.Label(new Rect(x, y, w, h), value, style); }
        bool Button(float x, float y, float w, float h, string value) => GUI.Button(new Rect(x, y, w, h), value, button);
        static void Panel(Rect rect)
        {
            Color before = GUI.color; GUI.color = new Color(0.025f, 0.04f, 0.065f, 0.96f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = before;
        }
    }
}
