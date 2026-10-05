using UnityEngine;

namespace LethalMenu.Menu
{
    public partial class HackMenu
    {
        #region Items Tab

        private void DrawItemsTab()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Item Manager", _buttonStyle, GUILayout.Height(28)))
                _itemManager.IsOpen = !_itemManager.IsOpen;
            if (GUILayout.Button("Loot Manager", _buttonStyle, GUILayout.Height(28)))
                _lootManager.IsOpen = !_lootManager.IsOpen;
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            DrawSection("Item Cheats", () =>
            {
                DrawHackToggle(Hack.InfiniteBattery, "Infinite Battery", "Items never lose charge");
                DrawHackToggle(Hack.OneHanded, "One-Handed", "Two-handed items become one-handed");
                DrawHackToggle(Hack.Reach, "Extended Reach", "Grab range 30 m");
                DrawHackToggle(Hack.LootThroughWalls, "Loot Through Walls", "10 km range, loot only");
                DrawHackToggle(Hack.InteractThroughWalls, "Interact Through Walls", "10 km range, interactables only");
                DrawHackToggle(Hack.InfiniteGrab, "Infinite Grab", "Grab range 10 km");
                GUILayout.Label("  Both through-walls toggles on: the grab ray hits loot and interactables.", _tooltipStyle);
                DrawHackToggle(Hack.LootBeforeGameStarts, "Loot Before Start", "Grab items before game starts");
                DrawHackToggle(Hack.GrabNutcrackerShotgun, "Grab Nutcracker Gun", "Steal shotgun from Nutcracker");
                DrawHackToggle(Hack.InfiniteScanRange, "Infinite Scan Range", "Q-scanner sees everything on the map");
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
        }
        #endregion
    }
}
