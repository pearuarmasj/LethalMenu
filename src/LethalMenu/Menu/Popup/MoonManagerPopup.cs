using UnityEngine;

namespace LethalMenu.Menu.Popup
{
    public class MoonManagerPopup : PopupMenu
    {
        public MoonManagerPopup() : base("Moon Manager", 20007, 450, 400) { }

        protected override void DrawBody()
        {
            var instance = StartOfRound.Instance;
            if (instance == null)
            {
                GUILayout.Label("Not in game");
                return;
            }

            GUILayout.Label($"Current: {instance.currentLevel?.PlanetName ?? "Unknown"}");
            GUILayout.Space(5);

            var levels = instance.levels;
            if (levels == null) return;

            var terminal = Object.FindObjectOfType<Terminal>();
            int credits = terminal?.groupCredits ?? 0;

            for (int i = 0; i < levels.Length; i++)
            {
                var level = levels[i];
                if (level == null) continue;

                GUILayout.BeginHorizontal();
                GUILayout.Label($"{level.PlanetName} ({level.currentWeather})", GUILayout.Width(250));
                int levelId = level.levelID;
                if (GUILayout.Button("Route", GUILayout.Width(60)))
                    instance.ChangeLevelServerRpc(levelId, credits);
                if (GUILayout.Button("Free", GUILayout.Width(50)))
                    instance.ChangeLevelServerRpc(levelId, credits);
                GUILayout.EndHorizontal();
            }
        }
    }
}
