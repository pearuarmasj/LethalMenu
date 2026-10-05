using UnityEngine;
using System.Linq;

namespace LethalMenu.Menu
{
    public partial class HackMenu
    {
        #region Network Tab

        private string _creditSetInput = "10000";
        private string _chatMessageInput = "";
        private string _signalMessageInput = "";

        // Remote-player teleports go out as UpdatePlayerPositionRpc; the target's own client is authoritative
        // for its position, so only other players see the move.
        private const string RemoteTeleportNote = "Teleporting another player only changes where others see them; on their own screen they stay put.";

        private void DrawNetworkTab()
        {
            DrawSection("Lobby", () =>
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
                DrawHackToggle(Hack.HearDeadPeople, "Hear Dead People", "Hear dead players' voice chat");
                DrawHackToggle(Hack.Invisibility, "Invisibility", "Other players can't see you");
                DrawHackToggle(Hack.DeathNotifications, "Player Death Notifications", "HUD tip when a player dies");

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Disconnect", _buttonStyle))
                {
                    Hack.Disconnect.Execute();
                }
                if (GUILayout.Button("Reconnect (lobby ID from clipboard)", _buttonStyle))
                {
                    Hack.ReconnectFromClipboard.Execute();
                }
                GUILayout.EndHorizontal();
            });

            // Terminal.SyncGroupCreditsServerRpc requires ownership of the (server-owned) Terminal, so only the host can set credits.
            DrawSection("Credits", () =>
            {
                if (!DrawHostGate()) return;

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
                if (GUILayout.Button("Max", _buttonStyle)) Cheats.NetworkCheats.SetCredits(999999);
                GUILayout.EndHorizontal();
            });

            DrawSection("Quota", () =>
            {
                if (!DrawHostGate()) return;

                var quotaInfo = Cheats.NetworkCheats.GetQuotaInfo();
                if (quotaInfo.quota != _lastSeenQuota)
                {
                    _lastSeenQuota = quotaInfo.quota;
                    _quotaInput = quotaInfo.quota.ToString();
                }
                if (quotaInfo.fulfilled != _lastSeenQuotaFulfilled)
                {
                    _lastSeenQuotaFulfilled = quotaInfo.fulfilled;
                    _quotaFulfilledInput = quotaInfo.fulfilled.ToString();
                }

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
                    }
                }
                GUILayout.EndHorizontal();
            });

            DrawSection("Chat", () =>
            {
                bool hasMessage = !string.IsNullOrEmpty(_chatMessageInput);
                if (hasMessage) Settings.SpamMessage = _chatMessageInput;

                GUILayout.BeginHorizontal();
                _chatMessageInput = GUILayout.TextField(_chatMessageInput, GUILayout.Width(200));
                if (GUILayout.Button("Clear", _buttonStyle, GUILayout.Width(50)))
                {
                    _chatMessageInput = "";
                }
                GUILayout.EndHorizontal();

                GUI.enabled = hasMessage;
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Send", _buttonStyle))
                {
                    Cheats.NetworkCheats.SendChatMessage(_chatMessageInput);
                }
                if (GUILayout.Button("Send as System", _buttonStyle))
                {
                    Cheats.NetworkCheats.SendSystemMessage(_chatMessageInput);
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Spam x10", _buttonStyle))
                {
                    Cheats.NetworkCheats.SpamChat(_chatMessageInput, 10);
                }
                if (GUILayout.Button("System Spam x10", _buttonStyle))
                {
                    Cheats.NetworkCheats.SpamSystemMessage(_chatMessageInput, 10);
                }
                if (GUILayout.Button("Both x50", _buttonStyle))
                {
                    Cheats.NetworkCheats.SpamChat(_chatMessageInput, 50);
                    Cheats.NetworkCheats.SpamSystemMessage(_chatMessageInput, 50);
                }
                GUILayout.EndHorizontal();

                var players = Cheats.NetworkCheats.GetAllPlayers();
                if (players.Length > 0)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Send as:", _labelStyle, GUILayout.Width(60));
                    foreach (var player in players)
                    {
                        if (GUILayout.Button(player.playerUsername ?? $"P{player.playerClientId}", _buttonStyle))
                        {
                            Cheats.NetworkCheats.ImpersonateInChat(_chatMessageInput, (int)player.playerClientId);
                        }
                    }
                    GUILayout.EndHorizontal();
                }
                GUI.enabled = true;

                DrawHackToggle(Hack.ChatSpam, "Chat Spam", "Keeps sending the message above");
                DrawHackToggle(Hack.JoinSpam, "Join Spam", "Fake 'X joined the game' messages");
            });

            // The ship RPCs below are RequireOwnership = false and work from any client, except
            // ManuallyEjectPlayersServerRpc and SetShipDoorsOverheatServerRpc (owner-only, i.e. host).
            DrawSection("Ship", () =>
            {
                bool isHost = IsLocalHost;

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Force Ship Leave", _buttonStyle))
                {
                    Hack.ForceShipLeave.Execute();
                }
                if (GUILayout.Button("End Round", _buttonStyle))
                {
                    Hack.ForceEnd.Execute();
                }
                GUI.enabled = isHost;
                if (GUILayout.Button(isHost ? "Eject All Players" : "Eject All (host)", _buttonStyle))
                {
                    Hack.EjectAllPlayers.Execute();
                }
                GUI.enabled = true;
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
                GUI.enabled = isHost;
                if (GUILayout.Button(isHost ? "Overheat Doors" : "Overheat Doors (host)", _buttonStyle))
                {
                    Cheats.NetworkCheats.OverheatShipDoors();
                }
                GUI.enabled = true;
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Spin Furniture", _buttonStyle))
                {
                    Cheats.NetworkCheats.SpinShipObjects(5f);
                }
                if (GUILayout.Button("Reset Furniture", _buttonStyle))
                {
                    Cheats.NetworkCheats.ResetAllShipObjects();
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Signal translator:", _labelStyle, GUILayout.Width(110));
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

            // Every VehicleController ServerRpc used here is RequireOwnership = false.
            DrawSection("Cruiser", () =>
            {
                int vehicleCount = LethalMenuMod.Vehicles.Count;
                if (vehicleCount == 0)
                {
                    GUILayout.Label("No Cruiser on the map. Buy one in Terminal > Store.", _labelStyle);
                    return;
                }

                GUILayout.Label($"On map: {vehicleCount}", _labelStyle);

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Hijack", _buttonStyle))
                {
                    Cheats.NetworkCheats.HijackVehicle();
                }
                if (GUILayout.Button("Eject Driver", _buttonStyle))
                {
                    Cheats.NetworkCheats.EjectVehicleDriver();
                }
                if (GUILayout.Button("Explode All", _buttonStyle))
                {
                    Cheats.NetworkCheats.ExplodeAllVehicles();
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
                if (GUILayout.Button("Kill Engine", _buttonStyle))
                {
                    Cheats.NetworkCheats.KillVehicleEngine();
                }
                if (GUILayout.Button("Repair Engine", _buttonStyle))
                {
                    Cheats.NetworkCheats.RepairVehicleEngine();
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Gear:", _labelStyle, GUILayout.Width(40));
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
                GUILayout.Label("Horn:", _labelStyle, GUILayout.Width(40));
                if (GUILayout.Button("ON", _buttonStyle))
                {
                    Cheats.NetworkCheats.ToggleCarHorns(true);
                }
                if (GUILayout.Button("OFF", _buttonStyle))
                {
                    Cheats.NetworkCheats.ToggleCarHorns(false);
                }
                GUILayout.EndHorizontal();
            });

            DrawSection("Level Badge", () =>
            {
                var levelNames = Cheats.NetworkCheats.GetLevelNames();
                int currentLevel = Cheats.NetworkCheats.GetCurrentLevelIndex();
                int currentXP = Cheats.NetworkCheats.GetCurrentXP();

                GUILayout.Label($"Current: {(levelNames.Length > currentLevel ? levelNames[currentLevel] : "?")} ({currentXP} XP)", _labelStyle);

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Max", _buttonStyle))
                {
                    Cheats.NetworkCheats.MaxOutXP();
                }
                if (GUILayout.Button("Reset", _buttonStyle))
                {
                    Cheats.NetworkCheats.SetPlayerXP(0);
                }
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

            DrawSection("Players", DrawPlayersSection);

            // DoorLock.UnlockDoorServerRpc and TerminalAccessibleObject.SetDoorOpenServerRpc are RequireOwnership = false.
            DrawSection("Facility", () =>
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Unlock All Doors", _buttonStyle))
                {
                    Hack.UnlockAllDoors.Execute();
                }
                // DoorLock.LockDoor has no network sync: it only locks doors on this client.
                if (GUILayout.Button("Lock All Doors (local)", _buttonStyle))
                {
                    Cheats.NetworkCheats.LockAllDoors();
                }
                if (GUILayout.Button("Toggle Big Doors", _buttonStyle))
                {
                    Cheats.NetworkCheats.ToggleBigDoors();
                }
                GUILayout.EndHorizontal();

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
                GUILayout.Label("Noise (attracts enemies):", _labelStyle, GUILayout.Width(150));
                if (GUILayout.Button("At Me", _buttonStyle))
                {
                    Cheats.NetworkCheats.MakeNoiseAtMe();
                }
                if (GUILayout.Button("At Camera", _buttonStyle))
                {
                    Cheats.NetworkCheats.MakeNoiseAtCamera();
                }
                GUILayout.EndHorizontal();
            });

            DrawSection("Hazards", () =>
            {
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
                GUILayout.Label("Everyone:", _labelStyle, GUILayout.Width(70));
                if (GUILayout.Button("Mines OFF", _buttonStyle))
                {
                    Cheats.NetworkCheats.ToggleAllLandmines(false);
                }
                if (GUILayout.Button("Mines ON", _buttonStyle))
                {
                    Cheats.NetworkCheats.ToggleAllLandmines(true);
                }
                if (GUILayout.Button("Turrets OFF", _buttonStyle))
                {
                    Cheats.NetworkCheats.ToggleAllTurrets(false);
                }
                if (GUILayout.Button("Turrets ON", _buttonStyle))
                {
                    Cheats.NetworkCheats.ToggleAllTurrets(true);
                }
                GUILayout.EndHorizontal();

                // Local toggles send nothing: hazards stay live for everyone else.
                GUILayout.BeginHorizontal();
                GUILayout.Label("Only me:", _labelStyle, GUILayout.Width(70));
                if (GUILayout.Button("Mines OFF", _buttonStyle))
                {
                    Cheats.NetworkCheats.ToggleAllLandminesLocal(false);
                }
                if (GUILayout.Button("Mines ON", _buttonStyle))
                {
                    Cheats.NetworkCheats.ToggleAllLandminesLocal(true);
                }
                if (GUILayout.Button("Turrets OFF", _buttonStyle))
                {
                    Cheats.NetworkCheats.ToggleAllTurretsLocal(false);
                }
                if (GUILayout.Button("Turrets ON", _buttonStyle))
                {
                    Cheats.NetworkCheats.ToggleAllTurretsLocal(true);
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Blow Up All Mines", _buttonStyle))
                {
                    Hack.BlowUpAllMines.Execute();
                }
                if (GUILayout.Button("Berserk Turrets", _buttonStyle))
                {
                    Hack.BerserkTurrets.Execute();
                }
                GUILayout.EndHorizontal();
            });

            // Time.timeScale is purely local (no host check or RPC involved).
            DrawSection("Game Speed (local)", () =>
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{Time.timeScale:F1}x", _labelStyle, GUILayout.Width(40));
                if (GUILayout.Button("0.5x", _buttonStyle)) Cheats.NetworkCheats.SetTimescale(0.5f);
                if (GUILayout.Button("1x", _buttonStyle)) Cheats.NetworkCheats.SetTimescale(1f);
                if (GUILayout.Button("2x", _buttonStyle)) Cheats.NetworkCheats.SetTimescale(2f);
                if (GUILayout.Button("5x", _buttonStyle)) Cheats.NetworkCheats.SetTimescale(5f);
                GUILayout.EndHorizontal();
            });

            DrawSection("Trolling", () =>
            {
                GUILayout.Label("Continuous", _labelStyle);

                GUILayout.BeginHorizontal();
                DrawHackToggle(Hack.HornSpam, "Ship Horn", null);
                DrawHackToggle(Hack.CarHornSpam, "Cruiser Horns", null);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                DrawHackToggle(Hack.DoorSpam, "Ship Doors", null);
                DrawHackToggle(Hack.DeskDoorSpam, "Company Desk Door", null);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                DrawHackToggle(Hack.SignalSpam, "Signal Translator", null);
                DrawHackToggle(Hack.EarrapeSpam, "Terminal Earrape", null);
                GUILayout.EndHorizontal();

                DrawHackToggle(Hack.RPCLagSpam, "RPC Lag", "Floods the host with RPCs");

                GUILayout.Space(5);
                GUILayout.Label("One-shot", _labelStyle);

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Flicker Ship Lights", _buttonStyle))
                {
                    Hack.FlickerLights.Execute();
                }
                if (GUILayout.Button("Horn + Lights + Doors + Signal", _buttonStyle))
                {
                    Hack.MaxChaos.Execute();
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Fire All Shotguns", _buttonStyle))
                {
                    Cheats.NetworkCheats.ShootAllShotguns();
                }
                if (GUILayout.Button("Fire All Shotguns x10", _buttonStyle))
                {
                    Cheats.NetworkCheats.SpamShootAllShotguns(10);
                }
                if (GUILayout.Button("Explode Jetpacks", _buttonStyle))
                {
                    Cheats.NetworkCheats.ExplodeAllJetpacks();
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Company Desk Attack", _buttonStyle))
                {
                    Cheats.NetworkCheats.ForceTentacleAttack();
                }
                if (GUILayout.Button("Bracken Lag (everyone)", _buttonStyle))
                {
                    Cheats.NetworkCheats.BrackenLagAllPlayers();
                }
                GUILayout.EndHorizontal();
            });
        }

        /// One target selector for every per-player action, then the actions that hit everybody.
        private void DrawPlayersSection()
        {
            var players = Cheats.NetworkCheats.GetAllPlayers();
            if (players.Length == 0)
            {
                GUILayout.Label("No players found.", _labelStyle);
                return;
            }

            _selectedPlayerIndex = Mathf.Clamp(_selectedPlayerIndex, 0, players.Length - 1);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Target:", _labelStyle, GUILayout.Width(50));
            if (GUILayout.Button("<", _buttonStyle, GUILayout.Width(30)))
                _selectedPlayerIndex = (_selectedPlayerIndex - 1 + players.Length) % players.Length;
            GUILayout.Label(players[_selectedPlayerIndex].playerUsername ?? "Unknown", _labelStyle, GUILayout.Width(120));
            if (GUILayout.Button(">", _buttonStyle, GUILayout.Width(30)))
                _selectedPlayerIndex = (_selectedPlayerIndex + 1) % players.Length;
            GUILayout.EndHorizontal();

            var target = players[_selectedPlayerIndex];
            var local = LethalMenuMod.LocalPlayer;
            bool targetIsSelf = target == local;
            bool isHost = IsLocalHost;

            // Health
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Damage 10", _buttonStyle))
            {
                Cheats.NetworkCheats.DamagePlayer(target, 10);
            }
            if (GUILayout.Button("Damage 50", _buttonStyle))
            {
                Cheats.NetworkCheats.DamagePlayer(target, 50);
            }
            if (GUILayout.Button("Kill", _buttonStyle))
            {
                Cheats.NetworkCheats.DamagePlayer(target, 100);
            }
            if (GUILayout.Button("Heal", _buttonStyle))
            {
                Cheats.NetworkCheats.HealPlayer(target);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            bool targetDemiGod = Settings.IsDemiGod(target);
            bool newTargetDemiGod = GUILayout.Toggle(targetDemiGod, "Demi-God (keep healed)", _buttonStyle);
            if (newTargetDemiGod != targetDemiGod)
                Settings.SetDemiGod(target, newTargetDemiGod);
            if (GUILayout.Button("Force Drop Items", _buttonStyle))
            {
                Cheats.NetworkCheats.ForceDropItems(target);
            }
            GUILayout.EndHorizontal();

            if (!targetIsSelf)
            {
                // Enemies. MOB uses EnemyAI.ChangeEnemyOwnerServerRpc (RequireOwnership = false); Bomb and Lag
                // spawn objects, which only the server can do.
                bool isFriend = Settings.IsFriend(target);
                bool newFriend = DrawToggle("Friend", isFriend, "Escorts ignore friends; friends can't be hunted");
                if (newFriend != isFriend)
                    Settings.SetFriend(target, newFriend);

                GUILayout.BeginHorizontal();
                GUI.enabled = Cheats.Directives.DirectiveTargeting.IsValidTarget(target);
                if (GUILayout.Button("Hunt (all enemies)", _buttonStyle))
                {
                    int sent = Cheats.Directives.EnemyDirector.HuntAll(target);
                    HUDManager.Instance?.DisplayTip("Hunt", $"{sent} enemies hunting {target.playerUsername}.");
                }
                GUI.enabled = true;
                if (GUILayout.Button("Mob", _buttonStyle))
                {
                    Cheats.NetworkCheats.MobPlayer(target, false);
                }
                if (GUILayout.Button("Mob + Teleport", _buttonStyle))
                {
                    Cheats.NetworkCheats.MobPlayer(target, true);
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUI.enabled = isHost;
                if (GUILayout.Button(isHost ? "Jetpack Bomb" : "Jetpack Bomb (host)", _buttonStyle))
                {
                    Cheats.NetworkCheats.BombPlayer(target);
                }
                if (GUILayout.Button(isHost ? "Bracken Lag" : "Bracken Lag (host)", _buttonStyle))
                {
                    Cheats.NetworkCheats.LagPlayer(target);
                }
                GUI.enabled = true;
                GUILayout.EndHorizontal();

                bool following = Hack.FollowPlayer.IsEnabled() && Cheats.FollowCheat.TargetPlayer == target;
                bool newFollowing = DrawToggle("Follow", following, $"Walk behind them, {Settings.FollowDelaySeconds:F1}s behind");
                if (newFollowing != following)
                {
                    Cheats.FollowCheat.TargetPlayer = target;
                    Hack.FollowPlayer.SetEnabled(newFollowing);
                }
                if (following)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"  Delay: {Settings.FollowDelaySeconds:F1}s", _labelStyle, GUILayout.Width(90));
                    Settings.FollowDelaySeconds = GUILayout.HorizontalSlider(Settings.FollowDelaySeconds, 0.2f, 3.0f);
                    GUILayout.EndHorizontal();
                }

                // Spin is drawn on this client only.
                GUILayout.BeginHorizontal();
                GUILayout.Label("Spin (local):", _labelStyle, GUILayout.Width(80));
                Settings.SpinCamera = GUILayout.Toggle(Settings.SpinCamera, "Camera", _toggleStyle, GUILayout.Width(70));
                Settings.SpinModel = GUILayout.Toggle(Settings.SpinModel, "Model", _toggleStyle, GUILayout.Width(60));
                GUILayout.Label($"{Settings.SpinDuration:F0}s", _labelStyle, GUILayout.Width(30));
                if (GUILayout.Button("-", _buttonStyle, GUILayout.Width(25)))
                    Settings.SpinDuration = Mathf.Max(1f, Settings.SpinDuration - 5f);
                if (GUILayout.Button("+", _buttonStyle, GUILayout.Width(25)))
                    Settings.SpinDuration = Mathf.Min(60f, Settings.SpinDuration + 5f);
                bool isSpinning = Cheats.NetworkCheats.IsSpinning(target);
                if (GUILayout.Button(isSpinning ? "Stop" : "Start", _buttonStyle, GUILayout.Width(50)))
                {
                    if (isSpinning)
                        Cheats.NetworkCheats.StopSpinPlayer(target);
                    else
                        Cheats.NetworkCheats.SpinPlayer(target, Settings.SpinDuration, Settings.SpinCamera, Settings.SpinModel);
                }
                GUILayout.EndHorizontal();
            }

            // Teleports
            GUILayout.Space(5);
            if (!targetIsSelf && local != null)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Teleport Me To Them", _buttonStyle))
                {
                    Cheats.NetworkCheats.TeleportPlayerToPlayer(local, target);
                }
                if (GUILayout.Button("Teleport Them To Me", _buttonStyle))
                {
                    Cheats.NetworkCheats.TeleportPlayerToPosition(target, local.transform.position);
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Random Inside", _buttonStyle))
            {
                Cheats.NetworkCheats.TeleportPlayerRandom(target, true);
            }
            if (GUILayout.Button("Random Outside", _buttonStyle))
            {
                Cheats.NetworkCheats.TeleportPlayerRandom(target, false);
            }
            if (GUILayout.Button("Void", _buttonStyle))
            {
                Cheats.NetworkCheats.TeleportPlayerToVoid(target);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (players.Length >= 2)
            {
                var other = players[(_selectedPlayerIndex + 1) % players.Length];
                if (GUILayout.Button($"Swap With {other.playerUsername}", _buttonStyle))
                {
                    Cheats.NetworkCheats.SwapPlayerPositions(target, other);
                }
            }
            // The ship teleporter and entrances move the target on their own client too.
            if (GUILayout.Button("Beam To Ship", _buttonStyle))
            {
                Cheats.NetworkCheats.TeleportPlayerViaShipTeleporter(target);
            }
            if (GUILayout.Button("Entrance Teleporter…", _buttonStyle))
            {
                EntranceTeleporter.Show(target);
            }
            GUILayout.EndHorizontal();

            if (!targetIsSelf)
                GUILayout.Label(RemoteTeleportNote, new GUIStyle(_tooltipStyle) { wordWrap = true });

            // Everybody
            GUILayout.Space(5);
            GUILayout.Label("All players", _labelStyle);
            GUILayout.BeginHorizontal();
            // Debug_ReviveAllPlayersServerRpc is owner-only (host).
            GUI.enabled = isHost;
            if (GUILayout.Button(isHost ? "Revive All" : "Revive All (host)", _buttonStyle))
            {
                Hack.ReviveAllPlayers.Execute();
            }
            GUI.enabled = true;
            if (GUILayout.Button("Teleport All To Me", _buttonStyle))
            {
                Hack.TeleportAllToMe.Execute();
            }
            if (GUILayout.Button("Kill All", _buttonStyle))
            {
                Cheats.NetworkCheats.MassKillPlayers();
            }
            GUILayout.EndHorizontal();
        }

        #endregion
    }
}
