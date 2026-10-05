using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LethalMenu.Menu
{
    public partial class HackMenu
    {
        #region Settings Tab

        private bool _isCapturingKeyBind;
        private Hack _capturingKeyBind;
        private string _keybindSearch = "";
        private bool _showChamColors;

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

            DrawSection("Keybinds", DrawKeybindSettings);

            DrawSection("Colors", () =>
            {
                DrawColorSetting("Crosshair", Settings.CrosshairColor, color => Settings.CrosshairColor = color);
                DrawColorSetting("Players", Settings.PlayerColor, color => Settings.PlayerColor = color);
                DrawColorSetting("Enemies", Settings.EnemyColor, color => Settings.EnemyColor = color);
                DrawColorSetting("Items", Settings.ItemColor, color => Settings.ItemColor = color);
                DrawColorSetting("Doors", Settings.DoorColor, color => Settings.DoorColor = color);
                DrawColorSetting("Landmines", Settings.MineColor, color => Settings.MineColor = color);
                DrawColorSetting("Turrets", Settings.TurretColor, color => Settings.TurretColor = color);
                DrawColorSetting("Breaker Box", Settings.FuseboxColor, color => Settings.FuseboxColor = color);

                GUILayout.Space(4);
                _showChamColors = GUILayout.Toggle(_showChamColors, "Cham colors", _buttonStyle);
                if (_showChamColors)
                    DrawChamColors();
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
                    ApplySettingsToMenu();
                }
                GUILayout.EndHorizontal();

                if (GUILayout.Button("Reset to Defaults", _buttonStyle, GUILayout.Height(28)))
                {
                    Settings.ResetConfig();
                    ApplySettingsToMenu();
                }

                GUILayout.Label(
                    "Config saves to: %APPDATA%\\LethalMenu\\config.json",
                    new GUIStyle(_labelStyle) { fontSize = 10, normal = { textColor = Color.gray } }
                );
            });
        }

        private void DrawKeybindSettings()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Search:", _labelStyle, GUILayout.Width(60));
            _keybindSearch = GUILayout.TextField(_keybindSearch ?? "", GUILayout.Width(180));
            if (GUILayout.Button("Clear All", _buttonStyle, GUILayout.Width(80)))
                HackExtensions.ClearKeyBinds();
            GUILayout.EndHorizontal();

            if (_isCapturingKeyBind)
            {
                GUILayout.Label(
                    $"Binding {_capturingKeyBind.GetDisplayName()}: press a key, Backspace clears, Esc cancels",
                    new GUIStyle(_labelStyle) { normal = { textColor = Color.yellow } });

                var pressedKey = HackExtensions.GetPressedKeyboardKey();
                if (pressedKey != null)
                {
                    if (pressedKey.keyCode == Key.Escape)
                    {
                        _isCapturingKeyBind = false;
                    }
                    else if (pressedKey.keyCode == Key.Backspace || pressedKey.keyCode == Key.Delete)
                    {
                        _capturingKeyBind.SetKeyBind((UnityEngine.InputSystem.Controls.ButtonControl?)null);
                        _isCapturingKeyBind = false;
                    }
                    else
                    {
                        _capturingKeyBind.SetKeyBind(pressedKey);
                        _isCapturingKeyBind = false;
                    }
                }
            }

            GUILayout.Space(4);

            foreach (Hack hack in Enum.GetValues(typeof(Hack)))
            {
                string displayName = hack.GetDisplayName();
                if (!string.IsNullOrWhiteSpace(_keybindSearch) &&
                    displayName.IndexOf(_keybindSearch, StringComparison.OrdinalIgnoreCase) < 0 &&
                    hack.ToString().IndexOf(_keybindSearch, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                GUILayout.BeginHorizontal();
                GUILayout.Label(displayName, _labelStyle, GUILayout.Width(180));
                GUILayout.Label(hack.GetKeyBindDisplayName(), _labelStyle, GUILayout.Width(90));
                if (GUILayout.Button(_isCapturingKeyBind && _capturingKeyBind == hack ? "Listening" : "Bind", _buttonStyle, GUILayout.Width(70)))
                {
                    _capturingKeyBind = hack;
                    _isCapturingKeyBind = true;
                }
                if (GUILayout.Button("Clear", _buttonStyle, GUILayout.Width(60)))
                    hack.SetKeyBind((UnityEngine.InputSystem.Controls.ButtonControl?)null);
                GUILayout.EndHorizontal();
            }
        }

        private void DrawColorSetting(string label, Color value, Action<Color> assign)
        {
            GUILayout.BeginVertical(_boxStyle);

            GUILayout.BeginHorizontal();
            var oldColor = GUI.color;
            GUI.color = value;
            GUILayout.Box("", GUILayout.Width(22), GUILayout.Height(18));
            GUI.color = oldColor;
            GUILayout.Label($"{label}: #{ColorUtility.ToHtmlStringRGBA(value)}", _labelStyle, GUILayout.Width(180));
            GUILayout.EndHorizontal();

            value.r = DrawColorChannel("R", value.r);
            value.g = DrawColorChannel("G", value.g);
            value.b = DrawColorChannel("B", value.b);
            value.a = DrawColorChannel("A", value.a);

            assign(value);
            GUILayout.EndVertical();
        }

        private void DrawChamColors()
        {
            GUILayout.Label("Single applies when \"One colour for everything\" is on (Visuals > Chams).", _tooltipStyle);
            DrawCompactColor("Single", Settings.ChamColor, c => Settings.ChamColor = c);
            DrawCompactColor("Players", Settings.PlayerChamColor, c => Settings.PlayerChamColor = c);
            DrawCompactColor("Enemies", Settings.EnemyChamColor, c => Settings.EnemyChamColor = c);
            DrawCompactColor("Items", Settings.ItemChamColor, c => Settings.ItemChamColor = c);
            DrawCompactColor("Landmines", Settings.LandmineChamColor, c => Settings.LandmineChamColor = c);
            DrawCompactColor("Turrets", Settings.TurretChamColor, c => Settings.TurretChamColor = c);
            DrawCompactColor("Doors", Settings.DoorChamColor, c => Settings.DoorChamColor = c);
            DrawCompactColor("Big Doors", Settings.BigDoorChamColor, c => Settings.BigDoorChamColor = c);
            DrawCompactColor("Ship Door", Settings.ShipDoorChamColor, c => Settings.ShipDoorChamColor = c);
            DrawCompactColor("Breaker Box", Settings.BreakerChamColor, c => Settings.BreakerChamColor = c);
            DrawCompactColor("Enemy Vents", Settings.EnemyVentChamColor, c => Settings.EnemyVentChamColor = c);
            DrawCompactColor("Item Dropship", Settings.ItemDropshipChamColor, c => Settings.ItemDropshipChamColor = c);
            DrawCompactColor("Cruiser", Settings.CruiserChamColor, c => Settings.CruiserChamColor = c);
            DrawCompactColor("Mold Spores", Settings.MoldSporeChamColor, c => Settings.MoldSporeChamColor = c);
            DrawCompactColor("Mineshaft Elevator", Settings.MineshaftElevatorChamColor, c => Settings.MineshaftElevatorChamColor = c);
            DrawCompactColor("Entrances", Settings.EntranceChamColor, c => Settings.EntranceChamColor = c);
            DrawCompactColor("Spike Traps", Settings.SpikeRoofTrapChamColor, c => Settings.SpikeRoofTrapChamColor = c);
            DrawCompactColor("Steam Valves", Settings.SteamValveChamColor, c => Settings.SteamValveChamColor = c);
        }

        /// One row per color: swatch, name, and R/G/B/A sliders.
        private void DrawCompactColor(string label, Color value, Action<Color> assign)
        {
            GUILayout.BeginHorizontal();
            var oldColor = GUI.color;
            GUI.color = value;
            GUILayout.Box("", GUILayout.Width(18), GUILayout.Height(16));
            GUI.color = oldColor;
            GUILayout.Label(label, _labelStyle, GUILayout.Width(130));
            value.r = GUILayout.HorizontalSlider(value.r, 0f, 1f, GUILayout.Width(45));
            value.g = GUILayout.HorizontalSlider(value.g, 0f, 1f, GUILayout.Width(45));
            value.b = GUILayout.HorizontalSlider(value.b, 0f, 1f, GUILayout.Width(45));
            value.a = GUILayout.HorizontalSlider(value.a, 0f, 1f, GUILayout.Width(45));
            GUILayout.EndHorizontal();
            assign(value);
        }

        private float DrawColorChannel(string label, float value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{label}: {Mathf.RoundToInt(value * 255f)}", _labelStyle, GUILayout.Width(55));
            value = GUILayout.HorizontalSlider(value, 0f, 1f, GUILayout.Width(180));
            GUILayout.EndHorizontal();
            return Mathf.Clamp01(value);
        }

        #endregion
    }
}
