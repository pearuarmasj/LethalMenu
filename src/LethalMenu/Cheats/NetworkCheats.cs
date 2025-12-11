using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using GameNetcodeStuff;
using LethalMenu.Util;
using Unity.Netcode;
using UnityEngine;

namespace LethalMenu.Cheats
{
    /// Network exploits using ServerRpc/ClientRpc methods.
    /// These call game network methods to achieve effects across all players.
    /// Uses reflection to call private RPCs when needed.
    public static class NetworkCheats
    {
        #region Credits & Shop Exploits

        /// Sets group credits to any value by calling SyncGroupCreditsServerRpc.
        public static void SetCredits(int amount)
        {
            var terminal = UnityEngine.Object.FindObjectOfType<Terminal>();
            if (terminal == null)
            {
                Debug.Log("[NetworkCheats] Terminal not found.");
                return;
            }

            terminal.groupCredits = amount;
            terminal.SyncGroupCreditsServerRpc(amount, terminal.numberOfItemsInDropship);
            Debug.Log($"[NetworkCheats] Set credits to ${amount}");
        }

        /// Unlocks a ship upgrade for free by calling BuyShipUnlockableServerRpc with current credits.
        public static void UnlockShipUpgrade(int unlockableId)
        {
            var startOfRound = StartOfRound.Instance;
            var terminal = UnityEngine.Object.FindObjectOfType<Terminal>();
            if (startOfRound == null || terminal == null)
            {
                Debug.Log("[NetworkCheats] Not in game.");
                return;
            }

            // Buy the unlockable but keep current credits
            startOfRound.BuyShipUnlockableServerRpc(unlockableId, terminal.groupCredits);
            Debug.Log($"[NetworkCheats] Unlocked ship upgrade ID: {unlockableId}");
        }

        /// Buys items without spending credits.
        public static void FreeItems(int[] itemIds)
        {
            var terminal = UnityEngine.Object.FindObjectOfType<Terminal>();
            if (terminal == null)
            {
                Debug.Log("[NetworkCheats] Terminal not found.");
                return;
            }

            terminal.BuyItemsServerRpc(itemIds, terminal.groupCredits, 0);
            Debug.Log($"[NetworkCheats] Purchased {itemIds.Length} items for free.");
        }

        #endregion

        #region Player Effects (Malicious)

        /// Damages another player using DamagePlayerFromOtherClientServerRpc.
        public static void DamagePlayer(PlayerControllerB target, int damage = 10)
        {
            if (target == null || target.isPlayerDead) return;

            var localPlayer = LethalMenuMod.LocalPlayer;
            if (localPlayer == null) return;

            // Call the server RPC to damage the player
            target.DamagePlayerFromOtherClientServerRpc(damage, Vector3.zero, (int)localPlayer.playerClientId);
            Debug.Log($"[NetworkCheats] Damaged {target.playerUsername} for {damage} damage.");
        }

        /// Forces a player to drop all held items using DropAllHeldItemsServerRpc.
        public static void ForceDropItems(PlayerControllerB target)
        {
            if (target == null || target.isPlayerDead) return;

            // Need to call this on the player's controller
            // Since we can't call ServerRpc directly on other players' objects,
            // we attempt via reflection or use alternative methods
            try
            {
                // Direct call attempt - may only work if we have permission
                target.DropAllHeldItemsServerRpc();
                Debug.Log($"[NetworkCheats] Forced {target.playerUsername} to drop items.");
            }
            catch (Exception e)
            {
                Debug.Log($"[NetworkCheats] Failed to force drop: {e.Message}");
            }
        }

        /// Heals a player to full health. Works on ANY player using negative damage exploit.
        /// If host: Uses DamagePlayerServerRpc to set health directly.
        /// If client: Uses DamagePlayerFromOtherClientServerRpc with -100 damage.
        public static void HealPlayer(PlayerControllerB target)
        {
            if (target == null || target.isPlayerDead)
            {
                Debug.Log("[NetworkCheats] HealPlayer: Target is null or dead.");
                return;
            }

            var localPlayer = LethalMenuMod.LocalPlayer;
            if (localPlayer == null)
            {
                Debug.Log("[NetworkCheats] HealPlayer: Local player not found.");
                return;
            }

            bool isHost = GameNetworkManager.Instance?.isHostingGame == true || localPlayer.IsServer;
            
            if (isHost)
            {
                // As host, use DamagePlayerServerRpc(0, 100) to set health to 100
                target.DamagePlayerServerRpc(0, 100);
                Debug.Log($"[NetworkCheats] Healed {target.playerUsername} via DamagePlayerServerRpc (host).");
            }
            else
            {
                // As client, use negative damage to heal
                // DamagePlayerFromOtherClientServerRpc with -100 damage = +100 health
                target.DamagePlayerFromOtherClientServerRpc(-100, UnityEngine.Vector3.zero, (int)localPlayer.playerClientId);
                Debug.Log($"[NetworkCheats] Healed {target.playerUsername} via negative damage exploit.");
            }
        }

        /// Heals the local player to full health.
        public static void HealSelf()
        {
            var localPlayer = LethalMenuMod.LocalPlayer;
            if (localPlayer == null || localPlayer.isPlayerDead)
            {
                Debug.Log("[NetworkCheats] HealSelf: Local player null or dead.");
                return;
            }
            
            HealPlayer(localPlayer);
        }

        #endregion

        #region Chat Exploits

        /// Sends a chat message as any player (spoofed chat).
        /// Uses the public AddTextToChatOnServer method (like lc-hax).
        /// Max 50 characters enforced by server.
        public static void SendChatMessage(string message, int playerIndex = -1)
        {
            var hud = HUDManager.Instance;
            if (hud == null)
            {
                Debug.Log("[NetworkCheats] HUD not found.");
                return;
            }

            // If no player index specified, use local player
            if (playerIndex < 0)
            {
                var local = LethalMenuMod.LocalPlayer;
                if (local != null)
                    playerIndex = (int)local.playerClientId;
            }

            // Truncate to 50 characters (server limit)
            if (message.Length > 50)
                message = message.Substring(0, 50);

            // Use PUBLIC method AddTextToChatOnServer (like lc-hax does)
            // This is the proper way to send chat, calls ServerRpc internally
            hud.AddTextToChatOnServer(message, playerIndex);
            Debug.Log($"[NetworkCheats] Sent chat as player {playerIndex}: {message}");
        }

        /// Sends a system text message (no player name attached).
        public static void SendSystemMessage(string message)
        {
            var hud = HUDManager.Instance;
            if (hud == null)
            {
                Debug.Log("[NetworkCheats] HUD not found.");
                return;
            }

            // playerId = -1 sends as system message (no player name)
            hud.AddTextToChatOnServer(message, -1);
            Debug.Log($"[NetworkCheats] Sent system message: {message}");
        }

        /// Spam system messages (uses coroutine for proper timing).
        /// Each message must be unique to avoid deduplication.
        public static void SpamSystemMessage(string message, int count = 10)
        {
            if (LethalMenuMod.Instance != null)
            {
                LethalMenuMod.Instance.StartCoroutine(SpamSystemMessageCoroutine(message, count));
            }
        }

        /// Coroutine that sends system message spam with unique suffixes.
        public static IEnumerator SpamSystemMessageCoroutine(string message, int count)
        {
            var hud = HUDManager.Instance;
            if (hud == null) yield break;

            // Calculate max base message length
            int maxSuffixLen = $" [{count}]".Length;
            int maxBaseLen = 50 - maxSuffixLen;
            string baseMsg = message.Length > maxBaseLen ? message.Substring(0, maxBaseLen) : message;

            for (int i = 0; i < count; i++)
            {
                // Each message MUST be unique or server may dedupe
                string uniqueMsg = $"{baseMsg} [{i+1}]";
                hud.AddTextToChatOnServer(uniqueMsg, -1); // -1 = system message
                
                // Small delay to avoid rate limiting
                yield return new WaitForSeconds(0.15f);
            }
            Debug.Log($"[NetworkCheats] Spammed system messages {count} times.");
        }

        /// Spam chat messages (uses coroutine for proper timing).
        /// Each message must be unique to avoid deduplication.
        public static void SpamChat(string message, int count = 10)
        {
            if (LethalMenuMod.Instance != null)
            {
                LethalMenuMod.Instance.StartCoroutine(SpamChatCoroutine(message, count));
            }
        }
        
        /// Coroutine that sends spam messages with unique suffixes.
        public static IEnumerator SpamChatCoroutine(string message, int count)
        {
            var hud = HUDManager.Instance;
            if (hud == null) yield break;

            var local = LethalMenuMod.LocalPlayer;
            int playerIndex = local != null ? (int)local.playerClientId : 0;

            // Calculate max base message length (50 - max suffix length)
            int maxSuffixLen = $" [{count}]".Length;
            int maxBaseLen = 50 - maxSuffixLen;
            string baseMsg = message.Length > maxBaseLen ? message.Substring(0, maxBaseLen) : message;

            for (int i = 0; i < count; i++)
            {
                // Each message MUST be unique or server may dedupe
                string uniqueMsg = $"{baseMsg} [{i+1}]";
                hud.AddTextToChatOnServer(uniqueMsg, playerIndex);
                
                // Small delay to avoid rate limiting
                yield return new WaitForSeconds(0.15f);
            }
            Debug.Log($"[NetworkCheats] Spammed chat {count} times.");
        }

        /// Max spam - sends 50 messages very fast.
        public static void SpamChatMax(string message)
        {
            SpamChat(message, 50);
        }

        #endregion

        #region Ship & Level Control

        /// Forces ship to leave early using SetShipLeaveEarlyServerRpc.
        public static void ForceShipLeave()
        {
            var timeOfDay = TimeOfDay.Instance;
            if (timeOfDay == null)
            {
                Debug.Log("[NetworkCheats] TimeOfDay not found.");
                return;
            }

            timeOfDay.SetShipLeaveEarlyServerRpc();
            Debug.Log("[NetworkCheats] Forced ship to leave early.");
        }

        /// Toggles ship lights on/off for everyone.
        public static void ToggleShipLights(bool on)
        {
            var shipLights = UnityEngine.Object.FindObjectOfType<ShipLights>();
            if (shipLights == null)
            {
                Debug.Log("[NetworkCheats] ShipLights not found.");
                return;
            }

            shipLights.SetShipLightsServerRpc(on);
            Debug.Log($"[NetworkCheats] Ship lights set to {(on ? "ON" : "OFF")}.");
        }

        /// Changes destination to a different moon (requires host or appropriate permissions).
        public static void ChangeLevel(int levelId)
        {
            var startOfRound = StartOfRound.Instance;
            var terminal = UnityEngine.Object.FindObjectOfType<Terminal>();
            if (startOfRound == null || terminal == null)
            {
                Debug.Log("[NetworkCheats] Not in game.");
                return;
            }

            startOfRound.ChangeLevelServerRpc(levelId, terminal.groupCredits);
            Debug.Log($"[NetworkCheats] Changing to level ID: {levelId}");
        }

        /// Forces all players to eject/leave (usually used when in orbit).
        public static void EjectAllPlayers()
        {
            var startOfRound = StartOfRound.Instance;
            if (startOfRound == null)
            {
                Debug.Log("[NetworkCheats] Not in game.");
                return;
            }

            startOfRound.ManuallyEjectPlayersServerRpc();
            Debug.Log("[NetworkCheats] Ejecting all players.");
        }

        /// Toggles the ship's magnet on/off.
        public static void ToggleMagnet(bool on)
        {
            var startOfRound = StartOfRound.Instance;
            if (startOfRound == null)
            {
                Debug.Log("[NetworkCheats] Not in game.");
                return;
            }

            startOfRound.SetMagnetOnServerRpc(on);
            Debug.Log($"[NetworkCheats] Magnet set to {(on ? "ON" : "OFF")}.");
        }

        #endregion

        #region Enemy Control

        /// Kills an enemy remotely using KillEnemyServerRpc.
        public static void KillEnemy(EnemyAI enemy)
        {
            if (enemy == null || enemy.isEnemyDead) return;

            try
            {
                enemy.KillEnemyServerRpc(true);
                Debug.Log($"[NetworkCheats] Killed enemy: {enemy.enemyType?.enemyName ?? "Unknown"}");
            }
            catch (Exception e)
            {
                Debug.Log($"[NetworkCheats] Failed to kill enemy: {e.Message}");
            }
        }

        /// Kills all enemies on the map.
        public static void KillAllEnemies()
        {
            int killed = 0;
            foreach (var enemy in LethalMenuMod.Enemies)
            {
                if (enemy != null && !enemy.isEnemyDead)
                {
                    try
                    {
                        enemy.KillEnemyServerRpc(true);
                        killed++;
                    }
                    catch { }
                }
            }
            Debug.Log($"[NetworkCheats] Killed {killed} enemies.");
        }

        #endregion

        #region Teleportation

        /// Teleports a player to an entrance using the game's EntranceTeleport system.
        /// This calls the ServerRpc which teleports them properly for all clients.
        /// <param name="target">Player to teleport</param>
        /// <param name="toMainEntrance">If true, teleport to main entrance. If false, teleport to fire exit.</param>
        public static void TeleportPlayerToEntrance(PlayerControllerB target, bool toMainEntrance = true)
        {
            if (target == null || target.isPlayerDead)
            {
                Debug.Log("[NetworkCheats] TeleportPlayerToEntrance: Target is null or dead.");
                return;
            }

            var entrances = UnityEngine.Object.FindObjectsOfType<EntranceTeleport>();
            if (entrances == null || entrances.Length == 0)
            {
                Debug.Log("[NetworkCheats] TeleportPlayerToEntrance: No entrances found.");
                return;
            }

            // isEntranceToBuilding = true means it's the door that leads INTO the building
            // We want to find the entrance, then use its ServerRpc to teleport to exit point
            // For main entrance: find entrance with entranceId == 0
            // For fire exit: find entrance with entranceId != 0
            EntranceTeleport? entrance = null;
            if (toMainEntrance)
            {
                entrance = entrances.FirstOrDefault(e => e.entranceId == 0);
            }
            else
            {
                entrance = entrances.FirstOrDefault(e => e.entranceId != 0);
            }

            if (entrance == null)
            {
                // Fallback: try any entrance
                entrance = entrances.FirstOrDefault();
            }

            if (entrance == null)
            {
                Debug.Log("[NetworkCheats] TeleportPlayerToEntrance: No suitable entrance found.");
                return;
            }

            try
            {
                entrance.TeleportPlayerServerRpc((int)target.playerClientId);
                Debug.Log($"[NetworkCheats] Teleported {target.playerUsername} to {(toMainEntrance ? "main entrance" : "fire exit")}.");
            }
            catch (Exception e)
            {
                Debug.Log($"[NetworkCheats] TeleportPlayerToEntrance failed: {e.Message}");
            }
        }

        /// Teleports a player to the ship using direct position teleport.
        /// Only works reliably for local player.
        public static void TeleportToShip(PlayerControllerB? target = null)
        {
            target ??= LethalMenuMod.LocalPlayer;
            if (target == null || target.isPlayerDead)
            {
                Debug.Log("[NetworkCheats] TeleportToShip: Target is null or dead.");
                return;
            }

            var startOfRound = StartOfRound.Instance;
            if (startOfRound == null || startOfRound.playerSpawnPositions == null || startOfRound.playerSpawnPositions.Length == 0)
            {
                Debug.Log("[NetworkCheats] TeleportToShip: Ship spawn positions not found.");
                return;
            }

            Vector3 shipPos = startOfRound.playerSpawnPositions[0].position;
            
            // Only local player can be directly teleported
            if (target == LethalMenuMod.LocalPlayer)
            {
                target.TeleportPlayer(shipPos);
                target.isInElevator = false;
                target.isInHangarShipRoom = true;
                target.isInsideFactory = false;
                Debug.Log($"[NetworkCheats] Teleported {target.playerUsername} to ship.");
            }
            else
            {
                Debug.Log("[NetworkCheats] TeleportToShip: Can only teleport local player directly. Use TeleportPlayerViaShipTeleporter for other players.");
            }
        }

        /// Teleports ANY player to the ship using the ship teleporter item.
        /// Requires the ship to have a normal teleporter (not inverse).
        /// Works by switching radar target and pressing the teleport button.
        public static void TeleportPlayerViaShipTeleporter(PlayerControllerB target)
        {
            if (target == null || target.isPlayerDead)
            {
                Debug.Log("[NetworkCheats] TeleportPlayerViaShipTeleporter: Target is null or dead.");
                return;
            }

            // Find the ship teleporter (not inverse)
            var teleporters = UnityEngine.Object.FindObjectsOfType<ShipTeleporter>();
            var normalTeleporter = teleporters.FirstOrDefault(t => t != null && !t.isInverseTeleporter);

            if (normalTeleporter == null)
            {
                Debug.Log("[NetworkCheats] TeleportPlayerViaShipTeleporter: No ship teleporter found. Buy one from the store.");
                return;
            }

            // Check cooldown
            if (normalTeleporter.cooldownTime > 0f)
            {
                Debug.Log($"[NetworkCheats] TeleportPlayerViaShipTeleporter: Teleporter on cooldown ({normalTeleporter.cooldownTime:F1}s remaining).");
                return;
            }

            var startOfRound = StartOfRound.Instance;
            if (startOfRound?.mapScreen == null)
            {
                Debug.Log("[NetworkCheats] TeleportPlayerViaShipTeleporter: Map screen not found.");
                return;
            }

            // Find the player's radar target index
            int targetIndex = -1;
            for (int i = 0; i < startOfRound.mapScreen.radarTargets.Count; i++)
            {
                if (startOfRound.mapScreen.radarTargets[i].transform == target.transform)
                {
                    targetIndex = i;
                    break;
                }
            }

            if (targetIndex == -1)
            {
                Debug.Log($"[NetworkCheats] TeleportPlayerViaShipTeleporter: {target.playerUsername} not found in radar targets.");
                return;
            }

            // Switch radar target to the player and use teleporter
            if (LethalMenuMod.Instance != null)
            {
                LethalMenuMod.Instance.StartCoroutine(TeleportPlayerCoroutine(target, normalTeleporter, targetIndex));
            }
        }

        /// Coroutine that switches radar target and teleports.
        private static IEnumerator TeleportPlayerCoroutine(PlayerControllerB target, ShipTeleporter teleporter, int targetIndex)
        {
            var startOfRound = StartOfRound.Instance;
            if (startOfRound?.mapScreen == null) yield break;

            // Switch radar target
            startOfRound.mapScreen.SwitchRadarTargetAndSync(targetIndex);
            Debug.Log($"[NetworkCheats] Switched radar target to {target.playerUsername}.");

            // Wait a frame for the radar to update
            yield return null;
            yield return null;

            // Press teleport button
            teleporter.PressTeleportButtonServerRpc();
            Debug.Log($"[NetworkCheats] Teleported {target.playerUsername} to ship via teleporter.");
        }

        /// Teleports local player to a specific position.
        public static void TeleportToPosition(Vector3 position)
        {
            var localPlayer = LethalMenuMod.LocalPlayer;
            if (localPlayer == null || localPlayer.isPlayerDead)
            {
                Debug.Log("[NetworkCheats] TeleportToPosition: Local player is null or dead.");
                return;
            }

            localPlayer.TeleportPlayer(position);
            Debug.Log($"[NetworkCheats] Teleported to position {position}.");
        }

        #endregion

        #region Terminal Audio

        /// Plays terminal audio for everyone (trolling).
        public static void PlayTerminalSound(int clipIndex)
        {
            var terminal = UnityEngine.Object.FindObjectOfType<Terminal>();
            if (terminal == null)
            {
                Debug.Log("[NetworkCheats] Terminal not found.");
                return;
            }

            terminal.PlayTerminalAudioServerRpc(clipIndex);
            Debug.Log($"[NetworkCheats] Playing terminal sound {clipIndex}.");
        }

        #endregion

        #region Utility Methods

        /// Gets all available ship unlockables and their IDs.
        public static (int id, string name, bool unlocked)[] GetShipUnlockables()
        {
            var startOfRound = StartOfRound.Instance;
            if (startOfRound == null || startOfRound.unlockablesList == null)
                return Array.Empty<(int, string, bool)>();

            var unlockables = startOfRound.unlockablesList.unlockables;
            var result = new (int, string, bool)[unlockables.Count];

            for (int i = 0; i < unlockables.Count; i++)
            {
                var u = unlockables[i];
                result[i] = (i, u.unlockableName ?? $"Unlockable {i}", u.hasBeenUnlockedByPlayer);
            }

            return result;
        }

        /// Gets all available levels (moons) and their IDs.
        public static (int id, string name)[] GetAvailableLevels()
        {
            var startOfRound = StartOfRound.Instance;
            if (startOfRound == null || startOfRound.levels == null)
                return Array.Empty<(int, string)>();

            var result = new (int, string)[startOfRound.levels.Length];
            for (int i = 0; i < startOfRound.levels.Length; i++)
            {
                result[i] = (i, startOfRound.levels[i]?.PlanetName ?? $"Level {i}");
            }
            return result;
        }

        /// Gets all connected players.
        public static PlayerControllerB[] GetAllPlayers()
        {
            return LethalMenuMod.Players.Where(p => p != null && !p.isPlayerDead).ToArray();
        }

        /// Gets all alive enemies.
        public static EnemyAI[] GetAllEnemies()
        {
            return LethalMenuMod.Enemies.Where(e => e != null && !e.isEnemyDead).ToArray();
        }

        /// Check if we're the host (have more permissions).
        public static bool IsHost()
        {
            return NetworkManager.Singleton?.IsHost ?? false;
        }

        #endregion

        #region Malicious Exploits (Use Responsibly)

        /// Attempts to crash other players by sending invalid terminal audio index.
        /// NO bounds checking on syncedAudios array - IndexOutOfRangeException.
        /// 
        /// Attempts to crash/lag other players by sending various invalid data.
        /// May cause IndexOutOfRangeException or other issues on clients.
        public static void AttemptTerminalCrash()
        {
            var terminal = UnityEngine.Object.FindObjectOfType<Terminal>();
            if (terminal == null)
            {
                Debug.Log("[NetworkCheats] Terminal not found.");
                return;
            }

            // Send invalid indices that will cause array bounds exception
            terminal.PlayTerminalAudioServerRpc(-1);
            terminal.PlayTerminalAudioServerRpc(int.MaxValue);
            terminal.PlayTerminalAudioServerRpc(-99999);
            Debug.Log("[NetworkCheats] Sent invalid terminal audio indices.");

            // Also try spamming valid audio to annoy
            for (int i = 0; i < 20; i++)
            {
                terminal.PlayTerminalAudioServerRpc(0);
            }
            Debug.Log("[NetworkCheats] Spammed terminal audio.");
        }

        /// Aggressive RPC spam to cause network lag.
        /// Spams multiple RPC types simultaneously.
        public static void LagAllPlayers(int iterations = 50)
        {
            if (LethalMenuMod.Instance != null)
            {
                LethalMenuMod.Instance.StartCoroutine(LagPlayersCoroutine(iterations));
            }
        }
        
        /// Coroutine that spams various RPCs aggressively to cause lag.
        public static IEnumerator LagPlayersCoroutine(int iterations)
        {
            var hud = HUDManager.Instance;
            var localPlayer = LethalMenuMod.LocalPlayer;
            var terminal = UnityEngine.Object.FindObjectOfType<Terminal>();
            
            if (localPlayer == null) yield break;

            Debug.Log($"[NetworkCheats] Starting lag spam ({iterations} iterations)...");

            for (int i = 0; i < iterations; i++)
            {
                // Signal translator spam (server-wide notification)
                if (hud != null)
                {
                    hud.UseSignalTranslatorServerRpc($"L{i % 100:D2}");
                }

                // Terminal audio spam
                if (terminal != null)
                {
                    terminal.PlayTerminalAudioServerRpc(0);
                }

                // Ship horn/alarm spam
                if (hud != null && i % 3 == 0)
                {
                    hud.AlarmHornServerRpc();
                }

                // Chat spam with invisible messages
                if (hud != null)
                {
                    hud.AddTextToChatOnServer($"<size=0>{i}</size>");
                }

                // Self-damage spam (causes visual updates for all)
                if (!localPlayer.isPlayerDead && i % 5 == 0)
                {
                    localPlayer.DamagePlayerFromOtherClientServerRpc(0, UnityEngine.Vector3.zero, -1);
                }

                yield return null; // Next frame
            }

            Debug.Log($"[NetworkCheats] Sent ~{iterations * 5} lag packets.");
        }

        /// PROPER Bracken lag attack - transfers enemy ownership to target player
        /// causing massive pathfinding lag on their client.
        /// Requires a Bracken (Flowerman) to be spawned.
        public static void BrackenLagPlayer(PlayerControllerB targetPlayer)
        {
            if (targetPlayer == null || targetPlayer.isPlayerDead)
            {
                Debug.Log("[NetworkCheats] Invalid target player.");
                return;
            }

            // Find a Bracken (FlowermanAI)
            var bracken = UnityEngine.Object.FindObjectOfType<FlowermanAI>();
            if (bracken == null)
            {
                Debug.Log("[NetworkCheats] No Bracken spawned - cannot use this attack.");
                return;
            }

            if (targetPlayer.isInsideFactory)
            {
                Debug.Log("[NetworkCheats] Target must be outside factory for max effect.");
            }

            if (LethalMenuMod.Instance != null)
            {
                LethalMenuMod.Instance.StartCoroutine(BrackenLagCoroutine(targetPlayer, bracken));
            }
        }

        /// Coroutine that performs the Bracken ownership transfer lag attack.
        private static IEnumerator BrackenLagCoroutine(PlayerControllerB targetPlayer, FlowermanAI bracken)
        {
            var localPlayer = LethalMenuMod.LocalPlayer;
            if (localPlayer == null) yield break;

            // Step 1: Take ownership of the Bracken
            Debug.Log("[NetworkCheats] Taking Bracken ownership...");
            for (int i = 0; i < 30; i++)
            {
                if (bracken.currentOwnershipOnThisClient == (int)localPlayer.playerClientId) break;
                bracken.ChangeEnemyOwnerServerRpc(localPlayer.actualClientId);
                yield return null;
            }

            // Step 2: Set Bracken to target the player and enter max anger mode
            bracken.SetMovingTowardsTargetPlayer(targetPlayer);
            bracken.SwitchToBehaviourState(2); // Aggravated/chase mode
            bracken.EnterAngerModeServerRpc(float.MaxValue); // Max anger = constant pathfinding
            bracken.UpdateEnemyPositionServerRpc(targetPlayer.transform.position);

            Debug.Log("[NetworkCheats] Bracken aggravated, transferring ownership to target...");

            // Step 3: Transfer ownership to target player - THIS causes the lag
            // The target's client now has to compute pathfinding for an enraged Bracken
            for (int i = 0; i < 30; i++)
            {
                if (bracken.currentOwnershipOnThisClient == (int)targetPlayer.playerClientId) break;
                bracken.ChangeEnemyOwnerServerRpc(targetPlayer.actualClientId);
                yield return null;
            }

            Debug.Log($"[NetworkCheats] Bracken lag attack executed on {targetPlayer.playerUsername}.");
        }

        /// Lag ALL players using Bracken ownership spam.
        public static void BrackenLagAllPlayers()
        {
            var bracken = UnityEngine.Object.FindObjectOfType<FlowermanAI>();
            if (bracken == null)
            {
                Debug.Log("[NetworkCheats] No Bracken spawned.");
                return;
            }

            if (LethalMenuMod.Instance != null)
            {
                LethalMenuMod.Instance.StartCoroutine(BrackenLagAllCoroutine(bracken));
            }
        }

        private static IEnumerator BrackenLagAllCoroutine(FlowermanAI bracken)
        {
            var localPlayer = LethalMenuMod.LocalPlayer;
            if (localPlayer == null) yield break;

            var players = GetAllPlayers().Where(p => p != localPlayer).ToArray();
            if (players.Length == 0)
            {
                Debug.Log("[NetworkCheats] No other players to lag.");
                yield break;
            }

            // Cycle through all players, transferring Bracken ownership rapidly
            for (int cycle = 0; cycle < 5; cycle++)
            {
                foreach (var player in players)
                {
                    if (player == null || player.isPlayerDead) continue;

                    // Take ownership
                    bracken.ChangeEnemyOwnerServerRpc(localPlayer.actualClientId);
                    yield return null;

                    // Aggravate and teleport to player
                    bracken.SetMovingTowardsTargetPlayer(player);
                    bracken.EnterAngerModeServerRpc(float.MaxValue);
                    bracken.UpdateEnemyPositionServerRpc(player.transform.position);

                    // Transfer to target
                    bracken.ChangeEnemyOwnerServerRpc(player.actualClientId);
                    yield return new WaitForSeconds(0.1f);
                }
            }

            Debug.Log("[NetworkCheats] Bracken lag attack cycled through all players.");
        }

        /// Rapidly toggles ship lights to be annoying (uses coroutine).
        public static void FlickerShipLights()
        {
            if (LethalMenuMod.Instance != null)
            {
                LethalMenuMod.Instance.StartCoroutine(FlickerShipLightsCoroutine());
            }
        }
        
        /// Coroutine that flickers ship lights repeatedly.
        public static IEnumerator FlickerShipLightsCoroutine()
        {
            var shipLights = UnityEngine.Object.FindObjectOfType<ShipLights>();
            if (shipLights == null) yield break;

            // Flicker lights 30 times with visible delay
            for (int i = 0; i < 30; i++)
            {
                shipLights.SetShipLightsServerRpc(i % 2 == 0);
                yield return new WaitForSeconds(0.15f); // 150ms between toggles for visible flicker
            }

            Debug.Log("[NetworkCheats] Flickered ship lights.");
        }

        /// 
        /// Sends massive damage to kill a player instantly.
        /// 
        public static void InstantKillPlayer(PlayerControllerB target)
        {
            if (target == null || target.isPlayerDead) return;

            var localPlayer = LethalMenuMod.LocalPlayer;
            if (localPlayer == null) return;

            // 100 damage should kill most players
            target.DamagePlayerFromOtherClientServerRpc(100, Vector3.up * 10f, (int)localPlayer.playerClientId);
            Debug.Log($"[NetworkCheats] Instant killed {target.playerUsername}.");
        }

        /// Kill all other players at once.
        public static void MassKillPlayers()
        {
            var players = GetAllPlayers();
            var localPlayer = LethalMenuMod.LocalPlayer;
            if (localPlayer == null) return;

            int killed = 0;
            foreach (var player in players)
            {
                if (player != null && player != localPlayer && !player.isPlayerDead)
                {
                    player.DamagePlayerFromOtherClientServerRpc(100, Vector3.up * 10f, (int)localPlayer.playerClientId);
                    killed++;
                }
            }

            Debug.Log($"[NetworkCheats] Mass killed {killed} players.");
        }

        /// Impersonate another player in chat.
        /// Sends message that appears to be from another player.
        public static void ImpersonateInChat(string message, int playerIndexToImpersonate)
        {
            var hud = HUDManager.Instance;
            if (hud == null) return;

            // Truncate to 50 characters
            if (message.Length > 50)
                message = message.Substring(0, 50);

            // Use the public method with the target player's index
            hud.AddTextToChatOnServer(message, playerIndexToImpersonate);
            Debug.Log($"[NetworkCheats] Sent message as player {playerIndexToImpersonate}.");
        }

        #endregion

        #region Signal Translator Spam (RequireOwnership = false)

        /// Sends a message via signal translator (shows on ship's monitor).
        /// Uses HUDManager.UseSignalTranslatorServerRpc - RequireOwnership = false.
        public static void SendSignalTranslatorMessage(string message)
        {
            var hud = HUDManager.Instance;
            if (hud == null)
            {
                Debug.Log("[NetworkCheats] HUD not found.");
                return;
            }

            // Max 10 characters for signal translator
            string truncated = message.Length > 10 ? message.Substring(0, 10) : message;
            hud.UseSignalTranslatorServerRpc(truncated);
            Debug.Log($"[NetworkCheats] Signal: {truncated}");
        }

        /// Spams signal translator with messages (very annoying, shows on everyone's ship monitor).
        public static void SpamSignalTranslator(int iterations = 20)
        {
            if (LethalMenuMod.Instance != null)
            {
                LethalMenuMod.Instance.StartCoroutine(SpamSignalTranslatorCoroutine(iterations));
            }
        }

        private static IEnumerator SpamSignalTranslatorCoroutine(int iterations)
        {
            var hud = HUDManager.Instance;
            if (hud == null) yield break;

            string[] messages = { "AAAAAAAAAA", "TROLLED", "HACKED", "LOLOLOL", "GOTCHA", "OWNED", "REKT", "GG EZ" };

            for (int i = 0; i < iterations; i++)
            {
                string msg = messages[i % messages.Length];
                hud.UseSignalTranslatorServerRpc(msg);
                yield return new WaitForSeconds(0.3f);
            }

            Debug.Log($"[NetworkCheats] Spammed signal translator {iterations} times.");
        }

        #endregion

        #region Ship Horn Spam (RequireOwnership = false)

        /// Pulls the ship alarm cord (honks the horn).
        /// Uses ShipAlarmCord.PullCordServerRpc - RequireOwnership = false.
        public static void PullShipHorn()
        {
            var alarmCord = UnityEngine.Object.FindObjectOfType<ShipAlarmCord>();
            if (alarmCord == null)
            {
                Debug.Log("[NetworkCheats] Ship alarm cord not found.");
                return;
            }

            var localPlayer = LethalMenuMod.LocalPlayer;
            int playerId = localPlayer != null ? (int)localPlayer.playerClientId : 0;

            alarmCord.PullCordServerRpc(playerId);
            Debug.Log("[NetworkCheats] Pulled ship horn.");
        }

        /// Stops the ship horn.
        public static void StopShipHorn()
        {
            var alarmCord = UnityEngine.Object.FindObjectOfType<ShipAlarmCord>();
            if (alarmCord == null) return;

            var localPlayer = LethalMenuMod.LocalPlayer;
            int playerId = localPlayer != null ? (int)localPlayer.playerClientId : 0;

            alarmCord.StopPullingCordServerRpc(playerId);
            Debug.Log("[NetworkCheats] Stopped ship horn.");
        }

        /// Spams the ship horn on/off rapidly (extremely annoying).
        public static void SpamShipHorn(int iterations = 15)
        {
            if (LethalMenuMod.Instance != null)
            {
                LethalMenuMod.Instance.StartCoroutine(SpamShipHornCoroutine(iterations));
            }
        }

        private static IEnumerator SpamShipHornCoroutine(int iterations)
        {
            var alarmCord = UnityEngine.Object.FindObjectOfType<ShipAlarmCord>();
            if (alarmCord == null) yield break;

            var localPlayer = LethalMenuMod.LocalPlayer;
            int playerId = localPlayer != null ? (int)localPlayer.playerClientId : 0;

            for (int i = 0; i < iterations; i++)
            {
                alarmCord.PullCordServerRpc(playerId);
                yield return new WaitForSeconds(0.2f);
                alarmCord.StopPullingCordServerRpc(playerId);
                yield return new WaitForSeconds(0.1f);
            }

            Debug.Log($"[NetworkCheats] Spammed ship horn {iterations} times.");
        }

        #endregion

        #region Game Control (RequireOwnership = false)

        /// Forces the game to start (launches ship).
        /// Uses StartOfRound.StartGameServerRpc - RequireOwnership = false.
        public static void ForceStartGame()
        {
            var startOfRound = StartOfRound.Instance;
            if (startOfRound == null)
            {
                Debug.Log("[NetworkCheats] Not in game.");
                return;
            }

            startOfRound.StartGameServerRpc();
            Debug.Log("[NetworkCheats] Force started game.");
        }

        /// Forces the game to end (returns to orbit).
        /// Uses StartOfRound.EndGameServerRpc - RequireOwnership = false.
        public static void ForceEndGame()
        {
            var startOfRound = StartOfRound.Instance;
            if (startOfRound == null)
            {
                Debug.Log("[NetworkCheats] Not in game.");
                return;
            }

            var localPlayer = LethalMenuMod.LocalPlayer;
            int playerId = localPlayer != null ? (int)localPlayer.playerClientId : 0;

            startOfRound.EndGameServerRpc(playerId);
            Debug.Log("[NetworkCheats] Force ended game.");
        }

        /// Opens or closes the ship doors.
        /// Uses StartOfRound.SetDoorsClosedServerRpc - RequireOwnership = false.
        /// Also plays the local animation via HangarShipDoor.
        public static void SetShipDoors(bool closed)
        {
            var startOfRound = StartOfRound.Instance;
            if (startOfRound == null)
            {
                Debug.Log("[NetworkCheats] Not in game.");
                return;
            }

            // Call the server RPC to sync state across network
            startOfRound.SetDoorsClosedServerRpc(closed);
            
            // Also play the animation locally (the RPC only sets the state, not the visual)
            var hangarDoor = UnityEngine.Object.FindObjectOfType<HangarShipDoor>();
            if (hangarDoor != null)
            {
                // PlayDoorAnimation requires buttonsEnabled, so set animator directly
                hangarDoor.shipDoorsAnimator?.SetBool("Closed", closed);
            }
            
            Debug.Log($"[NetworkCheats] Ship doors {(closed ? "closed" : "opened")}.");
        }

        /// Spams ship doors open/close (annoying visual effect).
        public static void SpamShipDoors(int iterations = 10)
        {
            if (LethalMenuMod.Instance != null)
            {
                LethalMenuMod.Instance.StartCoroutine(SpamShipDoorsCoroutine(iterations));
            }
        }

        private static IEnumerator SpamShipDoorsCoroutine(int iterations)
        {
            var startOfRound = StartOfRound.Instance;
            if (startOfRound == null) yield break;
            
            var hangarDoor = UnityEngine.Object.FindObjectOfType<HangarShipDoor>();

            for (int i = 0; i < iterations; i++)
            {
                bool closed = (i % 2 == 0);
                startOfRound.SetDoorsClosedServerRpc(closed);
                hangarDoor?.shipDoorsAnimator?.SetBool("Closed", closed);
                yield return new WaitForSeconds(0.5f);
            }

            Debug.Log($"[NetworkCheats] Spammed ship doors {iterations} times.");
        }

        /// Overheat ship doors - locks them via the overheat mechanism (like when Big Kiwi pries them).
        /// Uses StartOfRound.SetShipDoorsOverheatServerRpc - RequireOwnership = false.
        public static void OverheatShipDoors()
        {
            var startOfRound = StartOfRound.Instance;
            if (startOfRound == null)
            {
                Debug.Log("[NetworkCheats] OverheatShipDoors: Not in game.");
                HUDManager.Instance?.DisplayTip("Ship Doors", "Not in game.");
                return;
            }

            // This calls SetShipDoorsOverheatServerRpc which has RequireOwnership = false
            // It opens doors and sets overheated state, making them unusable
            startOfRound.SetShipDoorsOverheatServerRpc();
            Debug.Log("[NetworkCheats] Overheated ship doors.");
            HUDManager.Instance?.DisplayTip("Ship Doors", "Ship doors overheated and locked open.");
        }

        #endregion

        #region Free Vehicles (RequireOwnership = false)

        /// Buys a vehicle for free.
        /// Uses Terminal.BuyVehicleServerRpc - RequireOwnership = false.
        public static void BuyFreeVehicle(int vehicleId)
        {
            var terminal = UnityEngine.Object.FindObjectOfType<Terminal>();
            if (terminal == null)
            {
                Debug.Log("[NetworkCheats] Terminal not found.");
                return;
            }

            // Buy vehicle but keep current credits
            terminal.BuyVehicleServerRpc(vehicleId, terminal.groupCredits, false);
            Debug.Log($"[NetworkCheats] Bought vehicle ID {vehicleId} for free.");
        }

        /// Gets list of available vehicles.
        public static (int id, string name)[] GetAvailableVehicles()
        {
            var terminal = UnityEngine.Object.FindObjectOfType<Terminal>();
            if (terminal == null || terminal.buyableVehicles == null)
                return Array.Empty<(int, string)>();

            var result = new (int, string)[terminal.buyableVehicles.Length];
            for (int i = 0; i < terminal.buyableVehicles.Length; i++)
            {
                var v = terminal.buyableVehicles[i];
                result[i] = (i, v?.vehicleDisplayName ?? $"Vehicle {i}");
            }
            return result;
        }

        #endregion

        #region Combined Chaos (Multiple exploits at once)

        /// Triggers maximum chaos: horn spam, light flicker, door spam, signal spam.
        public static void MaxChaos()
        {
            if (LethalMenuMod.Instance != null)
            {
                LethalMenuMod.Instance.StartCoroutine(MaxChaosCoroutine());
            }
        }

        private static IEnumerator MaxChaosCoroutine()
        {
            Debug.Log("[NetworkCheats] MAXIMUM CHAOS ENGAGED!");

            // Start all chaos coroutines simultaneously
            var instance = LethalMenuMod.Instance;
            if (instance == null) yield break;
            instance.StartCoroutine(SpamShipHornCoroutine(10));
            instance.StartCoroutine(FlickerShipLightsCoroutine());
            instance.StartCoroutine(SpamShipDoorsCoroutine(8));
            instance.StartCoroutine(SpamSignalTranslatorCoroutine(15));

            yield return new WaitForSeconds(6f);
            Debug.Log("[NetworkCheats] Chaos complete.");
        }

        #endregion

        #region Landmines and Turrets

        /// Blow up all landmines on the map.
        public static void BlowUpAllLandmines()
        {
            var landmines = UnityEngine.Object.FindObjectsOfType<Landmine>();
            if (landmines == null || landmines.Length == 0)
            {
                Debug.Log("[NetworkCheats] No landmines found.");
                return;
            }

            int count = 0;
            foreach (var mine in landmines)
            {
                if (mine != null && !mine.hasExploded)
                {
                    mine.ExplodeMineServerRpc();
                    count++;
                }
            }
            Debug.Log($"[NetworkCheats] Exploded {count} landmines.");
        }

        /// Toggle all landmines enabled/disabled state.
        public static void ToggleAllLandmines(bool enable)
        {
            var landmines = UnityEngine.Object.FindObjectsOfType<Landmine>();
            if (landmines == null || landmines.Length == 0) return;

            foreach (var mine in landmines)
            {
                if (mine != null)
                {
                    mine.ToggleMine(enable);
                }
            }
            Debug.Log($"[NetworkCheats] Toggled {landmines.Length} landmines to {(enable ? "ON" : "OFF")}.");
        }

        /// Toggle all turrets enabled/disabled state.
        public static void ToggleAllTurrets(bool enable)
        {
            var turrets = UnityEngine.Object.FindObjectsOfType<Turret>();
            if (turrets == null || turrets.Length == 0) return;

            foreach (var turret in turrets)
            {
                if (turret != null)
                {
                    turret.ToggleTurretEnabled(enable);
                }
            }
            Debug.Log($"[NetworkCheats] Toggled {turrets.Length} turrets to {(enable ? "ON" : "OFF")}.");
        }

        /// Makes all turrets go berserk mode (fires at everything).
        public static void BerserkAllTurrets()
        {
            var turrets = UnityEngine.Object.FindObjectsOfType<Turret>();
            if (turrets == null || turrets.Length == 0) return;

            foreach (var turret in turrets)
            {
                if (turret != null)
                {
                    turret.SwitchTurretMode(3); // Berserk mode
                    turret.EnterBerserkModeServerRpc(-1);
                }
            }
            Debug.Log($"[NetworkCheats] {turrets.Length} turrets now berserk.");
        }

        #endregion

        #region Bridge and Structures

        /// Force the bridge to collapse.
        public static void ForceBridgeFall()
        {
            var bridge = UnityEngine.Object.FindObjectOfType<BridgeTrigger>();
            if (bridge == null)
            {
                Debug.Log("[NetworkCheats] No bridge found.");
                return;
            }
            bridge.BridgeFallServerRpc();
            Debug.Log("[NetworkCheats] Bridge collapsed.");
        }

        /// Force the small bridge (type 2) to collapse.
        public static void ForceSmallBridgeFall()
        {
            var bridge = UnityEngine.Object.FindObjectOfType<BridgeTriggerType2>();
            if (bridge == null)
            {
                Debug.Log("[NetworkCheats] No small bridge found.");
                return;
            }
            // Add instability until it falls
            for (int i = 0; i < 4; i++)
            {
                bridge.AddToBridgeInstabilityServerRpc();
            }
            Debug.Log("[NetworkCheats] Small bridge destabilized.");
        }

        #endregion

        #region Vehicle Control

        /// Toggle car horn on all vehicles.
        public static void ToggleCarHorns(bool on)
        {
            var vehicles = UnityEngine.Object.FindObjectsOfType<VehicleController>();
            if (vehicles == null || vehicles.Length == 0)
            {
                Debug.Log("[NetworkCheats] No vehicles found.");
                return;
            }

            var localPlayer = LethalMenuMod.LocalPlayer;
            int playerId = localPlayer != null ? (int)localPlayer.playerClientId : -1;

            foreach (var vehicle in vehicles)
            {
                if (vehicle != null)
                {
                    vehicle.SetHonkServerRpc(on, playerId);
                }
            }
            Debug.Log($"[NetworkCheats] Vehicle horns {(on ? "ON" : "OFF")}.");
        }

        /// Spam car horns on/off rapidly.
        public static void SpamCarHorns(int iterations = 15)
        {
            if (LethalMenuMod.Instance != null)
            {
                LethalMenuMod.Instance.StartCoroutine(SpamCarHornsCoroutine(iterations));
            }
        }

        private static IEnumerator SpamCarHornsCoroutine(int iterations)
        {
            var vehicles = UnityEngine.Object.FindObjectsOfType<VehicleController>();
            if (vehicles == null || vehicles.Length == 0) yield break;

            var localPlayer = LethalMenuMod.LocalPlayer;
            int playerId = localPlayer != null ? (int)localPlayer.playerClientId : -1;

            for (int i = 0; i < iterations; i++)
            {
                foreach (var vehicle in vehicles)
                {
                    if (vehicle != null)
                    {
                        vehicle.SetHonkServerRpc(i % 2 == 0, playerId);
                    }
                }
                yield return new WaitForSeconds(0.2f);
            }
            Debug.Log($"[NetworkCheats] Spammed car horns {iterations} times.");
        }

        /// Explode all vehicles (Cruisers).
        public static void ExplodeAllVehicles()
        {
            var vehicles = UnityEngine.Object.FindObjectsOfType<VehicleController>();
            if (vehicles == null || vehicles.Length == 0)
            {
                Debug.Log("[NetworkCheats] No vehicles found.");
                return;
            }

            foreach (var vehicle in vehicles)
            {
                if (vehicle != null)
                {
                    vehicle.DestroyCarServerRpc(-1);
                }
            }
            Debug.Log($"[NetworkCheats] Exploded {vehicles.Length} vehicles.");
        }

        /// Hijack/take control of a vehicle. Uses SetPlayerInControlOfVehicleServerRpc (RequireOwnership=false).
        public static void HijackVehicle(VehicleController? vehicle = null)
        {
            var localPlayer = LethalMenuMod.LocalPlayer;
            if (localPlayer == null)
            {
                Debug.Log("[NetworkCheats] HijackVehicle: Local player is null.");
                return;
            }

            // If no specific vehicle, find the first one
            if (vehicle == null)
            {
                var vehicles = UnityEngine.Object.FindObjectsOfType<VehicleController>();
                if (vehicles == null || vehicles.Length == 0)
                {
                    Debug.Log("[NetworkCheats] HijackVehicle: No vehicles found.");
                    HUDManager.Instance?.DisplayTip("Vehicle", "No vehicles found on map.");
                    return;
                }
                vehicle = vehicles[0];
            }

            // Call the ServerRpc to take control - this has RequireOwnership = false
            vehicle.SetPlayerInControlOfVehicleServerRpc((int)localPlayer.playerClientId);
            Debug.Log($"[NetworkCheats] Hijacked vehicle.");
            HUDManager.Instance?.DisplayTip("Vehicle", "Took control of vehicle.");
        }

        /// Kick the current driver out of a vehicle.
        public static void EjectVehicleDriver(VehicleController? vehicle = null)
        {
            if (vehicle == null)
            {
                var vehicles = UnityEngine.Object.FindObjectsOfType<VehicleController>();
                if (vehicles == null || vehicles.Length == 0)
                {
                    Debug.Log("[NetworkCheats] EjectVehicleDriver: No vehicles found.");
                    return;
                }
                vehicle = vehicles.FirstOrDefault(v => v.currentDriver != null);
            }

            if (vehicle == null || vehicle.currentDriver == null)
            {
                Debug.Log("[NetworkCheats] EjectVehicleDriver: No vehicle with driver found.");
                HUDManager.Instance?.DisplayTip("Vehicle", "No vehicle with driver found.");
                return;
            }

            int driverId = (int)vehicle.currentDriver.playerClientId;
            // Force remove player from control
            vehicle.RemovePlayerControlOfVehicleServerRpc(driverId, vehicle.transform.position, vehicle.transform.rotation, false);
            Debug.Log($"[NetworkCheats] Ejected driver from vehicle.");
            HUDManager.Instance?.DisplayTip("Vehicle", "Ejected driver from vehicle.");
        }

        /// Add turbo boosts to a vehicle.
        public static void AddVehicleTurbo(int turboAmount = 5, VehicleController? vehicle = null)
        {
            var localPlayer = LethalMenuMod.LocalPlayer;
            if (localPlayer == null) return;

            if (vehicle == null)
            {
                var vehicles = UnityEngine.Object.FindObjectsOfType<VehicleController>();
                if (vehicles == null || vehicles.Length == 0)
                {
                    Debug.Log("[NetworkCheats] AddVehicleTurbo: No vehicles found.");
                    HUDManager.Instance?.DisplayTip("Vehicle", "No vehicles found.");
                    return;
                }
                vehicle = vehicles[0];
            }

            // Call the ServerRpc to add turbo - RequireOwnership = false
            vehicle.AddTurboBoostServerRpc((int)localPlayer.playerClientId, turboAmount);
            Debug.Log($"[NetworkCheats] Added {turboAmount} turbo boosts to vehicle.");
            HUDManager.Instance?.DisplayTip("Vehicle", $"Added {turboAmount} turbo boosts.");
        }

        /// Use turbo boost on a vehicle.
        public static void UseVehicleTurbo(VehicleController? vehicle = null)
        {
            if (vehicle == null)
            {
                var vehicles = UnityEngine.Object.FindObjectsOfType<VehicleController>();
                if (vehicles == null || vehicles.Length == 0)
                {
                    Debug.Log("[NetworkCheats] UseVehicleTurbo: No vehicles found.");
                    return;
                }
                vehicle = vehicles[0];
            }

            vehicle.UseTurboBoostServerRpc();
            Debug.Log("[NetworkCheats] Used turbo boost.");
        }

        /// Damage vehicle engine / add oil to repair.
        public static void SetVehicleEngineHealth(int hp, VehicleController? vehicle = null)
        {
            var localPlayer = LethalMenuMod.LocalPlayer;
            if (localPlayer == null) return;

            if (vehicle == null)
            {
                var vehicles = UnityEngine.Object.FindObjectsOfType<VehicleController>();
                if (vehicles == null || vehicles.Length == 0)
                {
                    Debug.Log("[NetworkCheats] SetVehicleEngineHealth: No vehicles found.");
                    return;
                }
                vehicle = vehicles[0];
            }

            // AddEngineOilServerRpc sets the HP directly
            vehicle.AddEngineOilServerRpc((int)localPlayer.playerClientId, hp);
            Debug.Log($"[NetworkCheats] Set vehicle engine HP to {hp}.");
            HUDManager.Instance?.DisplayTip("Vehicle", $"Engine HP set to {hp}.");
        }

        /// Kill vehicle engine (set HP to 0).
        public static void KillVehicleEngine(VehicleController? vehicle = null)
        {
            SetVehicleEngineHealth(0, vehicle);
            HUDManager.Instance?.DisplayTip("Vehicle", "Engine killed.");
        }

        /// Fully repair vehicle engine.
        public static void RepairVehicleEngine(VehicleController? vehicle = null)
        {
            SetVehicleEngineHealth(100, vehicle);
            HUDManager.Instance?.DisplayTip("Vehicle", "Engine fully repaired.");
        }

        /// Shift vehicle gear remotely.
        public static void ShiftVehicleGear(int gear, VehicleController? vehicle = null)
        {
            var localPlayer = LethalMenuMod.LocalPlayer;
            if (localPlayer == null) return;

            if (vehicle == null)
            {
                var vehicles = UnityEngine.Object.FindObjectsOfType<VehicleController>();
                if (vehicles == null || vehicles.Length == 0)
                {
                    Debug.Log("[NetworkCheats] ShiftVehicleGear: No vehicles found.");
                    return;
                }
                vehicle = vehicles[0];
            }

            // Gear: 0=Park, 1=Drive, 2=Reverse
            vehicle.ShiftToGearServerRpc(gear, (int)localPlayer.playerClientId);
            string gearName = gear switch { 0 => "Park", 1 => "Drive", 2 => "Reverse", _ => "Unknown" };
            Debug.Log($"[NetworkCheats] Shifted vehicle to gear {gear} ({gearName}).");
            HUDManager.Instance?.DisplayTip("Vehicle", $"Gear: {gearName}");
        }

        /// Set vehicle radio station.
        public static void SetVehicleRadio(int station, int quality = 100, VehicleController? vehicle = null)
        {
            if (vehicle == null)
            {
                var vehicles = UnityEngine.Object.FindObjectsOfType<VehicleController>();
                if (vehicles == null || vehicles.Length == 0)
                {
                    Debug.Log("[NetworkCheats] SetVehicleRadio: No vehicles found.");
                    return;
                }
                vehicle = vehicles[0];
            }

            vehicle.SetRadioStationServerRpc(station, quality);
            Debug.Log($"[NetworkCheats] Set vehicle radio to station {station}.");
        }

        #endregion

        #region Jetpacks

        /// Explode all jetpacks.
        public static void ExplodeAllJetpacks()
        {
            var items = UnityEngine.Object.FindObjectsOfType<JetpackItem>();
            if (items == null || items.Length == 0)
            {
                Debug.Log("[NetworkCheats] No jetpacks found.");
                return;
            }

            int count = 0;
            foreach (var jetpack in items)
            {
                if (jetpack != null && !jetpack.isHeld)
                {
                    jetpack.ExplodeJetpackServerRpc();
                    count++;
                }
            }
            Debug.Log($"[NetworkCheats] Exploded {count} jetpacks.");
        }

        #endregion

        #region Shotgun Spam

        /// Fire all shotguns on the map.
        public static void ShootAllShotguns()
        {
            var shotguns = UnityEngine.Object.FindObjectsOfType<ShotgunItem>();
            if (shotguns == null || shotguns.Length == 0)
            {
                Debug.Log("[NetworkCheats] No shotguns found.");
                return;
            }

            int count = 0;
            foreach (var shotgun in shotguns)
            {
                if (shotgun != null && shotgun.shellsLoaded > 0)
                {
                    var pos = shotgun.transform.position + shotgun.transform.up * 0.45f;
                    var forward = shotgun.transform.forward;
                    shotgun.ShootGunServerRpc(pos, forward);
                    count++;
                }
            }
            Debug.Log($"[NetworkCheats] Fired {count} shotguns.");
        }

        /// Spam fire all shotguns repeatedly.
        public static void SpamShootAllShotguns(int iterations = 10)
        {
            if (LethalMenuMod.Instance != null)
            {
                LethalMenuMod.Instance.StartCoroutine(SpamShotgunsCoroutine(iterations));
            }
        }

        private static IEnumerator SpamShotgunsCoroutine(int iterations)
        {
            for (int i = 0; i < iterations; i++)
            {
                ShootAllShotguns();
                yield return new WaitForSeconds(0.1f);
            }
            Debug.Log($"[NetworkCheats] Spam fired shotguns {iterations} times.");
        }

        #endregion

        #region Factory Control

        /// Toggle factory/facility lights.
        public static void ToggleFactoryLights()
        {
            var breaker = UnityEngine.Object.FindObjectOfType<BreakerBox>();
            if (breaker == null)
            {
                Debug.Log("[NetworkCheats] Breaker box not found.");
                return;
            }

            var roundManager = RoundManager.Instance;
            if (roundManager == null) return;

            // Toggle power
            roundManager.SwitchPower(!breaker.isPowerOn);
            Debug.Log($"[NetworkCheats] Factory lights {(breaker.isPowerOn ? "ON" : "OFF")}.");
        }

        /// Force the company deposit desk tentacle attack.
        public static void ForceTentacleAttack()
        {
            var desk = UnityEngine.Object.FindObjectOfType<DepositItemsDesk>();
            if (desk == null)
            {
                Debug.Log("[NetworkCheats] Deposit desk not found.");
                return;
            }

            desk.AttackPlayersServerRpc();
            Debug.Log("[NetworkCheats] Tentacle attack triggered.");
        }

        /// Spam the deposit desk door open/close.
        public static void SpamDepositDeskDoor(int iterations = 20)
        {
            if (LethalMenuMod.Instance != null)
            {
                LethalMenuMod.Instance.StartCoroutine(SpamDepositDeskDoorCoroutine(iterations));
            }
        }

        private static IEnumerator SpamDepositDeskDoorCoroutine(int iterations)
        {
            var desk = UnityEngine.Object.FindObjectOfType<DepositItemsDesk>();
            if (desk == null) yield break;

            for (int i = 0; i < iterations; i++)
            {
                desk.OpenShutDoorClientRpc();
                yield return new WaitForSeconds(0.2f);
            }
            Debug.Log($"[NetworkCheats] Spammed deposit desk door {iterations} times.");
        }

        #endregion

        #region Terminal Sound Spam

        /// Spam terminal sounds (includes earrape invalid indices).
        public static void SpamTerminalSound(int iterations = 20)
        {
            if (LethalMenuMod.Instance != null)
            {
                LethalMenuMod.Instance.StartCoroutine(SpamTerminalSoundCoroutine(iterations));
            }
        }

        private static IEnumerator SpamTerminalSoundCoroutine(int iterations)
        {
            var terminal = UnityEngine.Object.FindObjectOfType<Terminal>();
            if (terminal == null) yield break;

            for (int i = 0; i < iterations; i++)
            {
                // Normal sound
                terminal.PlayTerminalAudioServerRpc(1);
                // Earrape invalid index (cash register sound)
                terminal.PlayTerminalAudioServerRpc(-1);
                yield return new WaitForSeconds(0.1f);
            }
            Debug.Log($"[NetworkCheats] Spammed terminal sound + earrape {iterations} times.");
        }

        /// Pure earrape spam - only invalid indices.
        public static void SpamTerminalEarrape(int iterations = 30)
        {
            if (LethalMenuMod.Instance != null)
            {
                LethalMenuMod.Instance.StartCoroutine(SpamTerminalEarrapeCoroutine(iterations));
            }
        }

        private static IEnumerator SpamTerminalEarrapeCoroutine(int iterations)
        {
            var terminal = UnityEngine.Object.FindObjectOfType<Terminal>();
            if (terminal == null) yield break;

            for (int i = 0; i < iterations; i++)
            {
                terminal.PlayTerminalAudioServerRpc(-1);
                terminal.PlayTerminalAudioServerRpc(-99999);
                terminal.PlayTerminalAudioServerRpc(int.MaxValue);
                yield return new WaitForSeconds(0.05f);
            }
            Debug.Log($"[NetworkCheats] Earrape spam {iterations} times.");
        }

        #endregion

        #region Continuous Spam Toggles
        
        // Timers for continuous spam (to avoid spamming every frame)
        private static float _lastHornSpam = 0f;
        private static float _lastDoorSpam = 0f;
        private static float _lastSignalSpam = 0f;
        private static float _lastRPCSpam = 0f;
        private static float _lastTerminalSpam = 0f;
        private static float _lastEarrapeSpam = 0f;
        private static float _lastChatSpam = 0f;
        private static float _lastCarHornSpam = 0f;
        private static float _lastDeskDoorSpam = 0f;
        private static int _spamCounter = 0;

        /// Call this every frame from Update() to process continuous spam toggles.
        public static void ProcessSpamToggles()
        {
            float time = Time.time;
            var hud = HUDManager.Instance;
            var terminal = UnityEngine.Object.FindObjectOfType<Terminal>();

            // Horn Spam (every 0.15s)
            if (Settings.HornSpam && time - _lastHornSpam > 0.15f)
            {
                _lastHornSpam = time;
                hud?.AlarmHornServerRpc();
            }

            // Door Spam (every 0.2s)
            if (Settings.DoorSpam && time - _lastDoorSpam > 0.2f)
            {
                _lastDoorSpam = time;
                _spamCounter++;
                var startOfRound = StartOfRound.Instance;
                if (startOfRound != null)
                {
                    bool closed = (_spamCounter % 2 == 0);
                    startOfRound.SetDoorsClosedServerRpc(closed);
                }
            }

            // Signal Translator Spam (every 0.3s - has cooldown but we try anyway)
            if (Settings.SignalSpam && time - _lastSignalSpam > 0.3f)
            {
                _lastSignalSpam = time;
                _spamCounter++;
                hud?.UseSignalTranslatorServerRpc($"S{_spamCounter % 100:D2}");
            }

            // RPC Lag Spam (every 0.05s - aggressive) - Full chaos: signal, chat, horn, damage, AND terminal sounds
            if (Settings.RPCLagSpam && time - _lastRPCSpam > 0.05f)
            {
                _lastRPCSpam = time;
                _spamCounter++;
                
                if (hud != null)
                {
                    // Signal spam
                    hud.UseSignalTranslatorServerRpc($"L{_spamCounter % 100:D2}");
                    // Chat spam (hidden)
                    hud.AddTextToChatOnServer($"<size=0>{_spamCounter}</size>");
                    // Horn spam every 3rd iteration
                    if (_spamCounter % 3 == 0)
                    {
                        hud.AlarmHornServerRpc();
                    }
                }
                
                // Terminal audio spam - cycle through indices for variety
                if (terminal != null)
                {
                    terminal.PlayTerminalAudioServerRpc(_spamCounter % 5); // Cycle 0-4
                    if (_spamCounter % 2 == 0)
                    {
                        terminal.PlayTerminalAudioServerRpc(-1); // Invalid for earrape
                    }
                }
                
                // Damage spam
                var localPlayer = LethalMenuMod.LocalPlayer;
                if (localPlayer != null && !localPlayer.isPlayerDead && _spamCounter % 5 == 0)
                {
                    localPlayer.DamagePlayerFromOtherClientServerRpc(0, UnityEngine.Vector3.zero, -1);
                }
            }

            // Terminal Sound Spam (every 0.08s) - FULL combo: index 0 (cash register) + index 1 (beep) + invalid indices
            if (Settings.TerminalSoundSpam && time - _lastTerminalSpam > 0.08f)
            {
                _lastTerminalSpam = time;
                _spamCounter++;
                if (terminal != null)
                {
                    // Index 0 = cash register sound (the one Terminal Crash button uses)
                    terminal.PlayTerminalAudioServerRpc(0);
                    // Index 1 = beep
                    terminal.PlayTerminalAudioServerRpc(1);
                    // Invalid indices = earrape
                    terminal.PlayTerminalAudioServerRpc(-1);
                    terminal.PlayTerminalAudioServerRpc(-99999);
                }
            }

            // Terminal Earrape Spam (every 0.03s) - Pure invalid index spam + index 0 for max noise
            if (Settings.TerminalEarrapeSpam && time - _lastEarrapeSpam > 0.03f)
            {
                _lastEarrapeSpam = time;
                _spamCounter++;
                if (terminal != null)
                {
                    // Index 0 = cash register (the actual sound)
                    terminal.PlayTerminalAudioServerRpc(0);
                    terminal.PlayTerminalAudioServerRpc(0);
                    terminal.PlayTerminalAudioServerRpc(0);
                    // Invalid indices
                    terminal.PlayTerminalAudioServerRpc(-1);
                    terminal.PlayTerminalAudioServerRpc(-99999);
                    terminal.PlayTerminalAudioServerRpc(int.MaxValue);
                }
            }

            // Chat Spam Loop (every 0.5s)
            if (Settings.ChatSpamLoop && time - _lastChatSpam > 0.5f)
            {
                _lastChatSpam = time;
                _spamCounter++;
                string msg = Settings.SpamMessage;
                if (msg.Length > 45) msg = msg.Substring(0, 45);
                hud?.AddTextToChatOnServer($"{msg}[{_spamCounter % 1000}]");
            }

            // Car Horn Spam (every 0.15s)
            if (Settings.CarHornSpam && time - _lastCarHornSpam > 0.15f)
            {
                _lastCarHornSpam = time;
                _spamCounter++;
                var cars = UnityEngine.Object.FindObjectsOfType<VehicleController>();
                var localPlayer = LethalMenuMod.LocalPlayer;
                int playerId = localPlayer != null ? (int)localPlayer.playerClientId : -1;
                foreach (var car in cars)
                {
                    if (car != null)
                    {
                        car.SetHonkServerRpc(_spamCounter % 2 == 0, playerId);
                    }
                }
            }

            // Desk Door Spam (every 0.2s)
            if (Settings.DeskDoorSpam && time - _lastDeskDoorSpam > 0.2f)
            {
                _lastDeskDoorSpam = time;
                var desk = UnityEngine.Object.FindObjectOfType<DepositItemsDesk>();
                desk?.OpenShutDoorClientRpc();
            }
        }

        #endregion

        #region Stun All Enemies

        /// Stun all enemies on the map.
        public static void StunAllEnemies()
        {
            var localPlayer = LethalMenuMod.LocalPlayer;
            if (localPlayer == null) return;

            int count = 0;
            foreach (var enemy in LethalMenuMod.Enemies)
            {
                if (enemy != null && !enemy.isEnemyDead)
                {
                    enemy.SetEnemyStunned(true, 5f, localPlayer);
                    count++;
                }
            }
            Debug.Log($"[NetworkCheats] Stunned {count} enemies.");
        }

        #endregion

        #region Self-Revive (Client-Side Respawn)

        /// Respawns the local player after death. This is a client-side respawn
        /// that resets all player states locally without needing host permissions.
        /// Note: Other players will still see you as dead. The body may remain.
        /// Based on StartOfRound.ReviveDeadPlayers logic.
        public static void SelfRevive()
        {
            var localPlayer = LethalMenuMod.LocalPlayer;
            if (localPlayer == null)
            {
                Debug.Log("[NetworkCheats] SelfRevive: Local player not found.");
                return;
            }

            if (!localPlayer.isPlayerDead)
            {
                Debug.Log("[NetworkCheats] SelfRevive: Player is not dead.");
                return;
            }

            var startOfRound = StartOfRound.Instance;
            if (startOfRound == null)
            {
                Debug.Log("[NetworkCheats] SelfRevive: Not in game.");
                return;
            }

            try
            {
                // Clear the global "all players dead" flag
                startOfRound.allPlayersDead = false;

                // Reset blood objects
                localPlayer.ResetPlayerBloodObjects(localPlayer.isPlayerDead);

                // Reset climbing state
                localPlayer.isClimbingLadder = false;
                localPlayer.ResetZAndXRotation();
                
                // Enable character controller
                localPlayer.thisController.enabled = true;
                
                // Restore health
                localPlayer.health = 100;
                localPlayer.disableLookInput = false;

                // Reset death state
                localPlayer.isPlayerDead = false;
                localPlayer.isPlayerControlled = true;
                localPlayer.isInElevator = true;
                localPlayer.isInHangarShipRoom = true;
                localPlayer.isInsideFactory = false;
                
                // Disable extrapolation
                startOfRound.SetPlayerObjectExtrapolate(false);
                
                // Teleport to ship spawn position
                if (startOfRound.playerSpawnPositions != null && startOfRound.playerSpawnPositions.Length > 0)
                {
                    localPlayer.TeleportPlayer(startOfRound.playerSpawnPositions[0].position, false, 0f, false, true);
                }
                
                localPlayer.setPositionOfDeadPlayer = false;
                
                // Re-enable player model
                localPlayer.DisablePlayerModel(startOfRound.allPlayerObjects[localPlayer.playerClientId], true, true);
                
                // Reset visual states
                localPlayer.helmetLight.enabled = false;
                localPlayer.Crouch(false);
                localPlayer.criticallyInjured = false;
                localPlayer.playerBodyAnimator?.SetBool("Limp", false);
                localPlayer.bleedingHeavily = false;
                localPlayer.activatingItem = false;
                localPlayer.twoHanded = false;
                localPlayer.inSpecialInteractAnimation = false;
                localPlayer.disableSyncInAnimation = false;
                localPlayer.inAnimationWithEnemy = null;
                localPlayer.holdingWalkieTalkie = false;
                localPlayer.speakingToWalkieTalkie = false;
                localPlayer.isSinking = false;
                localPlayer.isUnderwater = false;
                localPlayer.sinkingValue = 0f;
                localPlayer.statusEffectAudio?.Stop();
                localPlayer.DisableJetpackControlsLocally();
                
                // Reset radar dot animation
                localPlayer.mapRadarDotAnimator?.SetBool("dead", false);

                // Owner-specific resets (should always be true for local player)
                if (localPlayer.IsOwner)
                {
                    var hud = HUDManager.Instance;
                    if (hud != null)
                    {
                        hud.gasHelmetAnimator?.SetBool("gasEmitting", false);
                        hud.RemoveSpectateUI();
                        hud.gameOverAnimator?.SetTrigger("revive");
                        hud.UpdateHealthUI(100, false);
                        hud.audioListenerLowPass.enabled = false;
                    }
                    
                    localPlayer.hasBegunSpectating = false;
                    localPlayer.hinderedMultiplier = 1f;
                    localPlayer.isMovementHindered = 0;
                    localPlayer.sourcesCausingSinking = 0;
                    localPlayer.reverbPreset = startOfRound.shipReverb;
                }

                // Reset audio effects
                var soundManager = SoundManager.Instance;
                if (soundManager != null)
                {
                    soundManager.earsRingingTimer = 0f;
                    soundManager.playerVoicePitchTargets[localPlayer.playerClientId] = 1f;
                    soundManager.SetPlayerPitch(1f, (int)localPlayer.playerClientId);
                }

                localPlayer.voiceMuffledByEnemy = false;

                // Refresh voice chat
                if (localPlayer.currentVoiceChatIngameSettings == null)
                {
                    startOfRound.RefreshPlayerVoicePlaybackObjects();
                }
                
                if (localPlayer.currentVoiceChatIngameSettings?.voiceAudio != null)
                {
                    var occludeAudio = localPlayer.currentVoiceChatIngameSettings.voiceAudio.GetComponent<OccludeAudio>();
                    if (occludeAudio != null)
                    {
                        occludeAudio.overridingLowPass = false;
                    }
                }

                // Reset spectating state
                localPlayer.spectatedPlayerScript = null;
                startOfRound.SetSpectateCameraToGameOverMode(false, localPlayer);

                // Update living player count
                startOfRound.livingPlayers = startOfRound.connectedPlayersAmount + 1;
                startOfRound.allPlayersDead = false;
                startOfRound.UpdatePlayerVoiceEffects();
                startOfRound.shipAnimator?.ResetTrigger("ShipLeave");

                Debug.Log("[NetworkCheats] SelfRevive: Successfully respawned local player.");
                HUDManager.Instance?.DisplayTip("Self Revive", "You have been respawned at the ship!");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetworkCheats] SelfRevive failed: {ex.Message}");
                HUDManager.Instance?.DisplayTip("Self Revive", "Failed to respawn. Try again.");
            }
        }

        #endregion

        #region Fake Death

        /// Makes you appear dead to other players while staying alive locally.
        /// Calls KillPlayerServerRpc without spawning body.
        /// You can still move around but others see you as dead.
        /// Note: You will actually die when the ship leaves.
        public static void FakeDeath()
        {
            var localPlayer = LethalMenuMod.LocalPlayer;
            if (localPlayer == null)
            {
                Debug.Log("[NetworkCheats] FakeDeath: Local player not found.");
                return;
            }

            if (localPlayer.isPlayerDead)
            {
                Debug.Log("[NetworkCheats] FakeDeath: Player is already dead.");
                return;
            }

            try
            {
                // Enable fake death mode in settings (used by patches to prevent actual death)
                Settings.FakeDeath = true;

                // Call the kill RPC with spawnBody = false (no ragdoll)
                // Other players will receive this and see you as dead
                localPlayer.KillPlayerServerRpc(
                    (int)localPlayer.playerClientId,
                    false,  // spawnBody = false (don't spawn a ragdoll)
                    Vector3.zero,
                    (int)CauseOfDeath.Unknown,
                    0,      // deathAnimation
                    Vector3.zero
                );

                Debug.Log("[NetworkCheats] FakeDeath: Death broadcasted to other players. You appear dead but can still move.");
                HUDManager.Instance?.DisplayTip("Fake Death", "Other players think you're dead!\nYou'll actually die when ship leaves.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetworkCheats] FakeDeath failed: {ex.Message}");
                Settings.FakeDeath = false;
            }
        }

        /// Cancels fake death mode. Use SelfRevive if you actually died.
        public static void CancelFakeDeath()
        {
            Settings.FakeDeath = false;
            Debug.Log("[NetworkCheats] FakeDeath cancelled.");
        }

        #endregion

        #region Host-Only Network Cheats

        /// Revives all dead players. Must be host.
        public static void ReviveAllPlayers()
        {
            var startOfRound = StartOfRound.Instance;
            if (startOfRound == null)
            {
                Debug.LogWarning("[NetworkCheats] Not in game.");
                HUDManager.Instance?.DisplayTip("Revive All", "Not in game.");
                return;
            }

            if (!LethalMenuMod.LocalPlayer?.IsHost == true)
            {
                Debug.LogWarning("[NetworkCheats] Must be host to revive all players.");
                HUDManager.Instance?.DisplayTip("Revive All", "Host only.");
                return;
            }

            // Call ReviveDeadPlayers directly as host
            startOfRound.ReviveDeadPlayers();
            HUDManager.Instance?.HideHUD(false);
            
            // Sync to clients
            startOfRound.Debug_ReviveAllPlayersServerRpc();
            
            Debug.Log("[NetworkCheats] Revived all players.");
            HUDManager.Instance?.DisplayTip("Revive All", "All players revived.");
        }

        /// Unlocks all doors in the facility.
        public static void UnlockAllDoors()
        {
            var doors = UnityEngine.Object.FindObjectsOfType<DoorLock>();
            if (doors == null || doors.Length == 0)
            {
                HUDManager.Instance?.DisplayTip("Unlock Doors", "No doors found.");
                return;
            }

            int count = 0;
            foreach (var door in doors)
            {
                if (door.isLocked)
                {
                    door.UnlockDoorSyncWithServer();
                    count++;
                }
            }

            Debug.Log($"[NetworkCheats] Unlocked {count} doors.");
            HUDManager.Instance?.DisplayTip("Unlock Doors", $"Unlocked {count} doors.");
        }

        /// Locks all doors in the facility. Host only for full effect.
        public static void LockAllDoors()
        {
            var doors = UnityEngine.Object.FindObjectsOfType<DoorLock>();
            if (doors == null || doors.Length == 0)
            {
                HUDManager.Instance?.DisplayTip("Lock Doors", "No doors found.");
                return;
            }

            int count = 0;
            foreach (var door in doors)
            {
                if (!door.isLocked)
                {
                    door.LockDoor(9999f); // Lock for very long time
                    count++;
                }
            }

            Debug.Log($"[NetworkCheats] Locked {count} doors.");
            HUDManager.Instance?.DisplayTip("Lock Doors", $"Locked {count} doors.");
        }

        /// Spawns an enemy at a target position. Must be host.
        /// <param name="enemyName">Name of the enemy type to spawn.</param>
        /// <param name="position">World position to spawn at.</param>
        /// <param name="outsideEnemy">True for outside enemies, false for inside.</param>
        public static void SpawnEnemy(string enemyName, Vector3 position, bool outsideEnemy = false)
        {
            var roundManager = RoundManager.Instance;
            if (roundManager == null)
            {
                HUDManager.Instance?.DisplayTip("Spawn Enemy", "Not in game.");
                return;
            }

            if (!LethalMenuMod.LocalPlayer?.IsHost == true)
            {
                HUDManager.Instance?.DisplayTip("Spawn Enemy", "Host only.");
                return;
            }

            // Find the enemy type
            EnemyType? enemyType = FindEnemyType(enemyName, outsideEnemy);
            if (enemyType == null)
            {
                HUDManager.Instance?.DisplayTip("Spawn Enemy", $"Enemy '{enemyName}' not found.");
                return;
            }

            // Spawn the enemy
            roundManager.SpawnEnemyGameObject(position, 0f, -1, enemyType);
            Debug.Log($"[NetworkCheats] Spawned {enemyName} at {position}");
            HUDManager.Instance?.DisplayTip("Spawn Enemy", $"Spawned {enemyName}.");
        }

        /// Spawns an enemy at a target player's position.
        public static void SpawnEnemyAtPlayer(string enemyName, PlayerControllerB targetPlayer, bool outsideEnemy = false)
        {
            if (targetPlayer == null) return;
            SpawnEnemy(enemyName, targetPlayer.transform.position, outsideEnemy);
        }

        #endregion

        #region Experimentation (Reflection / Private Calls)

        // ============================================================
        // CATEGORY A: LOCAL-ONLY PRIVATE METHODS (NO RPC - WORKS LOCALLY)
        // These are actual private methods that execute locally without network validation.
        // May cause desync between clients.
        // ============================================================

        /// Detonate all landmines via private TriggerMineOnLocalClientByExiting (LOCAL ONLY).
        public static void ExperimentalDetonateLandmines()
        {
            var mines = UnityEngine.Object.FindObjectsOfType<Landmine>();
            if (mines == null || mines.Length == 0)
            {
                HUDManager.Instance?.DisplayTip("Experiment", "No landmines found.");
                return;
            }

            int count = 0;
            foreach (var mine in mines)
            {
                if (mine.hasExploded) continue;
                try
                {
                    // private void TriggerMineOnLocalClientByExiting() - triggers locally
                    ReflectionHelper.InvokePrivate(mine, "TriggerMineOnLocalClientByExiting");
                    count++;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[NetworkCheats] Mine detonation failed: {ex.Message}");
                }
            }

            HUDManager.Instance?.DisplayTip("Experiment", $"Triggered {count} landmines (local).");
        }

        /// Play the landmine detonation animation locally without network RPC.
        public static void ExperimentalPlayMineAnimation()
        {
            var mines = UnityEngine.Object.FindObjectsOfType<Landmine>();
            if (mines == null || mines.Length == 0)
            {
                HUDManager.Instance?.DisplayTip("Experiment", "No landmines found.");
                return;
            }

            int count = 0;
            foreach (var mine in mines)
            {
                try
                {
                    mine.SetOffMineAnimation();
                    count++;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[NetworkCheats] Mine animation failed: {ex.Message}");
                }
            }

            HUDManager.Instance?.DisplayTip("Experiment", $"Played animation on {count} mines (local).");
        }

        /// Set turret mode via private SwitchTurretMode (LOCAL ONLY).
        /// Modes: 0=Detection, 1=Charging, 2=Firing, 3=Berserk
        public static void ExperimentalSetTurretMode(int mode)
        {
            var turrets = UnityEngine.Object.FindObjectsOfType<Turret>();
            if (turrets == null || turrets.Length == 0)
            {
                HUDManager.Instance?.DisplayTip("Experiment", "No turrets found.");
                return;
            }

            string[] modeNames = { "Detection", "Charging", "Firing", "Berserk" };
            string modeName = mode >= 0 && mode < modeNames.Length ? modeNames[mode] : "Unknown";

            int count = 0;
            foreach (var turret in turrets)
            {
                try
                {
                    // private void SwitchTurretMode(int mode) - local state change
                    ReflectionHelper.InvokePrivate(turret, "SwitchTurretMode", mode);
                    count++;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[NetworkCheats] Turret mode failed: {ex.Message}");
                }
            }

            HUDManager.Instance?.DisplayTip("Experiment", $"Set {count} turrets to {modeName} (local).");
        }

        /// Force all turrets into mode 3 (berserk) via private SwitchTurretMode (LOCAL ONLY).
        public static void ExperimentalTurretsBerserk()
        {
            ExperimentalSetTurretMode(3);
        }

        /// Toggle turret enabled state locally via private ToggleTurretEnabledLocalClient (LOCAL ONLY).
        public static void ExperimentalToggleTurretsLocal(bool enabled)
        {
            var turrets = UnityEngine.Object.FindObjectsOfType<Turret>();
            if (turrets == null || turrets.Length == 0)
            {
                HUDManager.Instance?.DisplayTip("Experiment", "No turrets found.");
                return;
            }

            int count = 0;
            foreach (var turret in turrets)
            {
                try
                {
                    // private void ToggleTurretEnabledLocalClient(bool enabled)
                    ReflectionHelper.InvokePrivate(turret, "ToggleTurretEnabledLocalClient", enabled);
                    count++;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[NetworkCheats] Turret toggle failed: {ex.Message}");
                }
            }

            HUDManager.Instance?.DisplayTip("Experiment", $"{(enabled ? "Enabled" : "Disabled")} {count} turrets (local).");
        }

        /// Toggle landmine enabled state locally (LOCAL ONLY).
        public static void ExperimentalToggleLandminesLocal(bool enabled)
        {
            var mines = UnityEngine.Object.FindObjectsOfType<Landmine>();
            if (mines == null || mines.Length == 0)
            {
                HUDManager.Instance?.DisplayTip("Experiment", "No landmines found.");
                return;
            }

            int count = 0;
            foreach (var mine in mines)
            {
                try
                {
                    // public void ToggleMineEnabledLocalClient(bool enabled)
                    ReflectionHelper.InvokePrivate(mine, "ToggleMineEnabledLocalClient", enabled);
                    count++;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[NetworkCheats] Mine toggle failed: {ex.Message}");
                }
            }

            HUDManager.Instance?.DisplayTip("Experiment", $"{(enabled ? "Enabled" : "Disabled")} {count} mines (local).");
        }

        /// Clear a player's held item locally via DespawnHeldObjectOnClient (LOCAL ONLY).
        public static void ExperimentalClearHeldItemLocal(PlayerControllerB target)
        {
            if (target == null)
            {
                HUDManager.Instance?.DisplayTip("Experiment", "No target player.");
                return;
            }

            try
            {
                ReflectionHelper.InvokePrivate(target, "DespawnHeldObjectOnClient");
                HUDManager.Instance?.DisplayTip("Experiment", $"Cleared {target.playerUsername}'s held item (local).");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[NetworkCheats] Local despawn failed: {ex.Message}");
            }
        }

        // ============================================================
        // CATEGORY B: PRIVATE SERVERRPC METHODS (MAY REQUIRE OWNERSHIP)
        // These are actually private ServerRpc methods. Calling them via reflection
        // sends the RPC, but server may reject based on ownership validation.
        // ============================================================

        /// Call the private KillPlayerServerRpc via reflection (PRIVATE RPC - may fail).
        public static void ExperimentalKillPlayerPrivate(PlayerControllerB target)
        {
            if (target == null) return;

            try
            {
                // private void KillPlayerServerRpc(int playerId, bool spawnBody, Vector3 bodyVelocity, int causeOfDeath, int deathAnimation, Vector3 positionOffset)
                ReflectionHelper.InvokePrivate(target,
                    "KillPlayerServerRpc",
                    (int)target.playerClientId,
                    true,
                    Vector3.zero,
                    (int)CauseOfDeath.Unknown,
                    0,
                    Vector3.zero);

                Debug.Log($"[NetworkCheats] KillPlayerServerRpc invoked on {target.playerUsername}");
                HUDManager.Instance?.DisplayTip("Experiment", $"Kill RPC sent for {target.playerUsername}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[NetworkCheats] Kill RPC failed: {ex.Message}");
            }
        }

        /// Send chat message as any player via private AddPlayerChatMessageServerRpc (PRIVATE RPC).
        public static void ExperimentalChatAsPlayer(string message, int playerId)
        {
            var hud = HUDManager.Instance;
            if (hud == null)
            {
                Debug.LogWarning("[NetworkCheats] HUDManager not found.");
                return;
            }

            try
            {
                // private void AddPlayerChatMessageServerRpc(string chatMessage, int playerId)
                ReflectionHelper.InvokePrivate(hud, "AddPlayerChatMessageServerRpc", message, playerId);
                Debug.Log($"[NetworkCheats] Chat as player {playerId}: {message}");
                HUDManager.Instance?.DisplayTip("Experiment", $"Chat RPC sent as player {playerId}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[NetworkCheats] Chat RPC failed: {ex.Message}");
            }
        }

        /// Send system message via private AddTextMessageServerRpc (PRIVATE RPC).
        public static void ExperimentalSystemMessage(string message)
        {
            var hud = HUDManager.Instance;
            if (hud == null)
            {
                Debug.LogWarning("[NetworkCheats] HUDManager not found.");
                return;
            }

            try
            {
                // private void AddTextMessageServerRpc(string chatMessage)
                ReflectionHelper.InvokePrivate(hud, "AddTextMessageServerRpc", message);
                Debug.Log($"[NetworkCheats] System message: {message}");
                HUDManager.Instance?.DisplayTip("Experiment", "System message RPC sent");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[NetworkCheats] System message RPC failed: {ex.Message}");
            }
        }

        /// Despawn the target's held item via private DespawnHeldObjectServerRpc (PRIVATE RPC).
        public static void ExperimentalDespawnHeldItem(PlayerControllerB target)
        {
            if (target == null)
            {
                HUDManager.Instance?.DisplayTip("Experiment", "No target player.");
                return;
            }

            try
            {
                // private void DespawnHeldObjectServerRpc() - no parameters
                ReflectionHelper.InvokePrivate(target, "DespawnHeldObjectServerRpc");
                HUDManager.Instance?.DisplayTip("Experiment", $"Despawn RPC sent for {target.playerUsername}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[NetworkCheats] Despawn RPC failed: {ex.Message}");
            }
        }

        /// Force player position update via private UpdatePlayerPositionServerRpc (PRIVATE RPC).
        public static void ExperimentalForcePosition(PlayerControllerB target, Vector3 position)
        {
            if (target == null) return;

            try
            {
                // private void UpdatePlayerPositionServerRpc(Vector3 newPos, bool inElevator, bool inShipRoom, bool exhausted, bool isPlayerGrounded)
                ReflectionHelper.InvokePrivate(target, "UpdatePlayerPositionServerRpc", 
                    position, false, false, false, true);
                HUDManager.Instance?.DisplayTip("Experiment", $"Position RPC sent for {target.playerUsername}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[NetworkCheats] Position RPC failed: {ex.Message}");
            }
        }

        // ============================================================
        // CATEGORY C: GAME FLOW (PUBLIC RPC; works today in main menu)
        // These are the same public ServerRpc calls already used in the main menu.
        // ============================================================

        /// Force StartGame (HOST ONLY - has IsServer check).
        public static void ExperimentalStartGame()
        {
            var round = StartOfRound.Instance;
            if (round == null)
            {
                HUDManager.Instance?.DisplayTip("Experiment", "Not in game.");
                return;
            }

            // Use the same public RPC already available elsewhere.
            round.StartGameServerRpc();
            HUDManager.Instance?.DisplayTip("Experiment", "StartGameServerRpc invoked (public RPC).");
        }

        /// Force EndGame (HOST ONLY - has IsServer check).
        public static void ExperimentalEndGame()
        {
            var round = StartOfRound.Instance;
            var local = LethalMenuMod.LocalPlayer;
            if (round == null || local == null)
            {
                HUDManager.Instance?.DisplayTip("Experiment", "Not in game.");
                return;
            }

            round.EndGameServerRpc((int)local.playerClientId);
            HUDManager.Instance?.DisplayTip("Experiment", "EndGameServerRpc invoked (public RPC).");
        }

        /// Force moon change (HOST ONLY).
        public static void ExperimentalChangeLevel(int levelId)
        {
            var round = StartOfRound.Instance;
            var terminal = UnityEngine.Object.FindObjectOfType<Terminal>();
            if (round == null || terminal == null)
            {
                HUDManager.Instance?.DisplayTip("Experiment", "Not in game / no terminal.");
                return;
            }

            try
            {
                ReflectionHelper.InvokePrivate(round, "ChangeLevelServerRpc", levelId, terminal.groupCredits);
                HUDManager.Instance?.DisplayTip("Experiment", $"ChangeLevel to {levelId}." + (IsHost() ? "" : " (Not host)"));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[NetworkCheats] ChangeLevel failed: {ex.Message}");
            }
        }

        /// Spawn a ship unlockable (HOST ONLY).
        public static void ExperimentalSpawnUnlockable(int unlockableId)
        {
            var round = StartOfRound.Instance;
            var terminal = UnityEngine.Object.FindObjectOfType<Terminal>();
            if (round == null || terminal == null)
            {
                HUDManager.Instance?.DisplayTip("Experiment", "Not in game / no terminal.");
                return;
            }

            try
            {
                ReflectionHelper.InvokePrivate(round, "BuyShipUnlockableServerRpc", unlockableId, terminal.groupCredits);
                HUDManager.Instance?.DisplayTip("Experiment", $"Unlock {unlockableId}." + (IsHost() ? "" : " (Not host)"));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[NetworkCheats] Unlock failed: {ex.Message}");
            }
        }

        // ============================================================
        // CATEGORY D: PUBLIC RPC WITH RequireOwnership = false (ANY CLIENT)
        // These are public ServerRpc methods that explicitly allow any client to call.
        // Note: TeleportPlayerToVoid() already defined elsewhere in this class.
        // ============================================================

        /// Explode all landmines via PUBLIC ExplodeMineServerRpc (RequireOwnership=false).
        public static void ExplodeMinesViaRpc()
        {
            var mines = UnityEngine.Object.FindObjectsOfType<Landmine>();
            if (mines == null || mines.Length == 0)
            {
                HUDManager.Instance?.DisplayTip("Experiment", "No landmines found.");
                return;
            }

            int count = 0;
            foreach (var mine in mines)
            {
                if (mine.hasExploded) continue;
                try
                {
                    mine.ExplodeMineServerRpc(); // Public, RequireOwnership=false
                    count++;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[NetworkCheats] Mine RPC explode failed: {ex.Message}");
                }
            }

            HUDManager.Instance?.DisplayTip("Experiment", $"Exploded {count} mines via RPC.");
        }

        /// Force turrets berserk via PUBLIC EnterBerserkModeServerRpc (RequireOwnership=false).
        public static void TurretsBerserkViaRpc()
        {
            var turrets = UnityEngine.Object.FindObjectsOfType<Turret>();
            if (turrets == null || turrets.Length == 0)
            {
                HUDManager.Instance?.DisplayTip("Experiment", "No turrets found.");
                return;
            }

            int count = 0;
            var local = LethalMenuMod.LocalPlayer;
            int playerId = local != null ? (int)local.playerClientId : 0;

            foreach (var turret in turrets)
            {
                try
                {
                    turret.EnterBerserkModeServerRpc(playerId); // Public, RequireOwnership=false
                    count++;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[NetworkCheats] Turret berserk RPC failed: {ex.Message}");
                }
            }

            HUDManager.Instance?.DisplayTip("Experiment", $"Berserked {count} turrets via RPC.");
        }

        /// Toggle all turrets via PUBLIC ToggleTurretServerRpc (RequireOwnership=false).
        public static void ToggleTurretsViaRpc(bool enabled)
        {
            var turrets = UnityEngine.Object.FindObjectsOfType<Turret>();
            if (turrets == null || turrets.Length == 0)
            {
                HUDManager.Instance?.DisplayTip("Experiment", "No turrets found.");
                return;
            }

            int count = 0;
            foreach (var turret in turrets)
            {
                try
                {
                    turret.ToggleTurretServerRpc(enabled); // Public, RequireOwnership=false
                    count++;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[NetworkCheats] Turret toggle RPC failed: {ex.Message}");
                }
            }

            HUDManager.Instance?.DisplayTip("Experiment", $"{(enabled ? "Enabled" : "Disabled")} {count} turrets via RPC.");
        }

        // ============================================================
        // CATEGORY E: RPC EXEC STAGE SPOOF (ADVANCED)
        // Manipulates __rpc_exec_stage on NetworkBehaviours to mimic RPC execution.
        // ============================================================

        /// Set __rpc_exec_stage to Execute on any NetworkBehaviour.
        public static void ExperimentalForceRpcExecStageExecute(NetworkBehaviour behaviour)
        {
            if (behaviour == null)
            {
                HUDManager.Instance?.DisplayTip("RPC Exec", "No target NetworkBehaviour.");
                return;
            }

            if (ReflectionHelper.ForceRpcExecStageExecute(behaviour))
            {
                HUDManager.Instance?.DisplayTip("RPC Exec", "exec_stage set to Execute.");
                Debug.Log($"[NetworkCheats] exec_stage set to Execute on {behaviour.GetType().Name}");
            }
            else
            {
                HUDManager.Instance?.DisplayTip("RPC Exec", "Failed to set exec_stage.");
            }
        }

        /// Reset __rpc_exec_stage to None/default on any NetworkBehaviour.
        public static void ExperimentalResetRpcExecStage(NetworkBehaviour behaviour)
        {
            if (behaviour == null)
            {
                HUDManager.Instance?.DisplayTip("RPC Exec", "No target NetworkBehaviour.");
                return;
            }

            if (ReflectionHelper.ResetRpcExecStage(behaviour))
            {
                HUDManager.Instance?.DisplayTip("RPC Exec", "exec_stage reset.");
                Debug.Log($"[NetworkCheats] exec_stage reset on {behaviour.GetType().Name}");
            }
            else
            {
                HUDManager.Instance?.DisplayTip("RPC Exec", "Failed to reset exec_stage.");
            }
        }

        #endregion

        /// Gets all available enemy types for the current level.
        public static string[] GetAvailableEnemyNames()
        {
            var roundManager = RoundManager.Instance;
            if (roundManager?.currentLevel == null) return Array.Empty<string>();

            var enemies = new System.Collections.Generic.List<string>();

            // Inside enemies
            if (roundManager.currentLevel.Enemies != null)
            {
                foreach (var spawnableEnemy in roundManager.currentLevel.Enemies)
                {
                    if (spawnableEnemy?.enemyType != null)
                        enemies.Add(spawnableEnemy.enemyType.enemyName);
                }
            }

            // Outside enemies
            if (roundManager.currentLevel.OutsideEnemies != null)
            {
                foreach (var spawnableEnemy in roundManager.currentLevel.OutsideEnemies)
                {
                    if (spawnableEnemy?.enemyType != null && !enemies.Contains(spawnableEnemy.enemyType.enemyName))
                        enemies.Add("[O] " + spawnableEnemy.enemyType.enemyName);
                }
            }

            return enemies.ToArray();
        }

        /// Finds an EnemyType by name from the current level.
        private static EnemyType? FindEnemyType(string enemyName, bool outsideEnemy)
        {
            var roundManager = RoundManager.Instance;
            if (roundManager?.currentLevel == null) return null;

            // Handle outside enemy prefix
            string searchName = enemyName.StartsWith("[O] ") ? enemyName.Substring(4) : enemyName;

            // Search inside enemies
            if (!outsideEnemy && roundManager.currentLevel.Enemies != null)
            {
                foreach (var spawnableEnemy in roundManager.currentLevel.Enemies)
                {
                    if (spawnableEnemy?.enemyType?.enemyName?.Equals(searchName, StringComparison.OrdinalIgnoreCase) == true)
                        return spawnableEnemy.enemyType;
                }
            }

            // Search outside enemies
            if (roundManager.currentLevel.OutsideEnemies != null)
            {
                foreach (var spawnableEnemy in roundManager.currentLevel.OutsideEnemies)
                {
                    if (spawnableEnemy?.enemyType?.enemyName?.Equals(searchName, StringComparison.OrdinalIgnoreCase) == true)
                        return spawnableEnemy.enemyType;
                }
            }

            // Fallback: search all enemy types in Resources
            var allEnemyTypes = Resources.FindObjectsOfTypeAll<EnemyType>();
            foreach (var type in allEnemyTypes)
            {
                if (type?.enemyName?.Equals(searchName, StringComparison.OrdinalIgnoreCase) == true)
                    return type;
            }

            return null;
        }

        /// Spawns a mimic (masked enemy) that looks like a specific player. Must be host.
        public static void SpawnMimic(PlayerControllerB targetPlayer)
        {
            if (targetPlayer == null)
            {
                HUDManager.Instance?.DisplayTip("Spawn Mimic", "No player selected.");
                return;
            }

            var roundManager = RoundManager.Instance;
            if (roundManager == null)
            {
                HUDManager.Instance?.DisplayTip("Spawn Mimic", "Not in game.");
                return;
            }

            if (!LethalMenuMod.LocalPlayer?.IsHost == true)
            {
                HUDManager.Instance?.DisplayTip("Spawn Mimic", "Host only.");
                return;
            }

            // Find MaskedPlayerEnemy type
            EnemyType? maskedType = FindEnemyType("Masked", false);
            if (maskedType == null)
            {
                // Try to find from all resources
                var allTypes = Resources.FindObjectsOfTypeAll<EnemyType>();
                maskedType = allTypes.FirstOrDefault(t => t.enemyName.Contains("Masked"));
            }

            if (maskedType == null)
            {
                HUDManager.Instance?.DisplayTip("Spawn Mimic", "Masked enemy type not found.");
                return;
            }

            // Spawn at player position
            Vector3 spawnPos = targetPlayer.transform.position;
            float yRot = targetPlayer.transform.eulerAngles.y;

            var netObjRef = roundManager.SpawnEnemyGameObject(spawnPos, yRot, -1, maskedType);
            
            NetworkObject? networkObject;
            if (netObjRef.TryGet(out networkObject))
            {
                var mimic = networkObject.GetComponent<MaskedPlayerEnemy>();
                if (mimic != null)
                {
                    mimic.SetSuit(targetPlayer.currentSuitID);
                    mimic.mimickingPlayer = targetPlayer;
                    mimic.SetEnemyOutside(!targetPlayer.isInsideFactory);
                    Debug.Log($"[NetworkCheats] Spawned mimic of {targetPlayer.playerUsername}");
                    HUDManager.Instance?.DisplayTip("Spawn Mimic", $"Spawned mimic of {targetPlayer.playerUsername}.");
                }
            }
        }

        /// Toggle all big doors (ship doors, facility doors).
        public static void ToggleBigDoors()
        {
            var bigDoors = UnityEngine.Object.FindObjectsOfType<TerminalAccessibleObject>();
            if (bigDoors == null || bigDoors.Length == 0)
            {
                HUDManager.Instance?.DisplayTip("Big Doors", "No big doors found.");
                return;
            }

            foreach (var door in bigDoors)
            {
                if (door.isBigDoor)
                {
                    door.SetDoorToggleLocalClient();
                }
            }

            Debug.Log("[NetworkCheats] Toggled all big doors.");
            HUDManager.Instance?.DisplayTip("Big Doors", "Toggled all big doors.");
        }

        /// Teleports a player to another player's position.
        /// Uses direct transform manipulation + network sync for remote players.
        public static void TeleportPlayerToPlayer(PlayerControllerB source, PlayerControllerB target)
        {
            if (source == null || target == null) return;
            TeleportPlayerToPosition(source, target.transform.position);
        }

        /// Teleports ANY player to a specific position.
        /// For local player: uses TeleportPlayer directly.
        /// For remote players: uses direct transform + serverPosition manipulation (works as host).
        public static void TeleportPlayerToPosition(PlayerControllerB target, Vector3 position)
        {
            if (target == null || target.isPlayerDead)
            {
                Debug.Log("[NetworkCheats] TeleportPlayerToPosition: Target is null or dead.");
                return;
            }

            var localPlayer = LethalMenuMod.LocalPlayer;
            
            // If teleporting local player, use normal method
            if (target == localPlayer)
            {
                target.TeleportPlayer(position);
                Debug.Log($"[NetworkCheats] Teleported self to {position}");
                return;
            }

            // For remote players, we need host privileges
            if (localPlayer == null || !localPlayer.IsHost)
            {
                HUDManager.Instance?.DisplayTip("Teleport", "Host only for remote players.");
                return;
            }

            // Direct manipulation approach - works because host has authority
            // 1. Set transform position
            target.transform.position = position;
            
            // 2. Set serverPlayerPosition to sync across network
            target.serverPlayerPosition = position;
            
            // 3. Update CharacterController if available
            if (target.thisController != null && target.thisController.enabled)
            {
                target.thisController.enabled = false;
                target.transform.position = position;
                target.thisController.enabled = true;
            }

            // 4. Try to sync via UpdatePlayerPositionServerRpc using reflection
            // This forces position update to all clients
            try
            {
                // UpdatePlayerPositionServerRpc is private, so use reflection
                // Signature: UpdatePlayerPositionServerRpc(Vector3 newPos, bool inElevator, bool inShipRoom, bool exhausted, bool isPlayerGrounded)
                ReflectionHelper.InvokePrivate(target, "UpdatePlayerPositionServerRpc", 
                    position, // newPos
                    target.isInElevator, // inElevator
                    target.isInHangarShipRoom, // isInShip
                    target.isExhausted, // exhausted
                    true // isPlayerGrounded
                );
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[NetworkCheats] UpdatePlayerPositionServerRpc reflection failed: {ex.Message}");
                // Fallback: the direct transform manipulation should still work for host
            }

            // 5. Trigger teleport state to prevent fall damage etc
            target.teleportedLastFrame = true;
            
            Debug.Log($"[NetworkCheats] Teleported {target.playerUsername} to {position}");
            HUDManager.Instance?.DisplayTip("Teleport", $"Teleported {target.playerUsername}.");
        }

        /// Teleports all players to the local player's position. Trolling feature.
        public static void TeleportAllToMe()
        {
            var localPlayer = LethalMenuMod.LocalPlayer;
            if (localPlayer == null) return;

            if (!localPlayer.IsHost)
            {
                HUDManager.Instance?.DisplayTip("Teleport All", "Host only.");
                return;
            }

            var players = StartOfRound.Instance?.allPlayerScripts;
            if (players == null) return;

            int count = 0;
            foreach (var player in players)
            {
                if (player != null && player != localPlayer && player.isPlayerControlled)
                {
                    TeleportPlayerToPosition(player, localPlayer.transform.position);
                    count++;
                }
            }

            HUDManager.Instance?.DisplayTip("Teleport All", $"Teleported {count} players to you.");
        }

        /// Teleports a player to a random position on the map (inside or outside).
        public static void TeleportPlayerRandom(PlayerControllerB target, bool inside = true)
        {
            if (target == null) return;

            var roundManager = RoundManager.Instance;
            if (roundManager == null) return;

            Vector3 randomPos;
            if (inside && roundManager.insideAINodes != null && roundManager.insideAINodes.Length > 0)
            {
                var node = roundManager.insideAINodes[UnityEngine.Random.Range(0, roundManager.insideAINodes.Length)];
                randomPos = node.transform.position;
            }
            else if (!inside && roundManager.outsideAINodes != null && roundManager.outsideAINodes.Length > 0)
            {
                var node = roundManager.outsideAINodes[UnityEngine.Random.Range(0, roundManager.outsideAINodes.Length)];
                randomPos = node.transform.position;
            }
            else
            {
                // Fallback: random offset from current position
                randomPos = target.transform.position + UnityEngine.Random.insideUnitSphere * 50f;
            }

            TeleportPlayerToPosition(target, randomPos);
        }

        /// Teleports a player to the void/death zone.
        public static void TeleportPlayerToVoid(PlayerControllerB target)
        {
            var startOfRound = StartOfRound.Instance;
            if (startOfRound?.notSpawnedPosition == null)
            {
                Debug.Log("[NetworkCheats] TeleportPlayerToVoid: notSpawnedPosition not found.");
                return;
            }

            TeleportPlayerToPosition(target, startOfRound.notSpawnedPosition.position);
            HUDManager.Instance?.DisplayTip("Void", $"Sent {target?.playerUsername ?? "player"} to the void.");
        }

        /// Swap positions of two players.
        public static void SwapPlayerPositions(PlayerControllerB player1, PlayerControllerB player2)
        {
            if (player1 == null || player2 == null) return;

            var pos1 = player1.transform.position;
            var pos2 = player2.transform.position;

            TeleportPlayerToPosition(player1, pos2);
            TeleportPlayerToPosition(player2, pos1);

            Debug.Log($"[NetworkCheats] Swapped positions of {player1.playerUsername} and {player2.playerUsername}");
            HUDManager.Instance?.DisplayTip("Swap", $"Swapped {player1.playerUsername} ↔ {player2.playerUsername}");
        }

        /// Makes noise at a position to attract enemies.
        public static void MakeNoise(Vector3 position, float range = 1f, float loudness = 1f)
        {
            RoundManager.Instance?.PlayAudibleNoise(position, range, loudness, 0, false, 0);
            Debug.Log($"[NetworkCheats] Made noise at {position}");
        }

        /// Makes noise at local player position.
        public static void MakeNoiseAtMe(float range = 50f, float loudness = 1f)
        {
            var localPlayer = LethalMenuMod.LocalPlayer;
            if (localPlayer == null) return;
            MakeNoise(localPlayer.transform.position, range, loudness);
            HUDManager.Instance?.DisplayTip("Noise", $"Made loud noise (range {range})");
        }

        /// Makes noise at camera/freecam position.
        public static void MakeNoiseAtCamera(float range = 50f, float loudness = 1f)
        {
            var cam = Camera.main;
            if (cam == null) return;
            MakeNoise(cam.transform.position, range, loudness);
            HUDManager.Instance?.DisplayTip("Noise", $"Made loud noise (range {range})");
        }

        /// Set game timescale. 1.0 = normal, 2.0 = 2x speed, 0.5 = half speed.
        /// Host only for proper multiplayer sync.
        public static void SetTimescale(float scale)
        {
            scale = Mathf.Clamp(scale, 0.1f, 10f);
            Time.timeScale = scale;
            Debug.Log($"[NetworkCheats] Timescale set to {scale}");
            HUDManager.Instance?.DisplayTip("Timescale", $"Game speed: {scale:F1}x");
        }

        /// Set the profit quota. Host only. Syncs to all clients.
        public static void SetQuota(int amount, int fulfilled = -1)
        {
            var timeOfDay = TimeOfDay.Instance;
            if (timeOfDay == null)
            {
                HUDManager.Instance?.DisplayTip("Quota", "Not in game.");
                return;
            }

            if (!LethalMenuMod.LocalPlayer?.IsHost == true)
            {
                HUDManager.Instance?.DisplayTip("Quota", "Host only.");
                return;
            }

            timeOfDay.profitQuota = amount;
            if (fulfilled >= 0)
            {
                timeOfDay.quotaFulfilled = fulfilled;
            }
            
            timeOfDay.UpdateProfitQuotaCurrentTime();

            Debug.Log($"[NetworkCheats] Quota set to {amount}, fulfilled: {timeOfDay.quotaFulfilled}");
            HUDManager.Instance?.DisplayTip("Quota", $"Quota: {amount} (Fulfilled: {timeOfDay.quotaFulfilled})");
        }

        /// Set fulfilled amount only (how much you've sold). Host only.
        public static void SetQuotaFulfilled(int fulfilled)
        {
            var timeOfDay = TimeOfDay.Instance;
            if (timeOfDay == null)
            {
                HUDManager.Instance?.DisplayTip("Quota", "Not in game.");
                return;
            }

            if (!LethalMenuMod.LocalPlayer?.IsHost == true)
            {
                HUDManager.Instance?.DisplayTip("Quota", "Host only.");
                return;
            }

            timeOfDay.quotaFulfilled = fulfilled;
            timeOfDay.UpdateProfitQuotaCurrentTime();

            Debug.Log($"[NetworkCheats] Quota fulfilled set to {fulfilled}");
            HUDManager.Instance?.DisplayTip("Quota", $"Fulfilled: ${fulfilled} / ${timeOfDay.profitQuota}");
        }

        /// Auto-sell items to meet quota exactly (or as close as possible).
        /// Sells from ship items until quota is met.
        public static void SellQuota()
        {
            var timeOfDay = TimeOfDay.Instance;
            var localPlayer = LethalMenuMod.LocalPlayer;
            if (timeOfDay == null || localPlayer == null)
            {
                HUDManager.Instance?.DisplayTip("Sell Quota", "Not in game.");
                return;
            }

            int quotaLeft = timeOfDay.profitQuota - timeOfDay.quotaFulfilled;
            if (quotaLeft <= 0)
            {
                HUDManager.Instance?.DisplayTip("Sell Quota", "Quota already met!");
                return;
            }

            // Find deposit desk (Company selling desk)
            var depositDesk = UnityEngine.Object.FindObjectOfType<DepositItemsDesk>();
            if (depositDesk == null)
            {
                HUDManager.Instance?.DisplayTip("Sell Quota", "Not at Company (no sell desk).");
                return;
            }

            // Get company buy rate
            float buyRate = StartOfRound.Instance?.companyBuyingRate ?? 1f;

            // Find all scrap items on ship
            var allItems = UnityEngine.Object.FindObjectsOfType<GrabbableObject>();
            var sellableItems = new System.Collections.Generic.List<GrabbableObject>();
            
            foreach (var item in allItems)
            {
                if (item == null || item.isHeld || item.isHeldByEnemy) continue;
                if (!item.itemProperties.isScrap) continue;
                if (item.scrapValue <= 0) continue;
                
                // Check if it's on the ship or in the sell area
                if (item.isInShipRoom || item.isInElevator)
                {
                    sellableItems.Add(item);
                }
            }

            if (sellableItems.Count == 0)
            {
                HUDManager.Instance?.DisplayTip("Sell Quota", "No sellable items on ship.");
                return;
            }

            // Sort by value (sell cheapest first to get close to quota)
            sellableItems.Sort((a, b) => a.scrapValue.CompareTo(b.scrapValue));

            int totalSold = 0;
            int valueNeeded = quotaLeft;
            var itemsToSell = new System.Collections.Generic.List<GrabbableObject>();

            foreach (var item in sellableItems)
            {
                if (valueNeeded <= 0) break;
                
                int itemValue = (int)(item.scrapValue * buyRate);
                itemsToSell.Add(item);
                totalSold += itemValue;
                valueNeeded -= itemValue;
            }

            if (itemsToSell.Count == 0)
            {
                HUDManager.Instance?.DisplayTip("Sell Quota", "No items selected to sell.");
                return;
            }

            // Place items on desk counter and sell
            foreach (var item in itemsToSell)
            {
                // Teleport item to desk
                var deskPos = depositDesk.deskObjectsContainer.transform.position;
                item.transform.position = deskPos;
                item.targetFloorPosition = deskPos;
                
                // Add to desk's item list
                depositDesk.AddObjectToDeskServerRpc(item.NetworkObject);
            }

            // Trigger sell
            depositDesk.SellItemsOnServer();

            // Refresh local quota display after selling
            timeOfDay.UpdateProfitQuotaCurrentTime();

            Debug.Log($"[NetworkCheats] Sold {itemsToSell.Count} items for ~${totalSold}");
            HUDManager.Instance?.DisplayTip("Sell Quota", $"Sold {itemsToSell.Count} items (~${totalSold})");
        }

        /// Get current quota info.
        public static (int quota, int fulfilled, int remaining) GetQuotaInfo()
        {
            var timeOfDay = TimeOfDay.Instance;
            if (timeOfDay == null) return (0, 0, 0);
            
            int quota = timeOfDay.profitQuota;
            int fulfilled = timeOfDay.quotaFulfilled;
            int remaining = quota - fulfilled;
            return (quota, fulfilled, remaining);
        }

        /// Target all enemies at a specific player. Ultimate troll move.
        public static void MobPlayer(PlayerControllerB targetPlayer, bool teleportEnemies = false)
        {
            if (targetPlayer == null)
            {
                HUDManager.Instance?.DisplayTip("Mob", "No target player.");
                return;
            }

            var localPlayer = LethalMenuMod.LocalPlayer;
            if (localPlayer == null) return;

            var enemies = UnityEngine.Object.FindObjectsOfType<EnemyAI>();
            if (enemies == null || enemies.Length == 0)
            {
                HUDManager.Instance?.DisplayTip("Mob", "No enemies found.");
                return;
            }

            int count = 0;
            foreach (var enemy in enemies)
            {
                if (enemy == null || enemy.isEnemyDead) continue;
                
                // Skip certain enemy types that can't target players
                if (enemy is DocileLocustBeesAI || enemy is DoublewingAI || enemy is BlobAI || enemy is DressGirlAI)
                    continue;

                try
                {
                    // Take ownership and set target
                    enemy.ChangeEnemyOwnerServerRpc(localPlayer.actualClientId);
                    enemy.targetPlayer = targetPlayer;
                    enemy.movingTowardsTargetPlayer = true;
                    
                    // Set aggressive state
                    enemy.SwitchToBehaviourState(1); // Usually chase/attack state
                    
                    // Teleport enemy near target if requested
                    if (teleportEnemies)
                    {
                        Vector3 offset = UnityEngine.Random.insideUnitSphere * 5f;
                        offset.y = 0;
                        Vector3 newPos = targetPlayer.transform.position + offset;
                        
                        // Update all position references for proper network sync
                        enemy.transform.position = newPos;
                        enemy.serverPosition = newPos;
                        
                        // Warp the NavMesh agent if it exists
                        if (enemy.agent != null && enemy.agent.isOnNavMesh)
                        {
                            enemy.agent.Warp(newPos);
                        }
                        
                        // Force sync to all clients via RPC
                        enemy.SyncPositionToClients();
                    }
                    
                    count++;
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[NetworkCheats] Failed to mob enemy {enemy.enemyType?.enemyName}: {ex.Message}");
                }
            }

            Debug.Log($"[NetworkCheats] Mobbed {count} enemies on {targetPlayer.playerUsername}");
            HUDManager.Instance?.DisplayTip("Mob", $"{count} enemies targeting {targetPlayer.playerUsername}!");
        }

        /// Stun all enemies near a position.
        public static void StunEnemiesAtPosition(Vector3 position, float radius = 10f, float stunDuration = 5f)
        {
            var enemies = UnityEngine.Object.FindObjectsOfType<EnemyAI>();
            int count = 0;

            foreach (var enemy in enemies)
            {
                if (enemy == null || enemy.isEnemyDead) continue;
                
                float distance = Vector3.Distance(enemy.transform.position, position);
                if (distance <= radius)
                {
                    enemy.SetEnemyStunned(true, stunDuration);
                    count++;
                }
            }

            HUDManager.Instance?.DisplayTip("Stun", $"Stunned {count} enemies.");
        }

        /// Stun enemy/turret/landmine that the camera is looking at.
        public static void StunAtCrosshair()
        {
            var cam = Camera.main;
            if (cam == null) return;

            Ray ray = new Ray(cam.transform.position, cam.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                // Check for enemy
                var enemy = hit.collider.GetComponentInParent<EnemyAI>();
                if (enemy != null)
                {
                    enemy.SetEnemyStunned(true, 5f);
                    HUDManager.Instance?.DisplayTip("Stun", $"Stunned {enemy.enemyType?.enemyName}");
                    return;
                }

                // Check for turret
                var turret = hit.collider.GetComponent<Turret>();
                if (turret != null)
                {
                    var terminalObj = turret.GetComponent<TerminalAccessibleObject>();
                    if (terminalObj != null)
                    {
                        terminalObj.CallFunctionFromTerminal();
                        HUDManager.Instance?.DisplayTip("Stun", "Disabled turret.");
                    }
                    return;
                }

                // Check for landmine
                var landmine = hit.collider.GetComponent<Landmine>();
                if (landmine != null)
                {
                    var terminalObj = landmine.GetComponent<TerminalAccessibleObject>();
                    if (terminalObj != null)
                    {
                        terminalObj.CallFunctionFromTerminal();
                        HUDManager.Instance?.DisplayTip("Stun", "Disabled landmine.");
                    }
                    return;
                }
            }

            HUDManager.Instance?.DisplayTip("Stun", "Nothing to stun.");
        }

        /// Teleport player to the void (notSpawnedPosition - instant death).
        public static void SendToVoid(PlayerControllerB targetPlayer)
        {
            if (targetPlayer == null)
            {
                HUDManager.Instance?.DisplayTip("Void", "No target player.");
                return;
            }

            var startOfRound = StartOfRound.Instance;
            if (startOfRound == null)
            {
                HUDManager.Instance?.DisplayTip("Void", "Not in game.");
                return;
            }

            // notSpawnedPosition is where players go when not spawned (the void)
            Vector3 voidPos = startOfRound.notSpawnedPosition.position;
            targetPlayer.TeleportPlayer(voidPos);
            
            Debug.Log($"[NetworkCheats] Sent {targetPlayer.playerUsername} to the void at {voidPos}");
            HUDManager.Instance?.DisplayTip("Void", $"Sent {targetPlayer.playerUsername} to the void!");
        }

        /// Explode a jetpack on a player. Spawns one if needed.
        public static void BombPlayer(PlayerControllerB targetPlayer)
        {
            if (targetPlayer == null)
            {
                HUDManager.Instance?.DisplayTip("Bomb", "No target player.");
                return;
            }

            // Find an existing jetpack first
            var jetpacks = UnityEngine.Object.FindObjectsOfType<JetpackItem>();
            JetpackItem? jetpack = null;
            
            foreach (var j in jetpacks)
            {
                if (j != null && !j.isHeld && !j.isHeldByEnemy)
                {
                    jetpack = j;
                    break;
                }
            }

            // No jetpack? Spawn one
            if (jetpack == null)
            {
                var startOfRound = StartOfRound.Instance;
                if (startOfRound?.allItemsList?.itemsList == null)
                {
                    HUDManager.Instance?.DisplayTip("Bomb", "Cannot spawn jetpack (item list null).");
                    return;
                }

                // Find jetpack in item list
                Item? jetpackItem = null;
                foreach (var item in startOfRound.allItemsList.itemsList)
                {
                    if (item != null && item.itemName.Contains("Jetpack", StringComparison.OrdinalIgnoreCase))
                    {
                        jetpackItem = item;
                        break;
                    }
                }

                if (jetpackItem?.spawnPrefab == null)
                {
                    HUDManager.Instance?.DisplayTip("Bomb", "Jetpack item not found in game.");
                    return;
                }

                // Spawn the jetpack
                Vector3 spawnPos = targetPlayer.transform.position + Vector3.up * 2f;
                GameObject obj = UnityEngine.Object.Instantiate(jetpackItem.spawnPrefab, spawnPos, Quaternion.identity);
                if (obj.TryGetComponent(out NetworkObject netObj))
                {
                    netObj.Spawn();
                    jetpack = obj.GetComponent<JetpackItem>();
                }
                else
                {
                    UnityEngine.Object.Destroy(obj);
                    HUDManager.Instance?.DisplayTip("Bomb", "Failed to spawn jetpack.");
                    return;
                }
            }

            if (jetpack == null)
            {
                HUDManager.Instance?.DisplayTip("Bomb", "Jetpack spawn failed.");
                return;
            }

            // Position and explode
            jetpack.transform.position = targetPlayer.transform.position;
            jetpack.ExplodeJetpackServerRpc();
            
            Debug.Log($"[NetworkCheats] Bombed {targetPlayer.playerUsername}");
            HUDManager.Instance?.DisplayTip("Bomb", $"Jetpack exploded on {targetPlayer.playerUsername}!");
        }

        /// Lag a player by spawning 10 Brackens and passing AI computation to them.
        public static void LagPlayer(PlayerControllerB targetPlayer)
        {
            if (targetPlayer == null)
            {
                HUDManager.Instance?.DisplayTip("Lag", "No target player.");
                return;
            }

            var localPlayer = LethalMenuMod.LocalPlayer;
            if (localPlayer == null) return;

            // Find Bracken enemy type
            EnemyType? brackenType = null;
            var allEnemyTypes = Resources.FindObjectsOfTypeAll<EnemyType>();
            foreach (var type in allEnemyTypes)
            {
                if (type?.enemyName != null && 
                    (type.enemyName.Contains("Flowerman", StringComparison.OrdinalIgnoreCase) ||
                     type.enemyName.Contains("Bracken", StringComparison.OrdinalIgnoreCase)))
                {
                    brackenType = type;
                    break;
                }
            }

            if (brackenType?.enemyPrefab == null)
            {
                HUDManager.Instance?.DisplayTip("Lag", "Bracken enemy type not found.");
                return;
            }

            // Spawn 10 Brackens and set them on target
            Vector3 basePos = targetPlayer.transform.position;
            int spawnCount = 10;
            int successCount = 0;

            for (int i = 0; i < spawnCount; i++)
            {
                // Spread them around the target
                Vector3 offset = new Vector3(
                    UnityEngine.Random.Range(-15f, 15f),
                    0f,
                    UnityEngine.Random.Range(-15f, 15f)
                );
                Vector3 spawnPos = basePos + offset;

                GameObject enemyObj = UnityEngine.Object.Instantiate(brackenType.enemyPrefab, spawnPos, Quaternion.identity);
                if (!enemyObj.TryGetComponent(out NetworkObject networkObject))
                {
                    UnityEngine.Object.Destroy(enemyObj);
                    continue;
                }

                if (!enemyObj.TryGetComponent(out FlowermanAI bracken))
                {
                    UnityEngine.Object.Destroy(enemyObj);
                    continue;
                }

                // Spawn on network
                networkObject.Spawn(true);

                // Pass ownership to target - their client computes pathfinding
                bracken.ChangeEnemyOwnerServerRpc(targetPlayer.actualClientId);
                
                // Make it aggro
                bracken.targetPlayer = targetPlayer;
                bracken.movingTowardsTargetPlayer = true;
                bracken.EnterAngerModeServerRpc(float.MaxValue);

                // Make invisible - do it after a delay so meshes are initialized
                LethalMenuMod.Instance?.StartCoroutine(MakeEnemyInvisibleDelayed(bracken));

                successCount++;
            }

            Debug.Log($"[NetworkCheats] Spawned {successCount} INVISIBLE Brackens targeting {targetPlayer.playerUsername}");
            HUDManager.Instance?.DisplayTip("Lag", $"Spawned {successCount} invisible Brackens on {targetPlayer.playerUsername}!");
        }

        private static System.Collections.IEnumerator MakeEnemyInvisibleDelayed(EnemyAI enemy)
        {
            // Wait a frame for meshes to initialize
            yield return null;
            yield return null;
            
            if (enemy != null && !enemy.isEnemyDead)
            {
                // Set all mesh renderers to invisible layer (23)
                enemy.EnableEnemyMesh(false, true);
                
                // Also directly disable renderers as backup
                if (enemy.skinnedMeshRenderers != null)
                {
                    foreach (var renderer in enemy.skinnedMeshRenderers)
                    {
                        if (renderer != null) renderer.enabled = false;
                    }
                }
                if (enemy.meshRenderers != null)
                {
                    foreach (var renderer in enemy.meshRenderers)
                    {
                        if (renderer != null) renderer.enabled = false;
                    }
                }
            }
        }

        /// Spin all ship objects. Kind of lame but it's in the reference.
        public static void SpinShipObjects(float duration = 5f)
        {
            var shipObjects = UnityEngine.Object.FindObjectsOfType<PlaceableShipObject>();
            if (shipObjects == null || shipObjects.Length == 0)
            {
                HUDManager.Instance?.DisplayTip("Spin", "No ship objects found.");
                return;
            }

            // Start spinning coroutine for each object
            foreach (var obj in shipObjects)
            {
                if (obj != null)
                {
                    LethalMenuMod.Instance?.StartCoroutine(SpinObjectCoroutine(obj, duration));
                }
            }

            HUDManager.Instance?.DisplayTip("Spin", $"Spinning {shipObjects.Length} ship objects!");
        }

        private static System.Collections.IEnumerator SpinObjectCoroutine(PlaceableShipObject obj, float duration)
        {
            float elapsed = 0f;
            Vector3 originalPos = obj.transform.position;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float rotation = elapsed * 810f; // Fast spin
                
                // Use PlaceShipObject to sync rotation
                ShipBuildModeManager.Instance?.PlaceShipObject(
                    originalPos,
                    new Vector3(0f, rotation, 0f),
                    obj,
                    false
                );
                
                yield return null;
            }
        }

        // Active spin coroutines per player
        private static System.Collections.Generic.Dictionary<ulong, Coroutine> _activeSpinCoroutines = new System.Collections.Generic.Dictionary<ulong, Coroutine>();

        /// Spin a player - camera, model, or both.
        public static void SpinPlayer(PlayerControllerB targetPlayer, float duration, bool spinCamera, bool spinModel)
        {
            if (targetPlayer == null)
            {
                HUDManager.Instance?.DisplayTip("Spin", "No target player.");
                return;
            }

            if (!spinCamera && !spinModel)
            {
                HUDManager.Instance?.DisplayTip("Spin", "Select camera or model to spin.");
                return;
            }

            // Stop existing spin on this player
            StopSpinPlayer(targetPlayer);

            // Start new spin
            var coroutine = LethalMenuMod.Instance?.StartCoroutine(SpinPlayerCoroutine(targetPlayer, duration, spinCamera, spinModel));
            if (coroutine != null)
            {
                _activeSpinCoroutines[targetPlayer.playerClientId] = coroutine;
            }

            string mode = spinCamera && spinModel ? "camera+model" : (spinCamera ? "camera" : "model");
            HUDManager.Instance?.DisplayTip("Spin", $"Spinning {targetPlayer.playerUsername} ({mode}) for {duration}s!");
        }

        /// Stop spinning a player and reset their camera and model.
        public static void StopSpinPlayer(PlayerControllerB targetPlayer)
        {
            if (targetPlayer == null) return;

            if (_activeSpinCoroutines.TryGetValue(targetPlayer.playerClientId, out var coroutine))
            {
                if (coroutine != null && LethalMenuMod.Instance != null)
                {
                    LethalMenuMod.Instance.StopCoroutine(coroutine);
                }
                _activeSpinCoroutines.Remove(targetPlayer.playerClientId);
                
                // Reset camera roll to 0
                if (targetPlayer.gameplayCamera != null)
                {
                    var cam = targetPlayer.gameplayCamera.transform;
                    cam.localEulerAngles = new Vector3(cam.localEulerAngles.x, cam.localEulerAngles.y, 0f);
                }
                
                // Reset model rotation
                if (targetPlayer.thisPlayerModel != null)
                {
                    targetPlayer.thisPlayerModel.transform.localRotation = Quaternion.identity;
                }
            }
        }

        /// Check if player is being spun.
        public static bool IsSpinning(PlayerControllerB player)
        {
            return player != null && _activeSpinCoroutines.ContainsKey(player.playerClientId);
        }

        private static System.Collections.IEnumerator SpinPlayerCoroutine(PlayerControllerB player, float duration, bool spinCamera, bool spinModel)
        {
            float elapsed = 0f;
            float spinSpeed = 720f; // 2 full rotations per second
            
            // Get the actual model mesh transform (NOT the body transform which gets overwritten by input)
            Transform? modelTransform = player?.thisPlayerModel?.transform;

            while (elapsed < duration && player != null && !player.isPlayerDead)
            {
                elapsed += Time.deltaTime;
                float rotation = elapsed * spinSpeed;

                // Spin camera (z-roll for disorientation) - ONLY if camera enabled
                if (spinCamera && player.gameplayCamera != null)
                {
                    var cam = player.gameplayCamera.transform;
                    cam.localEulerAngles = new Vector3(cam.localEulerAngles.x, cam.localEulerAngles.y, rotation % 360f);
                }

                // Spin model ONLY - rotate the ACTUAL MESH, not the body transform
                if (spinModel && modelTransform != null)
                {
                    // Rotate the model mesh directly - game doesn't override this
                    modelTransform.localRotation = Quaternion.Euler(
                        modelTransform.localEulerAngles.x,
                        rotation % 360f,
                        modelTransform.localEulerAngles.z
                    );
                    
                    // Also sync to network for others to see
                    player.UpdatePlayerRotationServerRpc((short)0, (short)(rotation % 360f));
                }

                // FORCE camera to stay level if model-only (no camera spin)
                if (spinModel && !spinCamera && player.gameplayCamera != null)
                {
                    var cam = player.gameplayCamera.transform;
                    cam.localEulerAngles = new Vector3(cam.localEulerAngles.x, cam.localEulerAngles.y, 0f);
                }

                yield return null;
            }

            // Reset model rotation
            if (spinModel && modelTransform != null)
            {
                modelTransform.localRotation = Quaternion.identity;
            }

            // Reset camera roll to 0 when done
            if (player?.gameplayCamera != null)
            {
                var cam = player.gameplayCamera.transform;
                cam.localEulerAngles = new Vector3(cam.localEulerAngles.x, cam.localEulerAngles.y, 0f);
            }

            // Remove from active list
            if (player != null)
            {
                _activeSpinCoroutines.Remove(player.playerClientId);
            }
        }

        #region Cosmetic - XP/Level Manipulation

        /// Sets local player XP to a specific value and syncs to others.
        /// Purely cosmetic - only changes the badge and displayed level.
        public static void SetPlayerXP(int xp)
        {
            var hud = HUDManager.Instance;
            if (hud == null || LethalMenuMod.LocalPlayer == null)
            {
                Debug.LogWarning("[NetworkCheats] Not in game.");
                return;
            }

            xp = Mathf.Max(0, xp);
            
            // Find the level index for this XP
            int levelIndex = 0;
            for (int i = 0; i < hud.playerLevels.Length; i++)
            {
                if (xp >= hud.playerLevels[i].XPMin && xp < hud.playerLevels[i].XPMax)
                {
                    levelIndex = i;
                    break;
                }
                if (i == hud.playerLevels.Length - 1)
                {
                    levelIndex = i; // Max level
                }
            }

            // Set locally
            hud.localPlayerXP = xp;
            hud.localPlayerLevel = levelIndex;
            
            // Update UI
            hud.playerLevelText.text = hud.playerLevels[levelIndex].levelName;
            hud.playerLevelXPCounter.text = $"{xp} EXP";
            if (hud.playerLevels[levelIndex].XPMax > 0)
            {
                hud.playerLevelMeter.fillAmount = (float)(xp - hud.playerLevels[levelIndex].XPMin) / 
                    (hud.playerLevels[levelIndex].XPMax - hud.playerLevels[levelIndex].XPMin);
            }

            // Sync to other players
            bool hasBeta = ES3.Load<bool>("playedDuringBeta", "LCGeneralSaveData", true);
            hud.SyncPlayerLevelServerRpc((int)LethalMenuMod.LocalPlayer.playerClientId, levelIndex, hasBeta);

            Debug.Log($"[NetworkCheats] Set XP to {xp} (Level: {hud.playerLevels[levelIndex].levelName})");
            HUDManager.Instance?.DisplayTip("XP Set", $"{xp} XP - {hud.playerLevels[levelIndex].levelName}");
        }

        /// Sets local player to a specific level index.
        public static void SetPlayerLevel(int levelIndex)
        {
            var hud = HUDManager.Instance;
            if (hud == null || LethalMenuMod.LocalPlayer == null)
            {
                Debug.LogWarning("[NetworkCheats] Not in game.");
                return;
            }

            levelIndex = Mathf.Clamp(levelIndex, 0, hud.playerLevels.Length - 1);
            int xp = hud.playerLevels[levelIndex].XPMin;
            SetPlayerXP(xp);
        }

        /// Gets all available level names for UI.
        public static string[] GetLevelNames()
        {
            var hud = HUDManager.Instance;
            if (hud?.playerLevels == null) return new[] { "Unknown" };

            string[] names = new string[hud.playerLevels.Length];
            for (int i = 0; i < hud.playerLevels.Length; i++)
            {
                names[i] = hud.playerLevels[i].levelName ?? $"Level {i}";
            }
            return names;
        }

        /// Gets current player level index.
        public static int GetCurrentLevelIndex()
        {
            return HUDManager.Instance?.localPlayerLevel ?? 0;
        }

        /// Gets current player XP.
        public static int GetCurrentXP()
        {
            return HUDManager.Instance?.localPlayerXP ?? 0;
        }

        /// Sets XP to maximum for flex purposes.
        public static void MaxOutXP()
        {
            var hud = HUDManager.Instance;
            if (hud?.playerLevels == null) return;

            int maxLevel = hud.playerLevels.Length - 1;
            int maxXP = hud.playerLevels[maxLevel].XPMax - 1;
            SetPlayerXP(maxXP);
        }

        #endregion
    }
}