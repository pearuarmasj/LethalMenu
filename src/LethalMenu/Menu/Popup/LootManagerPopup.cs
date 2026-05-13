using UnityEngine;
using System.Linq;
using LethalMenu.Mixins;

namespace LethalMenu.Menu.Popup
{
    public class LootManagerPopup : PopupMenu, IItemManipulator, ITeleporter
    {
        public LootManagerPopup() : base("Loot Manager", 20006, 400, 400) { }

        protected override void DrawBody()
        {
            var items = LethalMenuMod.Items;
            int scrapCount = items.Count(i => i != null && i.itemProperties?.isScrap == true);
            int totalValue = items.Where(i => i != null && i.itemProperties?.isScrap == true).Sum(i => i.scrapValue);
            int shipCount = items.Count(i => i != null && i.isInShipRoom && i.itemProperties?.isScrap == true);
            int shipValue = items.Where(i => i != null && i.isInShipRoom && i.itemProperties?.isScrap == true).Sum(i => i.scrapValue);
            var (_, totalShipItems, rawShipValue, adjustedShipValue) = this.CalculateShipInventory();
            var buyRate = StartOfRound.Instance?.companyBuyingRate ?? 0f;

            GUILayout.Label($"Total Scrap: {scrapCount} items (${totalValue})");
            GUILayout.Label($"In Ship: {shipCount} items (${shipValue})");
            GUILayout.Label($"Sell Value: ${adjustedShipValue} ({buyRate:P0})");
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("TP All to Ship", GUILayout.Height(28)))
                this.TeleportAllItemsToShip();
            if (GUILayout.Button("TP Nearby to Me", GUILayout.Height(28)))
                this.TeleportNearbyItemsToPlayer(15f);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Sell Quota", GUILayout.Height(28)))
                Hack.SellQuota.Execute();
            if (GUILayout.Button($"Sell All Ship Items ({totalShipItems}, ${rawShipValue})", GUILayout.Height(28)))
                this.SellAllItemsNaturally();
            GUILayout.EndHorizontal();

            GUILayout.Space(8);

            GUILayout.Label("--- Ship Inventory ---");
            foreach (var item in items)
            {
                if (item == null || !item.isInShipRoom) continue;
                if (item.itemProperties?.isScrap != true) continue;

                GUILayout.Label($"  {item.itemProperties.itemName}: ${item.scrapValue}");
            }

            GUILayout.Space(5);
            GUILayout.Label("--- Outside Loot ---");
            foreach (var item in items)
            {
                if (item == null || item.isInShipRoom || item.isHeld) continue;
                if (item.itemProperties?.isScrap != true || item.scrapValue <= 0) continue;

                float dist = LethalMenuMod.LocalPlayer != null
                    ? Vector3.Distance(item.transform.position, LethalMenuMod.LocalPlayer.transform.position)
                    : 0f;
                GUILayout.Label($"  {item.itemProperties.itemName}: ${item.scrapValue} [{dist:F0}m]");
            }
        }
    }
}
