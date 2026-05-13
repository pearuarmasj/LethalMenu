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
                DrawHackToggle(Hack.EnableESP, "Enable ESP", "Show objects through walls");

                if (Hack.EnableESP.IsEnabled())
                {
                    GUILayout.Space(5);
                    DrawHackToggle(Hack.PlayerESP, "  Player ESP", null);
                    if (Hack.PlayerESP.IsEnabled())
                    {
                        DrawHackToggle(Hack.PlayerHealthBars, "    Health Bars", "Show HP bars above players");
                    }
                    DrawHackToggle(Hack.EnemyESP, "  Enemy ESP", null);
                    DrawHackToggle(Hack.ItemESP, "  Item ESP", null);
                    DrawHackToggle(Hack.DoorESP, "  Door ESP", null);
                    DrawHackToggle(Hack.MineESP, "  Mine ESP", null);
                    DrawHackToggle(Hack.TurretESP, "  Turret ESP", null);
                    DrawHackToggle(Hack.FuseboxESP, "  Fusebox ESP", null);
                }
            });

            DrawSection("Camera", () =>
            {
                DrawHackToggle(Hack.FreeCam, "FreeCam", "WASD+Mouse to fly around");
                if (Hack.FreeCam.IsEnabled())
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

                bool prevThirdPerson = Hack.ThirdPerson.IsEnabled();
                DrawHackToggle(Hack.ThirdPerson, "Third Person", "Press V to toggle (view from behind)");
                if (prevThirdPerson != Hack.ThirdPerson.IsEnabled())
                {
                    LethalMenu.Cheats.ThirdPersonCheat.Toggle();
                }
                if (Hack.ThirdPerson.IsEnabled())
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"  Distance: {Settings.ThirdPersonDistance:F1}", _labelStyle, GUILayout.Width(100));
                    Settings.ThirdPersonDistance = GUILayout.HorizontalSlider(Settings.ThirdPersonDistance, 1f, 10f);
                    GUILayout.EndHorizontal();
                }

                DrawHackToggle(Hack.SpectatePlayer, "Spectate Player", "Watch another player");
                if (Hack.SpectatePlayer.IsEnabled())
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
                DrawHackToggle(Hack.AlwaysShowClock, "Always Show Clock", "Clock always visible");
                DrawHackToggle(Hack.Crosshair, "Crosshair", "Show crosshair on screen");
                DrawHackToggle(Hack.HPDisplay, "HP Display", "Show health on screen");

                DrawHackToggle(Hack.InfoDisplay, "Info Display", "Show game info panel (top-right)");
                if (Hack.InfoDisplay.IsEnabled())
                {
                    GUILayout.Label("  Display Options:", _labelStyle);
                    GUILayout.BeginHorizontal();
                    ToggleHackButton(Hack.InfoDisplayCredits, "Credits", 70);
                    ToggleHackButton(Hack.InfoDisplayQuota, "Quota", 60);
                    ToggleHackButton(Hack.InfoDisplayDeadline, "Deadline", 70);
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    ToggleHackButton(Hack.InfoDisplayEnemies, "Enemies", 70);
                    ToggleHackButton(Hack.InfoDisplayBodies, "Bodies", 60);
                    ToggleHackButton(Hack.InfoDisplayMapLoot, "Map Loot", 75);
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    ToggleHackButton(Hack.InfoDisplayShipLoot, "Ship Loot", 75);
                    ToggleHackButton(Hack.InfoDisplayMoon, "Moon", 55);
                    ToggleHackButton(Hack.InfoDisplayTime, "Time", 50);
                    GUILayout.EndHorizontal();
                }

                DrawHackToggle(Hack.NoVisor, "No Visor", "Hide helmet visor");
                DrawHackToggle(Hack.NoCameraShake, "No Camera Shake", "Disable screen shake");
                DrawHackToggle(Hack.NoDepthOfField, "No Depth of Field", "Disable blur effects");

                bool prevFullRes = Hack.FullRenderResolution.IsEnabled();
                DrawHackToggle(Hack.FullRenderResolution, "Full Render Resolution", "Render at native screen resolution");
                if (prevFullRes != Hack.FullRenderResolution.IsEnabled())
                {
                    FullRenderResolutionPatch.ApplyResolution(LethalMenuMod.LocalPlayer);
                }

                DrawHackToggle(Hack.CustomFOV, "Custom FOV", "Change field of view");
                if (Hack.CustomFOV.IsEnabled())
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"  FOV: {Settings.FOVValue:F0}", _labelStyle, GUILayout.Width(80));
                    Settings.FOVValue = GUILayout.HorizontalSlider(Settings.FOVValue, 60f, 120f);
                    GUILayout.EndHorizontal();
                }
                DrawHackToggle(Hack.Breadcrumbs, "Breadcrumbs", "Mark your path");
                if (Hack.Breadcrumbs.IsEnabled())
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"  Interval: {Settings.BreadcrumbInterval:F1}s", _labelStyle, GUILayout.Width(100));
                    Settings.BreadcrumbInterval = GUILayout.HorizontalSlider(Settings.BreadcrumbInterval, 1f, 10f);
                    GUILayout.EndHorizontal();
                }
            });

            DrawSection("Environment", () =>
            {
                DrawHackToggle(Hack.NoFog, "No Fog", "Remove all fog effects");
                DrawHackToggle(Hack.ClearVisionMod, "Clear Vision", "Locally suppress HDRP fog/dust effects");
                DrawHackToggle(Hack.MinimalGUIMod, "Minimal GUI", "Hide all HUD for cinematic shots");
                DrawHackToggle(Hack.RadarPatch, "Radar+", "Extend in-ship radar coverage");
                DrawHackToggle(Hack.EnemyDeathNotification, "Enemy Death Notifications", "HUD tip on every enemy death");
            });
        }

        private void ToggleHackButton(Hack hack, string label, int width)
        {
            bool current = hack.IsEnabled();
            bool newVal = GUILayout.Toggle(current, label, _buttonStyle, GUILayout.Width(width));
            if (newVal != current) hack.SetEnabled(newVal);
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
