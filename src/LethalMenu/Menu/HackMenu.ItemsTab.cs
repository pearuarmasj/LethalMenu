using UnityEngine;

namespace LethalMenu.Menu
{
    public partial class HackMenu
    {
        #region Items Tab

        private void DrawItemsTab()
        {
            if (GUILayout.Button("Open Item Manager", _buttonStyle, GUILayout.Height(28)))
                _itemManager.IsOpen = !_itemManager.IsOpen;
            GUILayout.Space(5);

            DrawSection("Item Cheats", () =>
            {
                DrawHackToggle(Hack.InfiniteBattery, "Infinite Battery", "Items never lose charge");
                DrawHackToggle(Hack.OneHanded, "One-Handed", "Two-handed items become one-handed");
                DrawHackToggle(Hack.StrongHands, "Strong Hands", "Two-handed items held one-handed");
                DrawHackToggle(Hack.Reach, "Extended Reach", "Grab items from far away");
                DrawHackToggle(Hack.LootThroughWalls, "Loot Through Walls", "Grab items through walls");
                DrawHackToggle(Hack.InteractThroughWalls, "Interact Through Walls", "Interact through walls");
                DrawHackToggle(Hack.LootBeforeGameStarts, "Loot Before Start", "Grab items before game starts");
                DrawHackToggle(Hack.GrabNutcrackerShotgun, "Grab Nutcracker Gun", "Steal shotgun from Nutcracker");
            });

            DrawSection("Weapon Cheats", () =>
            {
                DrawHackToggle(Hack.SuperShovel, "Super Shovel", "One-hit kill with shovel");
                DrawHackToggle(Hack.SuperKnife, "Super Knife", "Knife does massive damage");
                DrawHackToggle(Hack.UnlimitedAmmo, "Unlimited Ammo", "Shotgun never runs out");
                DrawHackToggle(Hack.MinigunShotgun, "Minigun Shotgun", "Hold LMB to rapid fire shotgun");
                DrawHackToggle(Hack.UnlimitedZapGun, "Unlimited Zap Gun", "Zap gun never overheats");
            });

            DrawSection("Special Items", () =>
            {
                DrawHackToggle(Hack.UnlimitedTZP, "Unlimited TZP", "TZP never runs out");
                DrawHackToggle(Hack.NoTZPEffects, "No TZP Effects", "TZP doesn't affect vision");
                DrawHackToggle(Hack.EggsAlwaysExplode, "Eggs Always Explode", "Easter eggs always explode");
                DrawHackToggle(Hack.EggsNeverExplode, "Eggs Never Explode", "Easter eggs never explode");
            });

            DrawSection("Item Teleport", () =>
            {
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
        }

        private void TeleportItemsToShip()
        {
            if (LethalMenuMod.GameInstance == null || LethalMenuMod.LocalPlayer == null) return;

            var shipBounds = LethalMenuMod.GameInstance.shipInnerRoomBounds;
            if (shipBounds == null) return;

            var elevatorTransform = LethalMenuMod.GameInstance.elevatorTransform;
            var localPlayer = LethalMenuMod.LocalPlayer;
            var shipPos = GetItemTeleportShipPosition();

            var allItems = UnityEngine.Object.FindObjectsOfType<GrabbableObject>();
            int teleported = 0;
            int totalValue = 0;

            foreach (var item in allItems)
            {
                if (item == null) continue;
                if (item.isHeld || item.isHeldByEnemy) continue;
                if (item.isPocketed) continue;

                bool actuallyInShip = shipBounds.bounds.Contains(item.transform.position);
                if (actuallyInShip) continue;

                var targetPos = shipPos + GetItemTeleportStackOffset(teleported);

                if (elevatorTransform != null)
                {
                    item.transform.SetParent(elevatorTransform, true);
                }

                item.transform.position = targetPos;
                item.startFallingPosition = item.transform.localPosition;
                item.hasHitGround = false;
                item.reachedFloorTarget = false;
                item.fallTime = 0f;

                if (!item.isInShipRoom)
                {
                    localPlayer.SetItemInElevator(true, true, item);
                    if (item.itemProperties != null && item.itemProperties.isScrap && !item.scrapPersistedThroughRounds)
                        RoundManager.Instance?.CollectNewScrapForThisRound(item);
                    totalValue += item.scrapValue;
                }
                else
                {
                    item.isInShipRoom = true;
                    item.isInElevator = true;
                }

                item.FallToGround(false);
                teleported++;
            }

            Loader.Log($"Teleported {teleported} items to ship (value: ${totalValue})");
        }

        private Vector3 GetItemTeleportShipPosition()
        {
            var gameInstance = LethalMenuMod.GameInstance;
            if (gameInstance?.middleOfShipNode != null)
                return gameInstance.middleOfShipNode.position + Vector3.up * 1.5f;

            if (gameInstance?.insideShipPositions is { Length: > 0 } && gameInstance.insideShipPositions[0] != null)
                return gameInstance.insideShipPositions[0].position + Vector3.up * 1.5f;

            if (gameInstance?.elevatorTransform != null)
                return gameInstance.elevatorTransform.position + Vector3.up * 1.5f;

            return LethalMenuMod.LocalPlayer != null
                ? LethalMenuMod.LocalPlayer.transform.position + Vector3.up * 1.5f
                : Vector3.up * 1.5f;
        }

        private static Vector3 GetItemTeleportStackOffset(int index)
        {
            var x = (index % 5 - 2) * 0.35f;
            var z = (index / 5 % 5 - 2) * 0.35f;
            var y = index / 25 * 0.2f;
            return new Vector3(x, y, z);
        }

        private void TeleportNearbyItemsToPlayer(float radius)
        {
            if (LethalMenuMod.LocalPlayer == null || LethalMenuMod.GameInstance == null) return;

            var playerPos = LethalMenuMod.LocalPlayer.transform.position;
            float spawnHeight = playerPos.y + 1.5f;

            var shipBounds = LethalMenuMod.GameInstance.shipInnerRoomBounds;
            bool playerInShip = shipBounds != null && shipBounds.bounds.Contains(playerPos);

            var elevatorTransform = LethalMenuMod.GameInstance.elevatorTransform;

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
                    if (playerInShip && elevatorTransform != null)
                    {
                        item.transform.SetParent(elevatorTransform, true);
                        item.isInShipRoom = true;
                        item.isInElevator = true;
                    }

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
        #endregion
    }
}
