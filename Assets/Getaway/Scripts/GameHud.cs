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
                title = new GUIStyle(GUI.skin.label) { fontSize = 34, fontStyle = FontStyle.Bold };
                title.normal.textColor = new Color(1, 0.74f, 0.34f);
                text = new GUIStyle(GUI.skin.label) { fontSize = 20, wordWrap = true };
                small = new GUIStyle(text) { fontSize = 16 };
                button = new GUIStyle(GUI.skin.button) { fontSize = 20, fixedHeight = 44 };
            }
            Matrix4x4 previous = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(Screen.width / 1280f, Screen.height / 720f, 1));
            Panel(new Rect(20, 20, 1240, 115));
            GUI.Label(new Rect(40, 30, 600, 48), "GETAWAY / " + (game.StageIndex + 1).ToString("00"), title);
            GUI.Label(new Rect(40, 83, 640, 35), game.Stage.title + "  /  " + game.State.ToString().ToUpperInvariant(), small);
            GUI.Label(new Rect(720, 35, 500, 35), $"{game.World.Player.Kph:000} KM/H     HULL {game.World.Player.health:000}%     {game.Remaining:000}s", text);
            GUI.Label(new Rect(720, 80, 500, 35), "WASD / Arrows  •  Space brake  •  R recover  •  Esc pause", small);
            if (game.Active && !game.Paused)
            {
                Panel(new Rect(20, 585, 950, 115));
                string objective = game.State == MissionState.Pickup ? "Stop in the BLUE zone to collect the crew (2 seconds)." : game.State == MissionState.Chase ? $"Lose the police: stay {game.Stage.escapeDistance:0}m ahead for {game.Stage.escapeSeconds:0}s." : "Police evaded. Stop in the GREEN safehouse zone.";
                GUI.Label(new Rect(40, 600, 910, 35), objective, text);
                string progress = game.State == MissionState.Pickup ? $"Boarding {Mathf.Min(2, game.Boarding):0.0} / 2s" : $"Escape {game.EscapeProgress:0.0} / {game.Stage.escapeSeconds:0}s   Arrest {game.ArrestProgress:0.0} / 4s";
                GUI.Label(new Rect(40, 643, 910, 35), progress + $"     Target: {GameSession.FlatDistance(game.World.Player.transform.position, game.Objective):0}m", small);
                DrawMarker();
            }
            if (game.State == MissionState.Briefing || game.State == MissionState.Won || game.State == MissionState.Lost || game.Paused)
            {
                Panel(new Rect(330, 185, 620, 370));
                GUILayout.BeginArea(new Rect(355, 205, 570, 330));
                GUILayout.Label(game.Paused ? "PAUSED" : game.State == MissionState.Briefing ? game.Stage.title : game.State == MissionState.Won ? "JOB COMPLETE" : "JOB FAILED", title);
                GUILayout.Space(12);
                GUILayout.Label(game.State == MissionState.Briefing ? game.Stage.briefing : game.Paused ? "The crew is waiting." : game.Result, text);
                GUILayout.Space(15);
                if (game.Paused) { if (GUILayout.Button("Resume", button)) game.TogglePause(); }
                else if (game.State == MissionState.Briefing) { if (GUILayout.Button("Start getaway", button)) game.Begin(); }
                else
                {
                    if (game.State == MissionState.Won && game.StageIndex + 1 < game.stages.Length)
                        if (GUILayout.Button("Next stage", button)) game.LoadStage(game.StageIndex + 1);
                    if (GUILayout.Button("Retry stage", button)) game.LoadStage(game.StageIndex);
                }
                if (GUILayout.Button("Play from stage 1", button)) game.LoadStage(0);
                GUILayout.EndArea();
            }
            GUI.matrix = previous;
        }
        void DrawMarker()
        {
            Vector3 local = game.World.Player.transform.InverseTransformPoint(game.Objective);
            string direction = local.z < 0 ? "TURN BACK" : Mathf.Abs(local.x) < 5 ? "AHEAD" : local.x < 0 ? "LEFT" : "RIGHT";
            Panel(new Rect(1010, 600, 250, 100));
            GUI.Label(new Rect(1030, 625, 220, 50), direction, title);
        }
        static void Panel(Rect rect)
        {
            Color before = GUI.color;
            GUI.color = new Color(0.025f, 0.04f, 0.065f, 0.94f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = before;
        }
    }
}
