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
    /// <summary>
    /// Network exploits using ServerRpc/ClientRpc methods.
    /// These call game network methods to achieve effects across all players.
    /// Uses reflection to call private RPCs when needed.
    /// </summary>
    public static class NetworkCheats
    {
        #region Credits & Shop Exploits

        /// <summary>
        /// Sets group credits to any value by calling SyncGroupCreditsServerRpc.
        /// </summary>
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

        /// <summary>
        /// Unlocks a ship upgrade for free by calling BuyShipUnlockableServerRpc with current credits.
        /// </summary>
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

        /// <summary>
        /// Buys items without spending credits.
        /// </summary>
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

        /// <summary>
        /// Damages another player using DamagePlayerFromOtherClientServerRpc.
        /// </summary>
        public static void DamagePlayer(PlayerControllerB target, int damage = 10)
        {
            if (target == null || target.isPlayerDead) return;

            var localPlayer = LethalMenuMod.LocalPlayer;
            if (localPlayer == null) return;

            // Call the server RPC to damage the player
            target.DamagePlayerFromOtherClientServerRpc(damage, Vector3.zero, (int)localPlayer.playerClientId);
            Debug.Log($"[NetworkCheats] Damaged {target.playerUsername} for {damage} damage.");
        }

        /// <summary>
        /// Forces a player to drop all held items using DropAllHeldItemsServerRpc.
        /// </summary>
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

        /// <summary>
        /// Heals a player to full health. Works on ANY player using negative damage exploit.
        /// If host: Uses DamagePlayerServerRpc to set health directly.
        /// If client: Uses DamagePlayerFromOtherClientServerRpc with -100 damage.
        /// </summary>
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

        /// <summary>
        /// Heals the local player to full health.
        /// </summary>
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

        /// <summary>
        /// Sends a chat message as any player (spoofed chat).
        /// Uses the public AddTextToChatOnServer method (like lc-hax).
        /// Max 50 characters enforced by server.
        /// </summary>
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

        /// <summary>
        /// Sends a system text message (no player name attached).
        /// </summary>
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

        /// <summary>
        /// Spam system messages (uses coroutine for proper timing).
        /// Each message must be unique to avoid deduplication.
        /// </summary>
        public static void SpamSystemMessage(string message, int count = 10)
        {
            if (LethalMenuMod.Instance != null)
            {
                LethalMenuMod.Instance.StartCoroutine(SpamSystemMessageCoroutine(message, count));
            }
        }

        /// <summary>
        /// Coroutine that sends system message spam with unique suffixes.
        /// </summary>
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

        /// <summary>
        /// Spam chat messages (uses coroutine for proper timing).
        /// Each message must be unique to avoid deduplication.
        /// </summary>
        public static void SpamChat(string message, int count = 10)
        {
            if (LethalMenuMod.Instance != null)
            {
                LethalMenuMod.Instance.StartCoroutine(SpamChatCoroutine(message, count));
            }
        }
        
        /// <summary>
        /// Coroutine that sends spam messages with unique suffixes.
        /// </summary>
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

        /// <summary>
        /// Max spam - sends 50 messages very fast.
        /// </summary>
        public static void SpamChatMax(string message)
        {
            SpamChat(message, 50);
        }

        #endregion

        #region Ship & Level Control

        /// <summary>
        /// Forces ship to leave early using SetShipLeaveEarlyServerRpc.
        /// </summary>
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

        /// <summary>
        /// Toggles ship lights on/off for everyone.
        /// </summary>
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

        /// <summary>
        /// Changes destination to a different moon (requires host or appropriate permissions).
        /// </summary>
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

        /// <summary>
        /// Forces all players to eject/leave (usually used when in orbit).
        /// </summary>
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

        /// <summary>
        /// Toggles the ship's magnet on/off.
        /// </summary>
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

        /// <summary>
        /// Kills an enemy remotely using KillEnemyServerRpc.
        /// </summary>
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

        /// <summary>
        /// Kills all enemies on the map.
        /// </summary>
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

        /// <summary>
        /// Teleports a player to an entrance using the game's EntranceTeleport system.
        /// This calls the ServerRpc which teleports them properly for all clients.
        /// </summary>
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

        /// <summary>
        /// Teleports a player to the ship using direct position teleport.
        /// Only works reliably for local player.
        /// </summary>
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

        /// <summary>
        /// Teleports ANY player to the ship using the ship teleporter item.
        /// Requires the ship to have a normal teleporter (not inverse).
        /// Works by switching radar target and pressing the teleport button.
        /// </summary>
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

        /// <summary>
        /// Coroutine that switches radar target and teleports.
        /// </summary>
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

        /// <summary>
        /// Teleports local player to a specific position.
        /// </summary>
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

        /// <summary>
        /// Plays terminal audio for everyone (trolling).
        /// </summary>
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

        /// <summary>
        /// Gets all available ship unlockables and their IDs.
        /// </summary>
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

        /// <summary>
        /// Gets all available levels (moons) and their IDs.
        /// </summary>
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

        /// <summary>
        /// Gets all connected players.
        /// </summary>
        public static PlayerControllerB[] GetAllPlayers()
        {
            return LethalMenuMod.Players.Where(p => p != null && !p.isPlayerDead).ToArray();
        }

        /// <summary>
        /// Gets all alive enemies.
        /// </summary>
        public static EnemyAI[] GetAllEnemies()
        {
            return LethalMenuMod.Enemies.Where(e => e != null && !e.isEnemyDead).ToArray();
        }

        /// <summary>
        /// Check if we're the host (have more permissions).
        /// </summary>
        public static bool IsHost()
        {
            return NetworkManager.Singleton?.IsHost ?? false;
        }

        #endregion

        #region Malicious Exploits (Use Responsibly)

        /// <summary>
        /// Attempts to crash other players by sending invalid terminal audio index.
        /// NO bounds checking on syncedAudios array - IndexOutOfRangeException.
        /// </summary>
        /// <summary>
        /// Attempts to crash/lag other players by sending various invalid data.
        /// May cause IndexOutOfRangeException or other issues on clients.
        /// </summary>
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

        /// <summary>
        /// Aggressive RPC spam to cause network lag.
        /// Spams multiple RPC types simultaneously.
        /// </summary>
        public static void LagAllPlayers(int iterations = 50)
        {
            if (LethalMenuMod.Instance != null)
            {
                LethalMenuMod.Instance.StartCoroutine(LagPlayersCoroutine(iterations));
            }
        }
        
        /// <summary>
        /// Coroutine that spams various RPCs aggressively to cause lag.
        /// </summary>
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

        /// <summary>
        /// PROPER Bracken lag attack - transfers enemy ownership to target player
        /// causing massive pathfinding lag on their client.
        /// Requires a Bracken (Flowerman) to be spawned.
        /// </summary>
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

        /// <summary>
        /// Coroutine that performs the Bracken ownership transfer lag attack.
        /// </summary>
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

        /// <summary>
        /// Lag ALL players using Bracken ownership spam.
        /// </summary>
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

        /// <summary>
        /// Rapidly toggles ship lights to be annoying (uses coroutine).
        /// </summary>
        public static void FlickerShipLights()
        {
            if (LethalMenuMod.Instance != null)
            {
                LethalMenuMod.Instance.StartCoroutine(FlickerShipLightsCoroutine());
            }
        }
        
        /// <summary>
        /// Coroutine that flickers ship lights repeatedly.
        /// </summary>
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

        /// <summary>
        /// Sends massive damage to kill a player instantly.
        /// </summary>
        public static void InstantKillPlayer(PlayerControllerB target)
        {
            if (target == null || target.isPlayerDead) return;

            var localPlayer = LethalMenuMod.LocalPlayer;
            if (localPlayer == null) return;

            // 100 damage should kill most players
            target.DamagePlayerFromOtherClientServerRpc(100, Vector3.up * 10f, (int)localPlayer.playerClientId);
            Debug.Log($"[NetworkCheats] Instant killed {target.playerUsername}.");
        }

        /// <summary>
        /// Kill all other players at once.
        /// </summary>
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

        /// <summary>
        /// Impersonate another player in chat.
        /// Sends message that appears to be from another player.
        /// </summary>
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

        /// <summary>
        /// Sends a message via signal translator (shows on ship's monitor).
        /// Uses HUDManager.UseSignalTranslatorServerRpc - RequireOwnership = false.
        /// </summary>
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

        /// <summary>
        /// Spams signal translator with messages (very annoying, shows on everyone's ship monitor).
        /// </summary>
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

        /// <summary>
        /// Pulls the ship alarm cord (honks the horn).
        /// Uses ShipAlarmCord.PullCordServerRpc - RequireOwnership = false.
        /// </summary>
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

        /// <summary>
        /// Stops the ship horn.
        /// </summary>
        public static void StopShipHorn()
        {
            var alarmCord = UnityEngine.Object.FindObjectOfType<ShipAlarmCord>();
            if (alarmCord == null) return;

            var localPlayer = LethalMenuMod.LocalPlayer;
            int playerId = localPlayer != null ? (int)localPlayer.playerClientId : 0;

            alarmCord.StopPullingCordServerRpc(playerId);
            Debug.Log("[NetworkCheats] Stopped ship horn.");
        }

        /// <summary>
        /// Spams the ship horn on/off rapidly (extremely annoying).
        /// </summary>
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

        /// <summary>
        /// Forces the game to start (launches ship).
        /// Uses StartOfRound.StartGameServerRpc - RequireOwnership = false.
        /// </summary>
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

        /// <summary>
        /// Forces the game to end (returns to orbit).
        /// Uses StartOfRound.EndGameServerRpc - RequireOwnership = false.
        /// </summary>
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

        /// <summary>
        /// Opens or closes the ship doors.
        /// Uses StartOfRound.SetDoorsClosedServerRpc - RequireOwnership = false.
        /// Also plays the local animation via HangarShipDoor.
        /// </summary>
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

        /// <summary>
        /// Spams ship doors open/close (annoying visual effect).
        /// </summary>
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

        #endregion

        #region Free Vehicles (RequireOwnership = false)

        /// <summary>
        /// Buys a vehicle for free.
        /// Uses Terminal.BuyVehicleServerRpc - RequireOwnership = false.
        /// </summary>
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

        /// <summary>
        /// Gets list of available vehicles.
        /// </summary>
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

        /// <summary>
        /// Triggers maximum chaos: horn spam, light flicker, door spam, signal spam.
        /// </summary>
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

        /// <summary>
        /// Blow up all landmines on the map.
        /// </summary>
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

        /// <summary>
        /// Toggle all landmines enabled/disabled state.
        /// </summary>
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

        /// <summary>
        /// Toggle all turrets enabled/disabled state.
        /// </summary>
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

        /// <summary>
        /// Makes all turrets go berserk mode (fires at everything).
        /// </summary>
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

        /// <summary>
        /// Force the bridge to collapse.
        /// </summary>
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

        /// <summary>
        /// Force the small bridge (type 2) to collapse.
        /// </summary>
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

        /// <summary>
        /// Toggle car horn on all vehicles.
        /// </summary>
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

        /// <summary>
        /// Spam car horns on/off rapidly.
        /// </summary>
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

        /// <summary>
        /// Explode all vehicles (Cruisers).
        /// </summary>
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

        #endregion

        #region Jetpacks

        /// <summary>
        /// Explode all jetpacks.
        /// </summary>
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

        /// <summary>
        /// Fire all shotguns on the map.
        /// </summary>
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

        /// <summary>
        /// Spam fire all shotguns repeatedly.
        /// </summary>
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

        /// <summary>
        /// Toggle factory/facility lights.
        /// </summary>
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

        /// <summary>
        /// Force the company deposit desk tentacle attack.
        /// </summary>
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

        /// <summary>
        /// Spam the deposit desk door open/close.
        /// </summary>
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

        /// <summary>
        /// Spam terminal sounds (includes earrape invalid indices).
        /// </summary>
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

        /// <summary>
        /// Pure earrape spam - only invalid indices.
        /// </summary>
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

        /// <summary>
        /// Call this every frame from Update() to process continuous spam toggles.
        /// </summary>
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

        /// <summary>
        /// Stun all enemies on the map.
        /// </summary>
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
    }
}