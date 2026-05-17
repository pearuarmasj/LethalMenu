using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace LethalMenu.Mixins
{
    public interface IItemManipulator { }

    public static class ItemManipulatorMixin
    {
        public static void TeleportAllItemsToShip(this IItemManipulator _)
        {
            var startOfRound = StartOfRound.Instance ?? LethalMenuMod.GameInstance;
            var localPlayer = LethalMenuMod.LocalPlayer;
            if (startOfRound == null || localPlayer == null) return;

            var elevatorTransform = startOfRound.elevatorTransform;
            var shipPos = GetShipDropPosition(startOfRound);
            var allItems = Object.FindObjectsOfType<GrabbableObject>(includeInactive: true);
            var teleported = 0;
            var totalValue = 0;

            foreach (var item in allItems)
            {
                if (item == null || item.isHeld || item.isHeldByEnemy || item.isPocketed || item.isInShipRoom)
                    continue;

                if (item.itemProperties == null || (!item.itemProperties.isScrap && item.scrapValue <= 0))
                    continue;

                var targetPos = shipPos + GetStackOffset(teleported);
                if (elevatorTransform != null)
                    item.transform.SetParent(elevatorTransform, true);

                item.transform.position = targetPos;
                ResetFallState(item);

                localPlayer.SetItemInElevator(true, true, item);
                if (item.itemProperties.isScrap && !item.scrapPersistedThroughRounds)
                    RoundManager.Instance?.CollectNewScrapForThisRound(item);

                item.FallToGround(false);

                teleported++;
                totalValue += item.scrapValue;
            }

            Loader.Log($"Teleported {teleported} loot items to ship (value: ${totalValue})");
        }

        public static void TeleportNearbyItemsToPlayer(this IItemManipulator _, float radius)
        {
            var startOfRound = StartOfRound.Instance ?? LethalMenuMod.GameInstance;
            var localPlayer = LethalMenuMod.LocalPlayer;
            if (startOfRound == null || localPlayer == null) return;

            var playerPos = localPlayer.transform.position;
            var targetPos = playerPos + Vector3.up * 1.5f;
            var shipBounds = startOfRound.shipInnerRoomBounds;
            var playerInShip = shipBounds != null && shipBounds.bounds.Contains(playerPos);
            var elevatorTransform = startOfRound.elevatorTransform;
            var teleported = 0;
            var totalValue = 0;

            foreach (var item in Object.FindObjectsOfType<GrabbableObject>(includeInactive: true))
            {
                if (item == null || item.isHeld || item.isHeldByEnemy || item.isPocketed)
                    continue;

                if (Vector3.Distance(playerPos, item.transform.position) > radius)
                    continue;

                if (playerInShip && elevatorTransform != null)
                    item.transform.SetParent(elevatorTransform, true);

                item.transform.position = targetPos + GetStackOffset(teleported);
                ResetFallState(item);

                if (playerInShip && !item.isInShipRoom)
                {
                    localPlayer.SetItemInElevator(true, true, item);
                    if (item.itemProperties != null && item.itemProperties.isScrap && !item.scrapPersistedThroughRounds)
                        RoundManager.Instance?.CollectNewScrapForThisRound(item);
                    totalValue += item.scrapValue;
                }
                else if (playerInShip)
                {
                    item.isInShipRoom = true;
                    item.isInElevator = true;
                }

                item.FallToGround(false);
                teleported++;
            }

            Loader.Log($"Teleported {teleported} nearby items to player (value: ${totalValue})");
        }

        public static (int scrapCount, int totalItems, int rawValue, int adjustedValue) CalculateShipInventory(this IItemManipulator _)
        {
            var startOfRound = StartOfRound.Instance ?? LethalMenuMod.GameInstance;
            if (startOfRound == null || startOfRound.shipBounds == null)
                return (0, 0, 0, 0);

            var shipBounds = startOfRound.shipBounds;
            var allItems = Object.FindObjectsOfType<GrabbableObject>(includeInactive: true);
            var scrapCount = 0;
            var totalItems = 0;
            var rawValue = 0;

            foreach (var item in allItems)
            {
                if (item == null || item.itemProperties == null) continue;
                if (item.isHeld || item.isHeldByEnemy) continue;
                if (!shipBounds.bounds.Contains(item.transform.position)) continue;
                if (item.scrapValue <= 0) continue;

                totalItems++;
                scrapCount++;
                rawValue += item.scrapValue;
            }

            var adjustedValue = (int)(rawValue * startOfRound.companyBuyingRate);
            return (scrapCount, totalItems, rawValue, adjustedValue);
        }

        public static void SellAllItemsNaturally(this IItemManipulator _)
        {
            var startOfRound = StartOfRound.Instance ?? LethalMenuMod.GameInstance;
            if (startOfRound?.currentLevel == null)
            {
                Loader.Log("[LethalMenu] Not in game.");
                return;
            }

            if (!startOfRound.currentLevel.PlanetName.Contains("Gordion"))
            {
                Loader.Log("[LethalMenu] Not on Company planet. Go to 71-Gordion first.");
                return;
            }

            var shipBounds = startOfRound.shipBounds;
            if (shipBounds == null)
            {
                Loader.Log("[LethalMenu] Ship bounds not found.");
                return;
            }

            var itemsToSell = new List<GrabbableObject>();
            var rawValue = 0;

            foreach (var item in Object.FindObjectsOfType<GrabbableObject>(includeInactive: true))
            {
                if (item == null || item.itemProperties == null) continue;
                if (item.isHeld || item.isHeldByEnemy) continue;
                if (!shipBounds.bounds.Contains(item.transform.position)) continue;
                if (item.scrapValue <= 0) continue;

                itemsToSell.Add(item);
                rawValue += item.scrapValue;
            }

            if (itemsToSell.Count == 0)
            {
                Loader.Log("[LethalMenu] No sellable items in ship.");
                return;
            }

            var adjustedValue = (int)(rawValue * startOfRound.companyBuyingRate);
            var itemNames = new Dictionary<string, (int count, int value)>();
            foreach (var item in itemsToSell)
            {
                var name = item.itemProperties?.itemName ?? "Unknown";
                if (!itemNames.ContainsKey(name))
                    itemNames[name] = (0, 0);
                var (count, value) = itemNames[name];
                itemNames[name] = (count + 1, value + item.scrapValue);
            }

            Loader.Log("=== SELL SUMMARY ===");
            foreach (var kvp in itemNames)
                Loader.Log($"  {kvp.Key} x{kvp.Value.count} = ${kvp.Value.value}");
            Loader.Log($"  RAW TOTAL: ${rawValue}");
            Loader.Log($"  ADJUSTED ({startOfRound.companyBuyingRate:P0}): ${adjustedValue}");

            var terminal = Object.FindObjectOfType<Terminal>();
            if (terminal == null)
            {
                Loader.Log("[LethalMenu] Terminal not found.");
                return;
            }

            var oldCredits = terminal.groupCredits;
            terminal.groupCredits += adjustedValue;

            var timeOfDay = TimeOfDay.Instance;
            if (timeOfDay != null)
            {
                timeOfDay.quotaFulfilled += adjustedValue;
                timeOfDay.UpdateProfitQuotaCurrentTime();
            }

            startOfRound.gameStats.scrapValueCollected += adjustedValue;
            Loader.Log($"  Credits: ${oldCredits} -> ${terminal.groupCredits}");
            Loader.Log("====================");

            var itemListText = "";
            foreach (var kvp in itemNames)
                itemListText += $"{kvp.Key} (x{kvp.Value.count}) : {kvp.Value.value} \n";

            var hud = HUDManager.Instance;
            if (hud != null)
            {
                hud.moneyRewardsListText.text = itemListText;
                hud.moneyRewardsTotalText.text = $"TOTAL: ${adjustedValue}";
                hud.moneyRewardsAnimator.SetTrigger("showRewards");
                hud.rewardsScrollbar.value = 1f;
            }

            foreach (var item in itemsToSell)
            {
                if (item == null || item.NetworkObject == null || !item.NetworkObject.IsSpawned) continue;

                if (NetworkManager.Singleton?.IsHost == true || NetworkManager.Singleton?.IsServer == true)
                    item.NetworkObject.Despawn(true);
                else
                    item.gameObject.SetActive(false);
            }

            Loader.Log($"[LethalMenu] SUCCESS: Sold {itemsToSell.Count} items for ${adjustedValue}");
        }

        private static void ResetFallState(GrabbableObject item)
        {
            item.hasHitGround = false;
            item.reachedFloorTarget = false;
            item.fallTime = 0f;
            item.startFallingPosition = item.transform.localPosition;
            item.targetFloorPosition = item.transform.localPosition;
        }

        private static Vector3 GetShipDropPosition(StartOfRound startOfRound)
        {
            if (startOfRound.middleOfShipNode != null)
                return startOfRound.middleOfShipNode.position + Vector3.up * 1.5f;

            if (startOfRound.insideShipPositions is { Length: > 0 } && startOfRound.insideShipPositions[0] != null)
                return startOfRound.insideShipPositions[0].position + Vector3.up * 1.5f;

            var terminal = Object.FindObjectOfType<Terminal>();
            if (terminal != null)
                return terminal.transform.position + Vector3.up * 0.5f;

            return startOfRound.elevatorTransform != null
                ? startOfRound.elevatorTransform.position + Vector3.up * 1.5f
                : Vector3.up * 1.5f;
        }

        private static Vector3 GetStackOffset(int index)
        {
            var x = (index % 5 - 2) * 0.35f;
            var z = (index / 5 % 5 - 2) * 0.35f;
            var y = index / 25 * 0.2f;
            return new Vector3(x, y, z);
        }
    }
}
