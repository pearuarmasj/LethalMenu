using UnityEngine;
using System.Linq;

namespace LethalMenu.Menu
{
    public partial class HackMenu
    {
        #region Network Tab

        // Network tab state
        private string _creditSetInput = "10000";
        private string _chatMessageInput = "";
        private string _signalMessageInput = "";

        private int _selectedMimicPlayerIndex = 0;

        private bool RequireHost()
        {
            if (LethalMenuMod.LocalPlayer?.IsHost == true)
                return true;

            HUDManager.Instance?.DisplayTip("Host Only", "This action requires host.", false, false, "LCM-Host");
            return false;
        }

        private void DrawNetworkTab()
        {
            DrawSection("Network Status", () =>
            {
                var gameInstance = StartOfRound.Instance;
                if (gameInstance != null)
                {
                    GUILayout.Label($"Current Planet: {gameInstance.currentLevel?.PlanetName ?? "Unknown"}", _labelStyle);
                    GUILayout.Label($"Is Host: {Cheats.NetworkCheats.IsHost()}", _labelStyle);

                    var terminal = Object.FindObjectOfType<Terminal>();
                    if (terminal != null)
                    {
                        GUILayout.Label($"Credits: ${terminal.groupCredits}", _labelStyle);
                    }

                    var timeOfDay = TimeOfDay.Instance;
                    if (timeOfDay != null)
                    {
                        GUILayout.Label($"Quota: ${timeOfDay.quotaFulfilled} / ${timeOfDay.profitQuota}", _labelStyle);
                    }
                }
                else
                {
                    GUILayout.Label("Not in game", _labelStyle);
                }
            });

            DrawSection("Networking Options", () =>
            {
                DrawHackToggle(Hack.AntiKick, "Anti-Kick", "Rejoin lobbies after being kicked");
                DrawHackToggle(Hack.ShowKickedLobbies, "Show Kicked Hosts", "Mark lobbies from hosts who kicked you");

                if (Settings.KickedHostIds.Count > 0)
                {
                    GUILayout.Label($"  Kicked by {Settings.KickedHostIds.Count} host(s)", _labelStyle);
                    if (GUILayout.Button("Clear Kicked Hosts", _buttonStyle, GUILayout.Width(150)))
                    {
                        Settings.KickedHostIds.Clear();
                        Settings.SaveConfig();
                    }
                }

                DrawHackToggle(Hack.HearEveryone, "Hear Everyone", "Hear all voice chat");
                DrawHackToggle(Hack.Invisibility, "Invisibility", "Other players can't see you");
                DrawHackToggle(Hack.DeathNotifications, "Death Notifications", "See when players die");
                DrawHackToggle(Hack.HearDeadPeople, "Hear Dead People", "Hear dead players' voice chat");
            });

            DrawSection("Credits Exploit", () =>
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Amount:", _labelStyle, GUILayout.Width(60));
                _creditSetInput = GUILayout.TextField(_creditSetInput, GUILayout.Width(100));
                if (GUILayout.Button("Set Credits", _buttonStyle, GUILayout.Width(100)))
                {
                    if (int.TryParse(_creditSetInput, out int amount))
                    {
                        Cheats.NetworkCheats.SetCredits(amount);
                    }
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("+1000", _buttonStyle)) Cheats.NetworkCheats.SetCredits(GetCurrentCredits() + 1000);
                if (GUILayout.Button("+10000", _buttonStyle)) Cheats.NetworkCheats.SetCredits(GetCurrentCredits() + 10000);
                if (GUILayout.Button("MAX", _buttonStyle)) Cheats.NetworkCheats.SetCredits(999999);
                GUILayout.EndHorizontal();
            });

            // Quota (Host)
            DrawSection("Quota (Host)", () =>
            {
                if (!RequireHost()) { GUILayout.Label("Host only.", _labelStyle); return; }

                var quotaInfo = Cheats.NetworkCheats.GetQuotaInfo();
                _quotaInput = quotaInfo.quota.ToString();
                _quotaFulfilledInput = quotaInfo.fulfilled.ToString();

                int daysLeft = TimeOfDay.Instance != null ? Mathf.Max(0, TimeOfDay.Instance.daysUntilDeadline) : 0;

                string needText = quotaInfo.remaining >= 0
                    ? $"Need: ${quotaInfo.remaining}"
                    : $"Over by ${-quotaInfo.remaining}";
                GUILayout.Label($"Quota: ${quotaInfo.fulfilled} / ${quotaInfo.quota} ({needText})", _labelStyle);
                GUILayout.Label($"Days Left: {daysLeft}", _labelStyle);

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Set 0", _buttonStyle, GUILayout.Width(60))) Cheats.NetworkCheats.SetQuota(0);
                if (GUILayout.Button("Set 100", _buttonStyle, GUILayout.Width(60))) Cheats.NetworkCheats.SetQuota(100);
                if (GUILayout.Button("Complete", _buttonStyle, GUILayout.Width(70)))
                {
                    var quota = TimeOfDay.Instance?.profitQuota ?? 100;
                    Cheats.NetworkCheats.SetQuota(quota, quota);
                }
                if (GUILayout.Button("Sell Quota", _buttonStyle, GUILayout.Width(80)))
                {
                    Hack.SellQuota.Execute();
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Quota:", _labelStyle, GUILayout.Width(50));
                _quotaInput = GUILayout.TextField(_quotaInput, GUILayout.Width(60));
                GUILayout.Label("Fulfilled:", _labelStyle, GUILayout.Width(55));
                _quotaFulfilledInput = GUILayout.TextField(_quotaFulfilledInput, GUILayout.Width(60));
                if (GUILayout.Button("Set", _buttonStyle, GUILayout.Width(40)))
                {
                    if (int.TryParse(_quotaInput, out int q) && int.TryParse(_quotaFulfilledInput, out int f))
                    {
                        Cheats.NetworkCheats.SetQuota(q, f);
                        quotaInfo = Cheats.NetworkCheats.GetQuotaInfo();
                        _quotaInput = quotaInfo.quota.ToString();
                        _quotaFulfilledInput = quotaInfo.fulfilled.ToString();
                    }
                }
                GUILayout.EndHorizontal();
            });

            DrawSection("Chat Exploits", () =>
            {
                GUILayout.BeginHorizontal();
                _chatMessageInput = GUILayout.TextField(_chatMessageInput, GUILayout.Width(200));
                if (GUILayout.Button("Send", _buttonStyle, GUILayout.Width(50)))
                {
                    if (!string.IsNullOrEmpty(_chatMessageInput))
                    {
                        Cheats.NetworkCheats.SendChatMessage(_chatMessageInput);
                        // Note: Same message can be sent again by clicking again
                    }
                }
                if (GUILayout.Button("Clear", _buttonStyle, GUILayout.Width(50)))
                {
                    _chatMessageInput = "";
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("System Msg", _buttonStyle))
                {
                    if (!string.IsNullOrEmpty(_chatMessageInput))
                    {
                        Cheats.NetworkCheats.SendSystemMessage(_chatMessageInput);
                    }
                }
                if (GUILayout.Button("Chat Spam x10", _buttonStyle))
                {
                    if (!string.IsNullOrEmpty(_chatMessageInput))
                    {
                        Cheats.NetworkCheats.SpamChat(_chatMessageInput, 10);
                    }
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Sys Spam x10", _buttonStyle))
                {
                    if (!string.IsNullOrEmpty(_chatMessageInput))
                    {
                        Cheats.NetworkCheats.SpamSystemMessage(_chatMessageInput, 10);
                    }
                }
                if (GUILayout.Button("MAX SPAM x50", _buttonStyle))
                {
                    if (!string.IsNullOrEmpty(_chatMessageInput))
                    {
                        Cheats.NetworkCheats.SpamChat(_chatMessageInput, 50);
                        Cheats.NetworkCheats.SpamSystemMessage(_chatMessageInput, 50);
                    }
                }
                GUILayout.EndHorizontal();
            });

            DrawSection("Ship Control (Host)", () =>
            {
                if (!RequireHost()) { GUILayout.Label("Host only.", _labelStyle); return; }

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Force Ship Leave", _buttonStyle))
                {
                    Hack.ForceShipLeave.Execute();
                }
                if (GUILayout.Button("Eject All Players", _buttonStyle))
                {
                    Hack.EjectAllPlayers.Execute();
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Force Start", _buttonStyle))
                {
                    Hack.ForceStart.Execute();
                }
                if (GUILayout.Button("Force End", _buttonStyle))
                {
                    Hack.ForceEnd.Execute();
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Lights ON", _buttonStyle))
                {
                    Cheats.NetworkCheats.ToggleShipLights(true);
                }
                if (GUILayout.Button("Lights OFF", _buttonStyle))
                {
                    Cheats.NetworkCheats.ToggleShipLights(false);
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Magnet ON", _buttonStyle))
                {
                    Cheats.NetworkCheats.ToggleMagnet(true);
                }
                if (GUILayout.Button("Magnet OFF", _buttonStyle))
                {
                    Cheats.NetworkCheats.ToggleMagnet(false);
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Doors Open", _buttonStyle))
                {
                    Cheats.NetworkCheats.SetShipDoors(false);
                }
                if (GUILayout.Button("Doors Close", _buttonStyle))
                {
                    Cheats.NetworkCheats.SetShipDoors(true);
                }
                if (GUILayout.Button("OVERHEAT", _buttonStyle))
                {
                    Cheats.NetworkCheats.OverheatShipDoors();
                }
                GUILayout.EndHorizontal();
            });

            DrawSection("Vehicle Control (Cruiser) (Host)", () =>
            {
                if (!RequireHost()) { GUILayout.Label("Host only.", _labelStyle); return; }

                var vehicles = Object.FindObjectsOfType<VehicleController>();
                int vehicleCount = vehicles?.Length ?? 0;
                GUILayout.Label($"Vehicles on map: {vehicleCount}", _labelStyle);

                if (vehicleCount > 0)
                {
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Hijack Vehicle", _buttonStyle))
                    {
                        Cheats.NetworkCheats.HijackVehicle();
                    }
                    if (GUILayout.Button("Eject Driver", _buttonStyle))
                    {
                        Cheats.NetworkCheats.EjectVehicleDriver();
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("+5 Turbo", _buttonStyle))
                    {
                        Cheats.NetworkCheats.AddVehicleTurbo(5);
                    }
                    if (GUILayout.Button("Use Turbo", _buttonStyle))
                    {
                        Cheats.NetworkCheats.UseVehicleTurbo();
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Kill Engine", _buttonStyle))
                    {
                        Cheats.NetworkCheats.KillVehicleEngine();
                    }
                    if (GUILayout.Button("Repair Engine", _buttonStyle))
                    {
                        Cheats.NetworkCheats.RepairVehicleEngine();
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.Label("Gear Control:", _labelStyle);
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Park", _buttonStyle))
                    {
                        Cheats.NetworkCheats.ShiftVehicleGear(0);
                    }
                    if (GUILayout.Button("Drive", _buttonStyle))
                    {
                        Cheats.NetworkCheats.ShiftVehicleGear(1);
                    }
                    if (GUILayout.Button("Reverse", _buttonStyle))
                    {
                        Cheats.NetworkCheats.ShiftVehicleGear(2);
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Horn ON", _buttonStyle))
                    {
                        Cheats.NetworkCheats.ToggleCarHorns(true);
                    }
                    if (GUILayout.Button("Horn OFF", _buttonStyle))
                    {
                        Cheats.NetworkCheats.ToggleCarHorns(false);
                    }
                    if (GUILayout.Button("Explode All", _buttonStyle))
                    {
                        Cheats.NetworkCheats.ExplodeAllVehicles();
                    }
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.Label("  No vehicles found. Buy a Cruiser!", _labelStyle);
                }
            });

            DrawSection("Player Level (Cosmetic)", () =>
            {
                GUILayout.Label("Changes your badge - purely visual flex", _labelStyle);

                var levelNames = Cheats.NetworkCheats.GetLevelNames();
                int currentLevel = Cheats.NetworkCheats.GetCurrentLevelIndex();
                int currentXP = Cheats.NetworkCheats.GetCurrentXP();

                GUILayout.Label($"Current: {(levelNames.Length > currentLevel ? levelNames[currentLevel] : "?")} ({currentXP} XP)", _labelStyle);

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("MAX LEVEL", _buttonStyle, GUILayout.Height(28)))
                {
                    Cheats.NetworkCheats.MaxOutXP();
                }
                if (GUILayout.Button("Reset (0 XP)", _buttonStyle))
                {
                    Cheats.NetworkCheats.SetPlayerXP(0);
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("+100 XP", _buttonStyle))
                {
                    Cheats.NetworkCheats.SetPlayerXP(currentXP + 100);
                }
                if (GUILayout.Button("+1000 XP", _buttonStyle))
                {
                    Cheats.NetworkCheats.SetPlayerXP(currentXP + 1000);
                }
                if (GUILayout.Button("+10000 XP", _buttonStyle))
                {
                    Cheats.NetworkCheats.SetPlayerXP(currentXP + 10000);
                }
                GUILayout.EndHorizontal();
            });

            // Player Management (Host)
            DrawSection("Player Management (Host)", () =>
            {
                if (!RequireHost()) { GUILayout.Label("Host only.", _labelStyle); return; }

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Revive All Players", _buttonStyle))
                {
                    Hack.ReviveAllPlayers.Execute();
                }
                if (GUILayout.Button("Teleport All To Me", _buttonStyle))
                {
                    Hack.TeleportAllToMe.Execute();
                }
                GUILayout.EndHorizontal();
            });

            // Facility Control (Host)
            DrawSection("Facility Control (Host)", () =>
            {
                if (!RequireHost()) { GUILayout.Label("Host only.", _labelStyle); return; }

                GUILayout.Label("Facility Doors:", _labelStyle);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Unlock All", _buttonStyle))
                {
                    Hack.UnlockAllDoors.Execute();
                }
                if (GUILayout.Button("Lock All", _buttonStyle))
                {
                    Cheats.NetworkCheats.LockAllDoors();
                }
                if (GUILayout.Button("Toggle Big Doors", _buttonStyle))
                {
                    Cheats.NetworkCheats.ToggleBigDoors();
                }
                GUILayout.EndHorizontal();

                GUILayout.Label("Noise Maker:", _labelStyle);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Noise At Me", _buttonStyle))
                {
                    Cheats.NetworkCheats.MakeNoiseAtMe();
                }
                if (GUILayout.Button("Noise At Camera", _buttonStyle))
                {
                    Cheats.NetworkCheats.MakeNoiseAtCamera();
                }
                GUILayout.EndHorizontal();
            });

            // Timescale (Host)
            DrawSection("Timescale (Host)", () =>
            {
                if (!RequireHost()) { GUILayout.Label("Host only.", _labelStyle); return; }

                GUILayout.Label($"Game Speed: {Time.timeScale:F1}x", _labelStyle);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("0.5x", _buttonStyle)) Cheats.NetworkCheats.SetTimescale(0.5f);
                if (GUILayout.Button("1x", _buttonStyle)) Cheats.NetworkCheats.SetTimescale(1f);
                if (GUILayout.Button("2x", _buttonStyle)) Cheats.NetworkCheats.SetTimescale(2f);
                if (GUILayout.Button("5x", _buttonStyle)) Cheats.NetworkCheats.SetTimescale(5f);
                GUILayout.EndHorizontal();
            });

            // Player Trolling (Host)
            DrawSection("Player Trolling (Host)", () =>
            {
                if (!RequireHost()) { GUILayout.Label("Host only.", _labelStyle); return; }

                var mobPlayers = Cheats.NetworkCheats.GetAllPlayers();
                if (mobPlayers.Length > 0)
                {
                    var mobPlayerNames = mobPlayers.Select(p => p.playerUsername ?? "Unknown").ToArray();
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("<", _buttonStyle, GUILayout.Width(30)))
                        _selectedMimicPlayerIndex = (_selectedMimicPlayerIndex - 1 + mobPlayerNames.Length) % mobPlayerNames.Length;
                    _selectedMimicPlayerIndex = Mathf.Clamp(_selectedMimicPlayerIndex, 0, mobPlayerNames.Length - 1);
                    GUILayout.Label(mobPlayerNames[_selectedMimicPlayerIndex], _labelStyle, GUILayout.Width(100));
                    if (GUILayout.Button(">", _buttonStyle, GUILayout.Width(30)))
                        _selectedMimicPlayerIndex = (_selectedMimicPlayerIndex + 1) % mobPlayerNames.Length;
                    if (GUILayout.Button("MOB!", _buttonStyle, GUILayout.Width(60)))
                    {
                        Cheats.NetworkCheats.MobPlayer(mobPlayers[_selectedMimicPlayerIndex], false);
                    }
                    if (GUILayout.Button("MOB+TP!", _buttonStyle, GUILayout.Width(70)))
                    {
                        Cheats.NetworkCheats.MobPlayer(mobPlayers[_selectedMimicPlayerIndex], true);
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("VOID", _buttonStyle, GUILayout.Width(60)))
                    {
                        Cheats.NetworkCheats.SendToVoid(mobPlayers[_selectedMimicPlayerIndex]);
                    }
                    if (GUILayout.Button("BOMB", _buttonStyle, GUILayout.Width(60)))
                    {
                        Cheats.NetworkCheats.BombPlayer(mobPlayers[_selectedMimicPlayerIndex]);
                    }
                    if (GUILayout.Button("LAG", _buttonStyle, GUILayout.Width(50)))
                    {
                        Cheats.NetworkCheats.LagPlayer(mobPlayers[_selectedMimicPlayerIndex]);
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.Label("  VOID=death | BOMB=jetpack | LAG=bracken", _labelStyle);

                    GUILayout.Space(5);
                    GUILayout.Label("Spin Player:", _labelStyle);
                    GUILayout.BeginHorizontal();
                    Settings.SpinCamera = GUILayout.Toggle(Settings.SpinCamera, "Camera", _toggleStyle, GUILayout.Width(70));
                    Settings.SpinModel = GUILayout.Toggle(Settings.SpinModel, "Model", _toggleStyle, GUILayout.Width(60));
                    GUILayout.Label($"Time: {Settings.SpinDuration:F0}s", _labelStyle, GUILayout.Width(60));
                    if (GUILayout.Button("-", _buttonStyle, GUILayout.Width(25)))
                        Settings.SpinDuration = Mathf.Max(1f, Settings.SpinDuration - 5f);
                    if (GUILayout.Button("+", _buttonStyle, GUILayout.Width(25)))
                        Settings.SpinDuration = Mathf.Min(60f, Settings.SpinDuration + 5f);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("SPIN!", _buttonStyle, GUILayout.Width(60)))
                    {
                        Cheats.NetworkCheats.SpinPlayer(
                            mobPlayers[_selectedMimicPlayerIndex],
                            Settings.SpinDuration,
                            Settings.SpinCamera,
                            Settings.SpinModel);
                    }
                    if (GUILayout.Button("STOP", _buttonStyle, GUILayout.Width(50)))
                    {
                        Cheats.NetworkCheats.StopSpinPlayer(mobPlayers[_selectedMimicPlayerIndex]);
                    }
                    bool isSpinning = Cheats.NetworkCheats.IsSpinning(mobPlayers[_selectedMimicPlayerIndex]);
                    GUILayout.Label(isSpinning ? " [SPINNING]" : "", _labelStyle);
                    GUILayout.EndHorizontal();
                }

                if (GUILayout.Button("Spin Ship Objects", _buttonStyle))
                {
                    Cheats.NetworkCheats.SpinShipObjects(5f);
                }

                if (GUILayout.Button("Reset Ship Objects", _buttonStyle))
                {
                    Cheats.NetworkCheats.ResetAllShipObjects();
                }
            });

            DrawSection("Player Actions", () =>
            {
                var players = Cheats.NetworkCheats.GetAllPlayers();
                var playerNames = players.Select(p => p.playerUsername ?? "Unknown").ToArray();

                if (playerNames.Length > 0)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Target:", _labelStyle, GUILayout.Width(50));
                    _selectedPlayerIndex = Mathf.Clamp(_selectedPlayerIndex, 0, playerNames.Length - 1);

                    if (GUILayout.Button("<", _buttonStyle, GUILayout.Width(30)))
                        _selectedPlayerIndex = (_selectedPlayerIndex - 1 + playerNames.Length) % playerNames.Length;
                    GUILayout.Label(playerNames[_selectedPlayerIndex], _labelStyle, GUILayout.Width(120));
                    if (GUILayout.Button(">", _buttonStyle, GUILayout.Width(30)))
                        _selectedPlayerIndex = (_selectedPlayerIndex + 1) % playerNames.Length;
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Damage (10)", _buttonStyle))
                    {
                        Cheats.NetworkCheats.DamagePlayer(players[_selectedPlayerIndex], 10);
                    }
                    if (GUILayout.Button("Damage (50)", _buttonStyle))
                    {
                        Cheats.NetworkCheats.DamagePlayer(players[_selectedPlayerIndex], 50);
                    }
                    if (GUILayout.Button("Kill (100)", _buttonStyle))
                    {
                        Cheats.NetworkCheats.DamagePlayer(players[_selectedPlayerIndex], 100);
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Heal Player", _buttonStyle))
                    {
                        Cheats.NetworkCheats.HealPlayer(players[_selectedPlayerIndex]);
                    }
                    if (GUILayout.Button("Force Drop Items", _buttonStyle))
                    {
                        Cheats.NetworkCheats.ForceDropItems(players[_selectedPlayerIndex]);
                    }
                    GUILayout.EndHorizontal();

                    // Teleport options
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("TP Me To Player", _buttonStyle))
                    {
                        if (LethalMenuMod.LocalPlayer != null)
                            Cheats.NetworkCheats.TeleportPlayerToPlayer(LethalMenuMod.LocalPlayer, players[_selectedPlayerIndex]);
                    }
                    if (GUILayout.Button("TP Player To Me", _buttonStyle))
                    {
                        if (LethalMenuMod.LocalPlayer != null)
                            Cheats.NetworkCheats.TeleportPlayerToPosition(players[_selectedPlayerIndex], LethalMenuMod.LocalPlayer.transform.position);
                    }
                    GUILayout.EndHorizontal();

                    // More teleport options (host only for remote players)
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("TP Random Inside", _buttonStyle))
                    {
                        Cheats.NetworkCheats.TeleportPlayerRandom(players[_selectedPlayerIndex], true);
                    }
                    if (GUILayout.Button("TP Random Outside", _buttonStyle))
                    {
                        Cheats.NetworkCheats.TeleportPlayerRandom(players[_selectedPlayerIndex], false);
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("TP To Void (Death)", _buttonStyle))
                    {
                        Cheats.NetworkCheats.TeleportPlayerToVoid(players[_selectedPlayerIndex]);
                    }
                    if (players.Length >= 2)
                    {
                        int otherIdx = (_selectedPlayerIndex + 1) % players.Length;
                        if (GUILayout.Button($"Swap w/ {players[otherIdx].playerUsername}", _buttonStyle))
                        {
                            Cheats.NetworkCheats.SwapPlayerPositions(players[_selectedPlayerIndex], players[otherIdx]);
                        }
                    }
                    GUILayout.EndHorizontal();

                    // Teleport to ship via teleporter (works on any player)
                    if (GUILayout.Button("TELEPORT TO SHIP (via TP)", _buttonStyle, GUILayout.Height(28)))
                    {
                        Cheats.NetworkCheats.TeleportPlayerViaShipTeleporter(players[_selectedPlayerIndex]);
                    }

                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("TP to Main Entrance", _buttonStyle))
                    {
                        Cheats.NetworkCheats.TeleportPlayerToEntrance(players[_selectedPlayerIndex], true);
                    }
                    if (GUILayout.Button("TP to Fire Exit", _buttonStyle))
                    {
                        Cheats.NetworkCheats.TeleportPlayerToEntrance(players[_selectedPlayerIndex], false);
                    }
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.Label("No other players found.", _labelStyle);
                }
            });

            // Malicious section - only show if enabled
            DrawSection("Trolling / Malicious (USE RESPONSIBLY)", () =>
            {
                GUILayout.Label("These can ruin other players' experience.", new GUIStyle(_labelStyle) { normal = { textColor = Color.red } });

                // SPAM TOGGLES - Continuous spam while enabled
                GUILayout.Label("--- Continuous Spam Toggles ---", _labelStyle);

                GUILayout.BeginHorizontal();
                DrawHackToggle(Hack.HornSpam, "Horn Spam", null);
                DrawHackToggle(Hack.DoorSpam, "Door Spam", null);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                DrawHackToggle(Hack.SignalSpam, "Signal Spam", null);
                DrawHackToggle(Hack.RPCLagSpam, "RPC Lag", null);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                DrawHackToggle(Hack.TerminalSoundSpam, "Terminal Spam", null);
                DrawHackToggle(Hack.EarrapeSpam, "EARRAPE", null);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                DrawHackToggle(Hack.ChatSpam, "Chat Spam", null);
                DrawHackToggle(Hack.CarHornSpam, "Car Horns", null);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                DrawHackToggle(Hack.DeskDoorSpam, "Desk Door", null);
                if (GUILayout.Button("Flicker Lights", _buttonStyle))
                {
                    Hack.FlickerLights.Execute();
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(5);
                GUILayout.Label("--- One-Shot Actions ---", _labelStyle);

                // One-shot chaos buttons
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("MAX CHAOS", _buttonStyle))
                {
                    Hack.MaxChaos.Execute();
                }
                if (GUILayout.Button("Terminal Crash", _buttonStyle))
                {
                    Cheats.NetworkCheats.AttemptTerminalCrash();
                }
                GUILayout.EndHorizontal();

                // Bracken Lag Attack (the real deal)
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("BRACKEN LAG ALL", _buttonStyle))
                {
                    Cheats.NetworkCheats.BrackenLagAllPlayers();
                }
                if (GUILayout.Button("Kill All Players", _buttonStyle))
                {
                    Cheats.NetworkCheats.MassKillPlayers();
                }
                GUILayout.EndHorizontal();

                // Chat spam one-shot
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Chat Spam x50", _buttonStyle))
                {
                    Cheats.NetworkCheats.SpamChatMax(Settings.SpamMessage);
                }
                if (GUILayout.Button("Earrape x30", _buttonStyle))
                {
                    Cheats.NetworkCheats.SpamTerminalEarrape(30);
                }
                GUILayout.EndHorizontal();

                // Impersonation
                var players = Cheats.NetworkCheats.GetAllPlayers();
                if (players.Length > 0 && !string.IsNullOrEmpty(_chatMessageInput))
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Impersonate:", _labelStyle, GUILayout.Width(80));
                    for (int i = 0; i < Mathf.Min(players.Length, 4); i++)
                    {
                        int idx = i;
                        if (GUILayout.Button(players[idx].playerUsername ?? $"P{idx}", _buttonStyle, GUILayout.Width(70)))
                        {
                            Cheats.NetworkCheats.ImpersonateInChat(_chatMessageInput, (int)players[idx].playerClientId);
                        }
                    }
                    GUILayout.EndHorizontal();
                }
            });

            // Signal Translator direct message
            DrawSection("Signal Translator", () =>
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Message (max 10):", _labelStyle, GUILayout.Width(110));
                _signalMessageInput = GUILayout.TextField(_signalMessageInput ?? "", 10, GUILayout.Width(100));
                if (GUILayout.Button("Send", _buttonStyle, GUILayout.Width(60)))
                {
                    if (!string.IsNullOrEmpty(_signalMessageInput))
                    {
                        Cheats.NetworkCheats.SendSignalTranslatorMessage(_signalMessageInput);
                    }
                }
                GUILayout.EndHorizontal();
            });

            // Free vehicles section
            DrawSection("Free Vehicles", () =>
            {
                var vehicles = Cheats.NetworkCheats.GetAvailableVehicles();
                if (vehicles.Length > 0)
                {
                    GUILayout.BeginHorizontal();
                    for (int i = 0; i < vehicles.Length; i++)
                    {
                        var (id, name) = vehicles[i];
                        if (GUILayout.Button($"Buy {name}", _buttonStyle))
                        {
                            Cheats.NetworkCheats.BuyFreeVehicle(id);
                        }
                    }
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.Label("No vehicles available.", _labelStyle);
                }
            });

            // More trolling features
            DrawSection("Hazard Control", () =>
            {
                // Count active hazards
                int mineCount = 0;
                int turretCount = 0;
                foreach (var mine in LethalMenuMod.Landmines)
                {
                    if (mine != null && !mine.hasExploded) mineCount++;
                }
                foreach (var turret in LethalMenuMod.Turrets)
                {
                    if (turret != null) turretCount++;
                }
                GUILayout.Label($"Landmines: {mineCount}  |  Turrets: {turretCount}", _labelStyle);

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Blow Up All Mines", _buttonStyle))
                {
                    Hack.BlowUpAllMines.Execute();
                }
                if (GUILayout.Button("Mines OFF", _buttonStyle))
                {
                    Cheats.NetworkCheats.ToggleAllLandmines(false);
                }
                if (GUILayout.Button("Mines ON", _buttonStyle))
                {
                    Cheats.NetworkCheats.ToggleAllLandmines(true);
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Turrets OFF", _buttonStyle))
                {
                    Cheats.NetworkCheats.ToggleAllTurrets(false);
                }
                if (GUILayout.Button("Turrets ON", _buttonStyle))
                {
                    Cheats.NetworkCheats.ToggleAllTurrets(true);
                }
                if (GUILayout.Button("Berserk Turrets", _buttonStyle))
                {
                    Hack.BerserkTurrets.Execute();
                }
                GUILayout.EndHorizontal();
            });

            DrawSection("Structure Control", () =>
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Collapse Bridge", _buttonStyle))
                {
                    Cheats.NetworkCheats.ForceBridgeFall();
                }
                if (GUILayout.Button("Collapse Small Bridge", _buttonStyle))
                {
                    Cheats.NetworkCheats.ForceSmallBridgeFall();
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Toggle Factory Lights", _buttonStyle))
                {
                    Cheats.NetworkCheats.ToggleFactoryLights();
                }
                if (GUILayout.Button("Tentacle Attack", _buttonStyle))
                {
                    Cheats.NetworkCheats.ForceTentacleAttack();
                }
                GUILayout.EndHorizontal();
            });

            DrawSection("Vehicle Trolling", () =>
            {
                GUILayout.BeginHorizontal();
                DrawHackToggle(Hack.CarHornSpam, "Car Horn Spam", null);
                if (GUILayout.Button("Explode Vehicles", _buttonStyle))
                {
                    Cheats.NetworkCheats.ExplodeAllVehicles();
                }
                GUILayout.EndHorizontal();
            });

            DrawSection("Weapon/Item Chaos", () =>
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Fire All Shotguns", _buttonStyle))
                {
                    Cheats.NetworkCheats.ShootAllShotguns();
                }
                if (GUILayout.Button("Spam Shotguns", _buttonStyle))
                {
                    Cheats.NetworkCheats.SpamShootAllShotguns(10);
                }
                GUILayout.EndHorizontal();

                if (GUILayout.Button("Explode Jetpacks", _buttonStyle))
                {
                    Cheats.NetworkCheats.ExplodeAllJetpacks();
                }
            });
        }

        #endregion
    }
}
