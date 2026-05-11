using UnityEngine;

namespace LethalMenu.Menu.Popup
{
    public class WeatherManagerPopup : PopupMenu
    {
        public WeatherManagerPopup() : base("Weather Manager", 20003, 350, 300) { }

        protected override void DrawBody()
        {
            var instance = StartOfRound.Instance;
            if (instance == null)
            {
                GUILayout.Label("Not in game");
                return;
            }

            GUILayout.Label($"Current Moon: {instance.currentLevel?.PlanetName ?? "Unknown"}");
            GUILayout.Label($"Current Weather: {instance.currentLevel?.currentWeather}");
            GUILayout.Space(10);

            GUILayout.Label("--- All Moons Weather ---");
            var levels = instance.levels;
            if (levels == null) return;

            foreach (var level in levels)
            {
                if (level == null) continue;
                GUILayout.Label($"  {level.PlanetName}: {level.currentWeather}");
            }
        }
    }
}
