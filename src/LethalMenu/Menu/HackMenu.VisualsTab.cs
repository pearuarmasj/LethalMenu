using UnityEngine;
using LethalMenu.Patches;

namespace LethalMenu.Menu
{
    public partial class HackMenu
    {
        #region Visuals Tab

        private void DrawVisualsTab()
        {
            DrawSection("ESP", () =>
            {
                Settings.ESP = DrawToggle("Enable ESP", Settings.ESP, "Show objects through walls");

                if (Settings.ESP)
                {
                    GUILayout.Space(5);
                    Settings.PlayerESP = DrawToggle("  Player ESP", Settings.PlayerESP);
                    if (Settings.PlayerESP)
                    {
                        Settings.PlayerHealthBars = DrawToggle("    Health Bars", Settings.PlayerHealthBars, "Show HP bars above players");
                    }
                    Settings.EnemyESP = DrawToggle("  Enemy ESP", Settings.EnemyESP);
                    Settings.ItemESP = DrawToggle("  Item ESP", Settings.ItemESP);
                    Settings.DoorESP = DrawToggle("  Door ESP", Settings.DoorESP);
                    Settings.MineESP = DrawToggle("  Mine ESP", Settings.MineESP);
                    Settings.TurretESP = DrawToggle("  Turret ESP", Settings.TurretESP);
                    Settings.FuseboxESP = DrawToggle("  Fusebox ESP", Settings.FuseboxESP);
                }
            });

            DrawSection("Camera", () =>
            {
                Settings.FreeCam = DrawToggle("FreeCam", Settings.FreeCam, "WASD+Mouse to fly around");
                if (Settings.FreeCam)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"  Speed: {Settings.FreeCamSpeed:F0}", _labelStyle, GUILayout.Width(80));
                    Settings.FreeCamSpeed = GUILayout.HorizontalSlider(Settings.FreeCamSpeed, 5f, 50f);
                    GUILayout.EndHorizontal();

                    GUILayout.Label("  Arrow L/R: Snap to players | Down: Back to you", _labelStyle);
                    if (GUILayout.Button("Teleport Me To Camera", _buttonStyle, GUILayout.Height(25)))
                    {
                        LethalMenu.Cheats.FreeCamCheat.TeleportToCameraPosition();
                    }
                    GUILayout.Label("  Or hold Shift when disabling FreeCam", _labelStyle);
                }

                // Third-person camera
                bool prevThirdPerson = Settings.ThirdPerson;
                Settings.ThirdPerson = DrawToggle("Third Person", Settings.ThirdPerson, "Press V to toggle (view from behind)");
                if (prevThirdPerson != Settings.ThirdPerson)
                {
                    LethalMenu.Cheats.ThirdPersonCheat.Toggle();
                }
                if (Settings.ThirdPerson)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"  Distance: {Settings.ThirdPersonDistance:F1}", _labelStyle, GUILayout.Width(100));
                    Settings.ThirdPersonDistance = GUILayout.HorizontalSlider(Settings.ThirdPersonDistance, 1f, 10f);
                    GUILayout.EndHorizontal();
                }

                // Spectate player
                Settings.SpectatePlayer = DrawToggle("Spectate Player", Settings.SpectatePlayer, "Watch another player");
                if (Settings.SpectatePlayer)
                {
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("<", _buttonStyle, GUILayout.Width(30)))
                    {
                        CyclePreviousSpectatePlayer();
                    }

                    string playerName = "None";
                    if (Settings.SpectatePlayerIndex >= 0 && Settings.SpectatePlayerIndex < LethalMenuMod.Players.Count)
                    {
                        var player = LethalMenuMod.Players[Settings.SpectatePlayerIndex];
                        if (player != null)
                        {
                            playerName = player.playerUsername ?? "Unknown";
                        }
                    }
                    GUILayout.Label(playerName, _labelStyle, GUILayout.Width(120));

                    if (GUILayout.Button(">", _buttonStyle, GUILayout.Width(30)))
                    {
                        CycleNextSpectatePlayer();
                    }
                    GUILayout.EndHorizontal();
                }
            });

            DrawSection("HUD/Visual", () =>
            {
                Settings.AlwaysShowClock = DrawToggle("Always Show Clock", Settings.AlwaysShowClock, "Clock always visible");
                Settings.Crosshair = DrawToggle("Crosshair", Settings.Crosshair, "Show crosshair on screen");
                Settings.HPDisplay = DrawToggle("HP Display", Settings.HPDisplay, "Show health on screen");

                // Info Display - comprehensive game info HUD
                Settings.InfoDisplay = DrawToggle("Info Display", Settings.InfoDisplay, "Show game info panel (top-right)");
                if (Settings.InfoDisplay)
                {
                    GUILayout.Label("  Display Options:", _labelStyle);
                    GUILayout.BeginHorizontal();
                    Settings.InfoDisplayCredits = GUILayout.Toggle(Settings.InfoDisplayCredits, "Credits", _buttonStyle, GUILayout.Width(70));
                    Settings.InfoDisplayQuota = GUILayout.Toggle(Settings.InfoDisplayQuota, "Quota", _buttonStyle, GUILayout.Width(60));
                    Settings.InfoDisplayDeadline = GUILayout.Toggle(Settings.InfoDisplayDeadline, "Deadline", _buttonStyle, GUILayout.Width(70));
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    Settings.InfoDisplayEnemies = GUILayout.Toggle(Settings.InfoDisplayEnemies, "Enemies", _buttonStyle, GUILayout.Width(70));
                    Settings.InfoDisplayBodies = GUILayout.Toggle(Settings.InfoDisplayBodies, "Bodies", _buttonStyle, GUILayout.Width(60));
                    Settings.InfoDisplayMapLoot = GUILayout.Toggle(Settings.InfoDisplayMapLoot, "Map Loot", _buttonStyle, GUILayout.Width(75));
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    Settings.InfoDisplayShipLoot = GUILayout.Toggle(Settings.InfoDisplayShipLoot, "Ship Loot", _buttonStyle, GUILayout.Width(75));
                    Settings.InfoDisplayMoon = GUILayout.Toggle(Settings.InfoDisplayMoon, "Moon", _buttonStyle, GUILayout.Width(55));
                    Settings.InfoDisplayTime = GUILayout.Toggle(Settings.InfoDisplayTime, "Time", _buttonStyle, GUILayout.Width(50));
                    GUILayout.EndHorizontal();
                }

                Settings.NoVisor = DrawToggle("No Visor", Settings.NoVisor, "Hide helmet visor");
                Settings.NoCameraShake = DrawToggle("No Camera Shake", Settings.NoCameraShake, "Disable screen shake");
                Settings.NoFieldOfDepth = DrawToggle("No Depth of Field", Settings.NoFieldOfDepth, "Disable blur effects");

                // Full Render Resolution toggle - applies via Harmony patch
                bool prevFullRes = Settings.FullRenderResolution;
                Settings.FullRenderResolution = DrawToggle("Full Render Resolution", Settings.FullRenderResolution, "Render at native screen resolution");
                if (prevFullRes != Settings.FullRenderResolution)
                {
                    // Apply immediately when toggled
                    FullRenderResolutionPatch.ApplyResolution(LethalMenuMod.LocalPlayer);
                }

                Settings.FOV = DrawToggle("Custom FOV", Settings.FOV, "Change field of view");
                if (Settings.FOV)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"  FOV: {Settings.FOVValue:F0}", _labelStyle, GUILayout.Width(80));
                    Settings.FOVValue = GUILayout.HorizontalSlider(Settings.FOVValue, 60f, 120f);
                    GUILayout.EndHorizontal();
                }
                Settings.Breadcrumbs = DrawToggle("Breadcrumbs", Settings.Breadcrumbs, "Mark your path");
                if (Settings.Breadcrumbs)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"  Interval: {Settings.BreadcrumbInterval:F1}s", _labelStyle, GUILayout.Width(100));
                    Settings.BreadcrumbInterval = GUILayout.HorizontalSlider(Settings.BreadcrumbInterval, 1f, 10f);
                    GUILayout.EndHorizontal();
                }
            });

            DrawSection("Environment", () =>
            {
                Settings.NoFog = DrawToggle("No Fog", Settings.NoFog, "Remove all fog effects");
            });
        }

        private void CycleNextSpectatePlayer()
        {
            if (LethalMenuMod.Players.Count <= 1) return;

            int startIndex = Settings.SpectatePlayerIndex;
            int nextIndex = (startIndex + 1) % LethalMenuMod.Players.Count;

            for (int i = 0; i < LethalMenuMod.Players.Count; i++)
            {
                var player = LethalMenuMod.Players[nextIndex];
                if (player != null && player != LethalMenuMod.LocalPlayer && !player.isPlayerDead)
                {
                    Settings.SpectatePlayerIndex = nextIndex;
                    return;
                }
                nextIndex = (nextIndex + 1) % LethalMenuMod.Players.Count;
            }
        }

        private void CyclePreviousSpectatePlayer()
        {
            if (LethalMenuMod.Players.Count <= 1) return;

            int startIndex = Settings.SpectatePlayerIndex;
            if (startIndex < 0) startIndex = 0;
            int prevIndex = (startIndex - 1 + LethalMenuMod.Players.Count) % LethalMenuMod.Players.Count;

            for (int i = 0; i < LethalMenuMod.Players.Count; i++)
            {
                var player = LethalMenuMod.Players[prevIndex];
                if (player != null && player != LethalMenuMod.LocalPlayer && !player.isPlayerDead)
                {
                    Settings.SpectatePlayerIndex = prevIndex;
                    return;
                }
                prevIndex = (prevIndex - 1 + LethalMenuMod.Players.Count) % LethalMenuMod.Players.Count;
            }
        }

        #endregion
    }
}
