using UnityEngine;
using Unity.Netcode;
using System.Linq;
using LethalMenu.Mixins;

namespace LethalMenu.Menu.Popup
{
    public class ItemManagerPopup : PopupMenu, IItemManipulator
    {
        private string _searchFilter = "";
        private int _scrapValue = 100;

        public ItemManagerPopup() : base("Item Manager", 20001, 420, 400) { }

        protected override void DrawBody()
        {
            var allItems = StartOfRound.Instance?.allItemsList?.itemsList;
            if (allItems == null || allItems.Count == 0)
            {
                GUILayout.Label("No items available (not in game)");
                return;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("Search:", GUILayout.Width(50));
            _searchFilter = GUILayout.TextField(_searchFilter, GUILayout.Width(200));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Scrap Value: {_scrapValue}", GUILayout.Width(120));
            _scrapValue = (int)GUILayout.HorizontalSlider(_scrapValue, 0, 500, GUILayout.Width(200));
            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            bool isHost = NetworkManager.Singleton?.IsHost == true || NetworkManager.Singleton?.IsServer == true;
            if (!isHost)
            {
                GUILayout.Label("Spawning requires host.");
                GUILayout.Space(5);
            }

            var spawnables = allItems.Where(i => i != null && i.spawnPrefab != null).ToList();
            foreach (var item in spawnables)
            {
                var name = item.itemName ?? "Unknown";
                if (!string.IsNullOrEmpty(_searchFilter) &&
                    !name.ToLower().Contains(_searchFilter.ToLower()))
                    continue;

                GUILayout.BeginHorizontal();
                GUILayout.Label(name, GUILayout.Width(200));
                GUI.enabled = isHost;
                if (GUILayout.Button("Spawn", GUILayout.Width(60)))
                    SpawnItem(item);
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }
        }

        private void SpawnItem(Item item)
        {
            if (LethalMenuMod.LocalPlayer == null || item?.spawnPrefab == null) return;
            if (StartOfRound.Instance?.propsContainer == null) return;

            try
            {
                var spawnPos = LethalMenuMod.LocalPlayer.gameplayCamera.transform.position +
                               LethalMenuMod.LocalPlayer.gameplayCamera.transform.forward * 2f;
                var obj = Object.Instantiate(item.spawnPrefab, spawnPos, Quaternion.identity, StartOfRound.Instance.propsContainer);
                var grabbable = obj.GetComponent<GrabbableObject>();
                if (grabbable != null)
                {
                    grabbable.SetScrapValue(_scrapValue);
                    grabbable.fallTime = 0f;
                }
                obj.GetComponent<NetworkObject>()?.Spawn();
            }
            catch (System.Exception ex)
            {
                Loader.LogError($"Failed to spawn item: {ex.Message}");
            }
        }
    }
}
