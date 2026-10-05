using UnityEngine;

namespace LethalMenu.Menu
{
    public partial class HackMenu
    {
        #region Visuals Tab

        private void DrawVisualsTab()
        {
            DrawSection("ESP", () =>
            {
                DrawHackToggle(Hack.EnableESP, "Enable ESP", "Boxes and labels through walls");

                if (Hack.EnableESP.IsEnabled())
                {
                    GUILayout.Space(5);
                    DrawHackToggle(Hack.PlayerESP, "  Players", null);
                    if (Hack.PlayerESP.IsEnabled())
                    {
                        DrawHackToggle(Hack.PlayerHealthBars, "    Health Bars", "Show HP bars above players");
                    }
                    DrawHackToggle(Hack.EnemyESP, "  Enemies", null);
                    DrawHackToggle(Hack.ItemESP, "  Items", null);
                    DrawHackToggle(Hack.DoorESP, "  Entrances", null);
                    DrawHackToggle(Hack.MineESP, "  Landmines", null);
                    DrawHackToggle(Hack.TurretESP, "  Turrets", null);
                    DrawHackToggle(Hack.FuseboxESP, "  Breaker Box", null);
                    DrawHackToggle(Hack.SteamValveESP,        "  Steam Valves", null);
                    DrawHackToggle(Hack.BigDoorESP,           "  Big Doors", null);
                    DrawHackToggle(Hack.ShipDoorESP,          "  Ship Door", null);
                    DrawHackToggle(Hack.EnemyVentESP,         "  Enemy Vents", null);
                    DrawHackToggle(Hack.ItemDropshipESP,      "  Item Dropship", null);
                    DrawHackToggle(Hack.CruiserESP,           "  Cruiser", null);
                    DrawHackToggle(Hack.MoldSporeESP,         "  Mold Spores", null);
                    DrawHackToggle(Hack.MineshaftElevatorESP, "  Mineshaft Elevator", null);
                    DrawHackToggle(Hack.SpikeRoofTrapESP,     "  Spike Traps", null);
                }
            });

            DrawSection("Chams", () =>
            {
                DrawHackToggle(Hack.EnableChams, "Enable Chams", "Solid-colour meshes through walls");
                if (!Hack.EnableChams.IsEnabled()) return;

                GUILayout.Space(5);
                DrawHackToggle(Hack.PlayerChams,            "  Players", null);
                DrawHackToggle(Hack.EnemyChams,             "  Enemies", null);
                DrawHackToggle(Hack.ItemChams,              "  Items", null);
                DrawHackToggle(Hack.LandmineChams,          "  Landmines", null);
                DrawHackToggle(Hack.TurretChams,            "  Turrets", null);
                DrawHackToggle(Hack.DoorChams,              "  Doors", null);
                DrawHackToggle(Hack.BigDoorChams,           "  Big Doors", null);
                DrawHackToggle(Hack.ShipDoorChams,          "  Ship Door", null);
                DrawHackToggle(Hack.BreakerChams,           "  Breaker Box", null);
                DrawHackToggle(Hack.EnemyVentChams,         "  Enemy Vents", null);
                DrawHackToggle(Hack.ItemDropshipChams,      "  Item Dropship", null);
                DrawHackToggle(Hack.CruiserChams,           "  Cruiser", null);
                DrawHackToggle(Hack.MoldSporeChams,         "  Mold Spores", null);
                DrawHackToggle(Hack.MineshaftElevatorChams, "  Mineshaft Elevator", null);
                DrawHackToggle(Hack.EntranceChams,          "  Entrances", null);
                DrawHackToggle(Hack.SpikeRoofTrapChams,     "  Spike Traps", null);
                DrawHackToggle(Hack.SteamValveChams,        "  Steam Valves", null);

                GUILayout.Space(5);
                Settings.UseSingleChamColor = GUILayout.Toggle(Settings.UseSingleChamColor, "  One colour for everything", _toggleStyle);
                GUILayout.BeginHorizontal();
                GUILayout.Label($"  Min Distance: {Settings.ChamDistance:F0}m", _labelStyle, GUILayout.Width(120));
                Settings.ChamDistance = GUILayout.HorizontalSlider(Settings.ChamDistance, 0f, 100f);
                GUILayout.EndHorizontal();
            });

            DrawSection("Camera", () =>
            {
                DrawHackToggle(Hack.FreeCam, "FreeCam", "Fly the camera; your body stays put");
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
                    GUILayout.Label("  Holding Shift while turning FreeCam off does the same.", _labelStyle);
                }

                DrawHackToggle(Hack.ThirdPerson, "Third Person", "V switches view");
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

            DrawSection("HUD", () =>
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
                DrawHackToggle(Hack.MinimalGUI, "Minimal GUI", "Hide the HUD");
                DrawHackToggle(Hack.RadarPatch, "Radar+", "Extend in-ship radar coverage");
                DrawHackToggle(Hack.EnemyDeathNotification, "Enemy Death Notifications", "HUD tip when an enemy dies");
            });

            DrawSection("View", () =>
            {
                DrawHackToggle(Hack.VisibleBody, "Visible Body", "See your own body in first person");
                DrawHackToggle(Hack.NoVisor, "No Visor", "Hide helmet visor");
                DrawHackToggle(Hack.NoCameraShake, "No Camera Shake", "Disable screen shake");
                DrawHackToggle(Hack.NoDepthOfField, "No Depth of Field", "Disable blur effects");
                DrawHackToggle(Hack.NoFog, "No Fog", "Disable fog");
                DrawHackToggle(Hack.FullRenderResolution, "Full Render Resolution", "Render at native screen resolution");

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
