using UnityEngine;

namespace LethalMenu.Menu
{
    public partial class HackMenu
    {
        #region Settings Tab

        private void DrawSettingsTab()
        {
            DrawSection("Theme", () =>
            {
                var themes = Theme.ThemeLoader.GetAvailableThemes();
                GUILayout.BeginHorizontal();
                GUILayout.Label("Theme:", _labelStyle, GUILayout.Width(60));
                foreach (var theme in themes)
                {
                    var style = (theme == Theme.ThemeLoader.CurrentName) ? _selectedTabStyle : _buttonStyle;
                    if (GUILayout.Button(theme, style, GUILayout.Height(24)))
                    {
                        Theme.ThemeLoader.SetTheme(theme);
                        Settings.ThemeName = theme;
                        _stylesInitialized = false;
                    }
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label($"Opacity: {Settings.MenuAlpha:F1}", _labelStyle, GUILayout.Width(80));
                Settings.MenuAlpha = GUILayout.HorizontalSlider(Settings.MenuAlpha, 0.1f, 1f, GUILayout.Width(200));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label($"Font: {Settings.MenuFontSize}", _labelStyle, GUILayout.Width(80));
                Settings.MenuFontSize = (int)GUILayout.HorizontalSlider(Settings.MenuFontSize, 8, 24, GUILayout.Width(200));
                GUILayout.EndHorizontal();

                Settings.HackHighlight = GUILayout.Toggle(Settings.HackHighlight, "Highlight active hacks", _toggleStyle);

                GUILayout.BeginHorizontal();
                GUILayout.Label($"Slider: {Settings.SliderWidth}", _labelStyle, GUILayout.Width(80));
                Settings.SliderWidth = (int)GUILayout.HorizontalSlider(Settings.SliderWidth, 50, 120, GUILayout.Width(200));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label($"Textbox: {Settings.TextboxWidth}", _labelStyle, GUILayout.Width(80));
                Settings.TextboxWidth = (int)GUILayout.HorizontalSlider(Settings.TextboxWidth, 50, 120, GUILayout.Width(200));
                GUILayout.EndHorizontal();
            });

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
