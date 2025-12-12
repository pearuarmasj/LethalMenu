using System;
using GameNetcodeStuff;
using LethalMenu.Util;
using UnityEngine;

namespace LethalMenu.Cheats
{
    /// Network exploits using ServerRpc/ClientRpc methods.
    /// These call game network methods to achieve effects across all players.
    /// Uses reflection to call private RPCs when needed.
    /// 
    /// Split into partial classes:
    /// - NetworkCheats.cs (this file) - Core utilities, doors, noise, quota
    /// - NetworkCheats.EnemySpawn.cs - Enemy spawning (mimic, etc.)
    /// - NetworkCheats.EnemyControl.cs - Enemy kill/stun
    /// - NetworkCheats.EnemyTargeting.cs - Mob player, lag player
    /// - NetworkCheats.Teleportation.cs - Teleport to entrance/ship
    /// - NetworkCheats.TrollEffects.cs - Spin effects
    /// - NetworkCheats.PlayerEffects.cs - Player status effects
    /// - NetworkCheats.Malicious.cs - Bomb player, send to void
    /// - NetworkCheats.Vehicles.cs - Vehicle cheats
    /// - NetworkCheats.WorldHazards.cs - Landmines, turrets
    /// - NetworkCheats.Chat.cs - Chat/spam
    /// - NetworkCheats.CreditsShop.cs - Credits manipulation
    /// - NetworkCheats.ShipLevel.cs - Ship/level control
    /// - NetworkCheats.GameControl.cs - Game state control
    /// - NetworkCheats.HostOnly.cs - Host-only cheats
    /// - NetworkCheats.Utility.cs - Utility functions
    /// - NetworkCheats.FakeDeath.cs - Fake death
    /// - NetworkCheats.SelfRevive.cs - Self revive
    /// - NetworkCheats.Cosmetic.cs - Cosmetic changes
    /// - NetworkCheats.TerminalAudio.cs - Terminal audio
    /// - NetworkCheats.FactoryControl.cs - Factory control
    /// - NetworkCheats.Spam.cs - Spam features
    /// - NetworkCheats.Experimentation.cs - Experimental reflection calls
    public static partial class NetworkCheats
    {
        #region Big Doors

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

        #endregion

        #region Teleportation (Legacy - also see NetworkCheats.Teleportation.cs)

        /// Teleports a player to another player's position.
        public static void TeleportPlayerToPlayer(PlayerControllerB source, PlayerControllerB target)
        {
            if (source == null || target == null) return;
            TeleportPlayerToPosition(source, target.transform.position);
        }

        /// Teleports ANY player to a specific position.
        public static void TeleportPlayerToPosition(PlayerControllerB target, Vector3 position)
        {
            if (target == null || target.isPlayerDead)
            {
                Debug.Log("[NetworkCheats] TeleportPlayerToPosition: Target is null or dead.");
                return;
            }

            var localPlayer = LethalMenuMod.LocalPlayer;
            
            if (target == localPlayer)
            {
                target.TeleportPlayer(position);
                Debug.Log($"[NetworkCheats] Teleported self to {position}");
                return;
            }

            if (localPlayer == null || !localPlayer.IsHost)
            {
                HUDManager.Instance?.DisplayTip("Teleport", "Host only for remote players.");
                return;
            }

            target.transform.position = position;
            target.serverPlayerPosition = position;
            
            if (target.thisController != null && target.thisController.enabled)
            {
                target.thisController.enabled = false;
                target.transform.position = position;
                target.thisController.enabled = true;
            }

            try
            {
                ReflectionHelper.InvokePrivate(target, "UpdatePlayerPositionServerRpc", 
                    position, target.isInElevator, target.isInHangarShipRoom, target.isExhausted, true);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[NetworkCheats] UpdatePlayerPositionServerRpc reflection failed: {ex.Message}");
            }

            target.teleportedLastFrame = true;
            
            Debug.Log($"[NetworkCheats] Teleported {target.playerUsername} to {position}");
            HUDManager.Instance?.DisplayTip("Teleport", $"Teleported {target.playerUsername}.");
        }

        /// Teleports all players to the local player's position.
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

        /// Teleports a player to a random position on the map.
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

            Vector3 voidPos = startOfRound.notSpawnedPosition.position;
            targetPlayer.TeleportPlayer(voidPos);
            
            Debug.Log($"[NetworkCheats] Sent {targetPlayer.playerUsername} to the void at {voidPos}");
            HUDManager.Instance?.DisplayTip("Void", $"Sent {targetPlayer.playerUsername} to the void!");
        }

        #endregion

        #region Noise

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

        #endregion

        #region Game State

        /// Set game timescale.
        public static void SetTimescale(float scale)
        {
            scale = Mathf.Clamp(scale, 0.1f, 10f);
            Time.timeScale = scale;
            Debug.Log($"[NetworkCheats] Timescale set to {scale}");
            HUDManager.Instance?.DisplayTip("Timescale", $"Game speed: {scale:F1}x");
        }

        #endregion

        #region Quota

        /// Set the profit quota. Host only.
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

        /// Set fulfilled amount only. Host only.
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

        /// Auto-sell items to meet quota exactly.
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

            var depositDesk = UnityEngine.Object.FindObjectOfType<DepositItemsDesk>();
            if (depositDesk == null)
            {
                HUDManager.Instance?.DisplayTip("Sell Quota", "Not at Company (no sell desk).");
                return;
            }

            float buyRate = StartOfRound.Instance?.companyBuyingRate ?? 1f;

            var allItems = UnityEngine.Object.FindObjectsOfType<GrabbableObject>();
            var sellableItems = new System.Collections.Generic.List<GrabbableObject>();
            
            foreach (var item in allItems)
            {
                if (item == null || item.isHeld || item.isHeldByEnemy) continue;
                if (!item.itemProperties.isScrap) continue;
                if (item.scrapValue <= 0) continue;
                
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

            foreach (var item in itemsToSell)
            {
                var deskPos = depositDesk.deskObjectsContainer.transform.position;
                item.transform.position = deskPos;
                item.targetFloorPosition = deskPos;
                
                depositDesk.AddObjectToDeskServerRpc(item.NetworkObject);
            }

            depositDesk.SellItemsOnServer();
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

        #endregion

        #region Bomb Player

        /// Explode a jetpack on a player.
        public static void BombPlayer(PlayerControllerB targetPlayer)
        {
            if (targetPlayer == null)
            {
                HUDManager.Instance?.DisplayTip("Bomb", "No target player.");
                return;
            }

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

            if (jetpack == null)
            {
                var startOfRound = StartOfRound.Instance;
                if (startOfRound?.allItemsList?.itemsList == null)
                {
                    HUDManager.Instance?.DisplayTip("Bomb", "Cannot spawn jetpack (item list null).");
                    return;
                }

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

                Vector3 spawnPos = targetPlayer.transform.position + Vector3.up * 2f;
                GameObject obj = UnityEngine.Object.Instantiate(jetpackItem.spawnPrefab, spawnPos, Quaternion.identity);
                if (obj.TryGetComponent(out Unity.Netcode.NetworkObject netObj))
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

            jetpack.transform.position = targetPlayer.transform.position;
            jetpack.ExplodeJetpackServerRpc();
            
            Debug.Log($"[NetworkCheats] Bombed {targetPlayer.playerUsername}");
            HUDManager.Instance?.DisplayTip("Bomb", $"Jetpack exploded on {targetPlayer.playerUsername}!");
        }

        #endregion
    }
}
