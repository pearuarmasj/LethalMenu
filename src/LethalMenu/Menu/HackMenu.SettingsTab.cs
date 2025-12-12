using UnityEngine;

namespace LethalMenu.Menu
{
    public partial class HackMenu
    {
        #region Settings Tab

        private void DrawSettingsTab()
        {
            DrawSection("Menu Settings", () =>
            {
                GUILayout.Label("Press INSERT to toggle menu", _labelStyle);
                GUILayout.Space(10);
                var timeOfDay = TimeOfDay.Instance;
                GUILayout.Label($"Game Time: {(timeOfDay?.normalizedTimeOfDay ?? 0):P0}", _labelStyle);
                GUILayout.Label($"Players: {LethalMenuMod.Players.Count}", _labelStyle);
                GUILayout.Label($"Local Player: {LethalMenuMod.LocalPlayer?.playerUsername ?? "None"}", _labelStyle);
            });

            DrawSection("Config", () =>
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Save Config", _buttonStyle, GUILayout.Height(28)))
                {
                    Settings.SaveConfig();
                }
                if (GUILayout.Button("Load Config", _buttonStyle, GUILayout.Height(28)))
                {
                    Settings.LoadConfig();
                }
                GUILayout.EndHorizontal();

                if (GUILayout.Button("Reset to Defaults", _buttonStyle, GUILayout.Height(28)))
                {
                    Settings.ResetConfig();
                }

                GUILayout.Label(
                    "Config saves to: %APPDATA%\\LethalMenu\\config.json",
                    new GUIStyle(_labelStyle) { fontSize = 10, normal = { textColor = Color.gray } }
                );
            });

            DrawSection("Debug", () =>
            {
                if (GUILayout.Button("Log Game State", _buttonStyle, GUILayout.Height(28)))
                {
                    Loader.Log($"GameInstance: {LethalMenuMod.GameInstance != null}");
                    Loader.Log($"LocalPlayer: {LethalMenuMod.LocalPlayer?.playerUsername}");
                    Loader.Log($"Players: {LethalMenuMod.Players.Count}");
                    Loader.Log($"Enemies: {LethalMenuMod.Enemies.Count}");
                    Loader.Log($"Items: {LethalMenuMod.Items.Count}");
                }
            });
        }

        #endregion
    }
}
