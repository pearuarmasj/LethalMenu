using LethalMenu.Mixins;
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
                DrawHackToggle(Hack.InfiniteScanRange, "Infinite Scan Range", "Q-scanner sees everything on the map");
                DrawHackToggle(Hack.InfiniteGrab, "Infinite Grab", "Grab items from any distance");
                DrawHackToggle(Hack.InfiniteItemUsage, "Infinite Item Usage", "Battery and charges never decrement");
                DrawHackToggle(Hack.InfiniteDeposit, "Infinite Deposit", "Deposit desk has no item cap");
                DrawHackToggle(Hack.LootAnyItemBeltBag, "Loot Any Item (Belt Bag)", "Belt bag accepts any grabbable");
                DrawHackToggle(Hack.LootThroughWallsBeltBag, "Loot Through Walls (Belt Bag)", "Belt bag picks up scrap through walls");
                DrawHackToggle(Hack.UnlimitedPresents, "Unlimited Presents", "Gift boxes can be opened repeatedly");
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
                var allItems = Object.FindObjectsOfType<GrabbableObject>(includeInactive: true);

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
                    this.TeleportAllItemsToShip();
                }
                if (GUILayout.Button("TP Nearby to Me", _buttonStyle, GUILayout.Height(28)))
                {
                    this.TeleportNearbyItemsToPlayer(15f);
                }
                GUILayout.EndHorizontal();
            });
        }
        #endregion
    }
}
