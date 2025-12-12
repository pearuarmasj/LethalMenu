using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace LethalMenu.Menu
{
    public partial class HackMenu
    {
        // Item spawner state
        private int _selectedItemIndex = 0;
        private string _spawnValue = "100";

        #region Items Tab

        private void DrawItemsTab()
        {
            DrawSection("Item Cheats", () =>
            {
                Settings.InfiniteBattery = DrawToggle("Infinite Battery", Settings.InfiniteBattery, "Items never lose charge");
                Settings.OneHanded = DrawToggle("One-Handed", Settings.OneHanded, "Two-handed items become one-handed");
                Settings.StrongHands = DrawToggle("Strong Hands", Settings.StrongHands, "Two-handed items held one-handed");
                Settings.Reach = DrawToggle("Extended Reach", Settings.Reach, "Grab items from far away");
                Settings.LootThroughWalls = DrawToggle("Loot Through Walls", Settings.LootThroughWalls, "Grab items through walls");
                Settings.InteractThroughWalls = DrawToggle("Interact Through Walls", Settings.InteractThroughWalls, "Interact through walls");
                Settings.LootBeforeGameStarts = DrawToggle("Loot Before Start", Settings.LootBeforeGameStarts, "Grab items before game starts");
                Settings.GrabNutcrackerShotgun = DrawToggle("Grab Nutcracker Gun", Settings.GrabNutcrackerShotgun, "Steal shotgun from Nutcracker");
            });

            DrawSection("Weapon Cheats", () =>
            {
                Settings.SuperShovel = DrawToggle("Super Shovel", Settings.SuperShovel, "One-hit kill with shovel");
                Settings.SuperKnife = DrawToggle("Super Knife", Settings.SuperKnife, "Knife does massive damage");
                Settings.UnlimitedAmmo = DrawToggle("Unlimited Ammo", Settings.UnlimitedAmmo, "Shotgun never runs out");
                Settings.MinigunShotgun = DrawToggle("Minigun Shotgun", Settings.MinigunShotgun, "Hold LMB to rapid fire shotgun");
                Settings.UnlimitedZapGun = DrawToggle("Unlimited Zap Gun", Settings.UnlimitedZapGun, "Zap gun never overheats");
            });

            DrawSection("Special Items", () =>
            {
                Settings.UnlimitedTZP = DrawToggle("Unlimited TZP", Settings.UnlimitedTZP, "TZP never runs out");
                Settings.NoTZPEffects = DrawToggle("No TZP Effects", Settings.NoTZPEffects, "TZP doesn't affect vision");
                Settings.EggsAlwaysExplode = DrawToggle("Eggs Always Explode", Settings.EggsAlwaysExplode, "Easter eggs always explode");
                Settings.EggsNeverExplode = DrawToggle("Eggs Never Explode", Settings.EggsNeverExplode, "Easter eggs never explode");
            });

            DrawSection("Item Teleport", () =>
            {
                // Count ALL grabbable items (not just scrap) for teleport purposes
                var gameInstance = StartOfRound.Instance;
                var allItems = Object.FindObjectsOfType<GrabbableObject>();

                int totalItems = 0;
                int inShipCount = 0;
                int outsideCount = 0;

                if (gameInstance?.shipInnerRoomBounds != null)
                {
                    var shipBounds = gameInstance.shipInnerRoomBounds;
                    foreach (var item in allItems)
                    {
                        if (item == null) continue;
                        if (item.isHeld || item.isHeldByEnemy) continue;
                        if (item.scrapValue <= 0 && !item.itemProperties.isScrap) continue;

                        totalItems++;
                        if (shipBounds.bounds.Contains(item.transform.position))
                            inShipCount++;
                        else
                            outsideCount++;
                    }
                }

                GUILayout.Label($"Loot: {inShipCount} in ship, {outsideCount} outside ({totalItems} total)", _labelStyle);

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("TP All to Ship", _buttonStyle, GUILayout.Height(28)))
                {
                    TeleportItemsToShip();
                }
                if (GUILayout.Button("TP Nearby to Me", _buttonStyle, GUILayout.Height(28)))
                {
                    TeleportNearbyItemsToPlayer(15f);
                }
                GUILayout.EndHorizontal();
            });

            DrawSection("Item Spawner (Host Only)", () =>
            {
                var allItems = StartOfRound.Instance?.allItemsList?.itemsList;
                if (allItems == null || allItems.Count == 0)
                {
                    GUILayout.Label("No items available", _labelStyle);
                    return;
                }

                bool isHost = NetworkManager.Singleton?.IsHost ?? false;
                if (!isHost)
                {
                    GUILayout.Label("Only the host can spawn items", _labelStyle);
                    return;
                }

                // Item selector - show items with valid prefabs
                var spawnableItems = allItems.Where(i => i != null && i.spawnPrefab != null).ToList();
                if (spawnableItems.Count == 0)
                {
                    GUILayout.Label("No spawnable items found", _labelStyle);
                    return;
                }

                _selectedItemIndex = Mathf.Clamp(_selectedItemIndex, 0, spawnableItems.Count - 1);
                var selectedItem = spawnableItems[_selectedItemIndex];

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("<", _buttonStyle, GUILayout.Width(30)))
                {
                    _selectedItemIndex = (_selectedItemIndex - 1 + spawnableItems.Count) % spawnableItems.Count;
                }
                GUILayout.Label(selectedItem.itemName, _labelStyle, GUILayout.Width(150));
                if (GUILayout.Button(">", _buttonStyle, GUILayout.Width(30)))
                {
                    _selectedItemIndex = (_selectedItemIndex + 1) % spawnableItems.Count;
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Value:", _labelStyle, GUILayout.Width(50));
                _spawnValue = GUILayout.TextField(_spawnValue, GUILayout.Width(60));
                if (GUILayout.Button("Spawn", _buttonStyle, GUILayout.Height(25)))
                {
                    SpawnItem(selectedItem);
                }
                GUILayout.EndHorizontal();
            });
        }

        private void TeleportItemsToShip()
        {
            if (LethalMenuMod.GameInstance == null || LethalMenuMod.LocalPlayer == null) return;

            // Get player position as reference (they should be in ship when using this)
            Vector3 playerPos = LethalMenuMod.LocalPlayer.transform.position;
            float spawnHeight = playerPos.y + 1.5f; // Spawn above ground to let them fall

            // Get ship bounds for containment check
            var shipBounds = LethalMenuMod.GameInstance.shipInnerRoomBounds;
            if (shipBounds == null)
            {
                Loader.LogError("Ship bounds not found");
                return;
            }

            // Get the ship's elevator transform for proper parenting
            var elevatorTransform = LethalMenuMod.GameInstance.elevatorTransform;
            var localPlayer = LethalMenuMod.LocalPlayer;

            // Re-collect items to ensure fresh list
            var allItems = UnityEngine.Object.FindObjectsOfType<GrabbableObject>();
            int teleported = 0;
            int totalValue = 0;

            foreach (var item in allItems)
            {
                if (item == null) continue;
                if (item.isHeld || item.isHeldByEnemy) continue;
                if (item.isPocketed) continue;

                // Check if item is ACTUALLY in ship bounds
                bool actuallyInShip = shipBounds.bounds.Contains(item.transform.position);
                if (actuallyInShip) continue;

                // Set position above player to allow proper falling
                var targetPos = new Vector3(playerPos.x, spawnHeight, playerPos.z);

                // Parent to elevator so it moves with ship
                if (elevatorTransform != null)
                {
                    item.transform.SetParent(elevatorTransform, true);
                }

                // Set position and trigger proper ground detection
                item.transform.position = targetPos;
                item.startFallingPosition = item.transform.localPosition;
                item.hasHitGround = false;
                item.reachedFloorTarget = false;
                item.fallTime = 0f;

                // IMPORTANT: Use SetItemInElevator to properly track stats
                // This updates scrapCollectedInLevel and player's profitable stat
                if (!item.isInShipRoom)
                {
                    // Call SetItemInElevator which handles all the stat tracking
                    localPlayer.SetItemInElevator(true, true, item);
                    totalValue += item.scrapValue;
                }
                else
                {
                    // Already marked as in ship, just set flags
                    item.isInShipRoom = true;
                    item.isInElevator = true;
                }

                // Call FallToGround to properly land the item
                item.FallToGround(false);

                teleported++;
            }

            Loader.Log($"Teleported {teleported} items to ship (value: ${totalValue})");
        }

        private void TeleportNearbyItemsToPlayer(float radius)
        {
            if (LethalMenuMod.LocalPlayer == null || LethalMenuMod.GameInstance == null) return;

            var playerPos = LethalMenuMod.LocalPlayer.transform.position;
            float spawnHeight = playerPos.y + 1.5f;

            // Get ship bounds to check if player is in ship
            var shipBounds = LethalMenuMod.GameInstance.shipInnerRoomBounds;
            bool playerInShip = shipBounds != null && shipBounds.bounds.Contains(playerPos);

            // Get elevator transform for parenting if in ship
            var elevatorTransform = LethalMenuMod.GameInstance.elevatorTransform;

            // Re-collect items for fresh list
            var allItems = UnityEngine.Object.FindObjectsOfType<GrabbableObject>();
            int teleported = 0;

            foreach (var item in allItems)
            {
                if (item == null) continue;
                if (item.isHeld || item.isHeldByEnemy) continue;
                if (item.isPocketed) continue;

                float dist = Vector3.Distance(playerPos, item.transform.position);
                if (dist <= radius)
                {
                    // Parent to elevator if player is in ship
                    if (playerInShip && elevatorTransform != null)
                    {
                        item.transform.SetParent(elevatorTransform, true);
                        item.isInShipRoom = true;
                        item.isInElevator = true;
                    }

                    // Set position and trigger fall
                    var targetPos = new Vector3(playerPos.x, spawnHeight, playerPos.z);
                    item.transform.position = targetPos;
                    item.startFallingPosition = item.transform.localPosition;
                    item.hasHitGround = false;
                    item.reachedFloorTarget = false;
                    item.fallTime = 0f;
                    item.FallToGround(false);

                    teleported++;
                }
            }

            Loader.Log($"Teleported {teleported} nearby items to player");
        }

        private void SpawnItem(Item item)
        {
            if (LethalMenuMod.LocalPlayer == null || item?.spawnPrefab == null) return;
            if (StartOfRound.Instance?.propsContainer == null) return;

            int value = 100;
            int.TryParse(_spawnValue, out value);

            try
            {
                var spawnPos = LethalMenuMod.LocalPlayer.gameplayCamera.transform.position +
                               LethalMenuMod.LocalPlayer.gameplayCamera.transform.forward * 2f;

                var obj = Object.Instantiate(item.spawnPrefab, spawnPos, Quaternion.identity, StartOfRound.Instance.propsContainer);
                var grabbable = obj.GetComponent<GrabbableObject>();
                if (grabbable != null)
                {
                    grabbable.SetScrapValue(value);
                    grabbable.fallTime = 0f;
                }
                obj.GetComponent<NetworkObject>()?.Spawn();
                Loader.Log($"[LethalMenu] Spawned {item.itemName} with value {value}");
            }
            catch (System.Exception ex)
            {
                Loader.LogError($"[LethalMenu] Failed to spawn item: {ex.Message}");
            }
        }

        #endregion
    }
}
