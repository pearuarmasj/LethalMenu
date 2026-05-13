using UnityEngine;

namespace LethalMenu.Menu.Popup
{
    public class WeatherManagerPopup : PopupMenu
    {
        private int _selectedLevelIndex;
        private LevelWeatherType _selectedWeather;

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

            _selectedLevelIndex = Mathf.Clamp(_selectedLevelIndex, 0, levels.Length - 1);
            var selectedLevel = levels[_selectedLevelIndex];

            GUILayout.BeginHorizontal();
            GUILayout.Label("Moon:", GUILayout.Width(60));
            if (GUILayout.Button("<", GUILayout.Width(30)))
                _selectedLevelIndex = (_selectedLevelIndex - 1 + levels.Length) % levels.Length;
            GUILayout.Label(selectedLevel?.PlanetName ?? "Unknown", GUILayout.Width(160));
            if (GUILayout.Button(">", GUILayout.Width(30)))
                _selectedLevelIndex = (_selectedLevelIndex + 1) % levels.Length;
            GUILayout.EndHorizontal();

            var weatherValues = (LevelWeatherType[])System.Enum.GetValues(typeof(LevelWeatherType));
            int currentWeatherIndex = System.Array.IndexOf(weatherValues, _selectedWeather);
            if (currentWeatherIndex < 0)
            {
                _selectedWeather = selectedLevel?.currentWeather ?? LevelWeatherType.None;
                currentWeatherIndex = System.Array.IndexOf(weatherValues, _selectedWeather);
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("Weather:", GUILayout.Width(60));
            if (GUILayout.Button("<", GUILayout.Width(30)))
                _selectedWeather = weatherValues[(currentWeatherIndex - 1 + weatherValues.Length) % weatherValues.Length];
            GUILayout.Label(_selectedWeather.ToString(), GUILayout.Width(160));
            if (GUILayout.Button(">", GUILayout.Width(30)))
                _selectedWeather = weatherValues[(currentWeatherIndex + 1) % weatherValues.Length];
            GUILayout.EndHorizontal();

            if (selectedLevel != null && GUILayout.Button("Apply Weather", GUILayout.Height(28)))
            {
                selectedLevel.currentWeather = _selectedWeather;
                HUDManager.Instance?.DisplayTip("Weather", $"{selectedLevel.PlanetName}: {_selectedWeather}");
            }

            GUILayout.Space(10);
            foreach (var level in levels)
            {
                if (level == null) continue;
                GUILayout.Label($"  {level.PlanetName}: {level.currentWeather}");
            }
        }
    }
}
