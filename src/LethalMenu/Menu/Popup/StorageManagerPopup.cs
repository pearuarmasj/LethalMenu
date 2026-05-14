using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace LethalMenu.Menu.Popup
{
    public class StorageManagerPopup : PopupMenu
    {
        private int _selectedStorageIndex;
        private int _selectedShipObjectIndex;
        private Vector2 _storageScrollPosition;
        private Vector2 _shipObjectsScrollPosition;

        public StorageManagerPopup() : base("Storage", 20009, 620, 560) { }

        protected override void DrawBody()
        {
            var startOfRound = StartOfRound.Instance;
            if (startOfRound == null)
            {
                GUILayout.Label("Not in game");
                return;
            }

            var stored = GetStoredUnlockables(startOfRound);
            var shipObjects = GetStorableShipObjects(startOfRound);

            DrawStoredObjects(startOfRound, stored);

            GUILayout.Space(12);
            DrawShipObjects(startOfRound, shipObjects);
        }

        private void DrawStoredObjects(StartOfRound startOfRound, List<(int index, UnlockableItem item)> stored)
        {
            GUILayout.Label("--- Stored Objects ---");
            if (stored.Count == 0)
            {
                GUILayout.Label("[No items stored. While moving an object with B, press X to store it.]");
                return;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Stored Objects: {stored.Count}");
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Return All", GUILayout.Width(100)))
            {
                foreach (var entry in stored)
                    ReturnStoredUnlockable(startOfRound, entry.index);
            }
            GUILayout.EndHorizontal();

            _selectedStorageIndex = Mathf.Clamp(_selectedStorageIndex, 0, stored.Count - 1);

            _storageScrollPosition = GUILayout.BeginScrollView(_storageScrollPosition, GUILayout.Height(220));
            for (int i = 0; i < stored.Count; i++)
            {
                var (unlockableId, item) = stored[i];
                GUILayout.BeginHorizontal();
                bool selected = i == _selectedStorageIndex;
                if (GUILayout.Toggle(selected, "", GUILayout.Width(20)))
                    _selectedStorageIndex = i;
                GUILayout.Label($"{item.unlockableName ?? $"Unlockable {unlockableId}"}{GetStorageTypeTag(item)}");
                if (GUILayout.Button("Return", GUILayout.Width(80)))
                {
                    ReturnStoredUnlockable(startOfRound, unlockableId);
                    _selectedStorageIndex = Mathf.Clamp(i, 0, stored.Count - 2);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();

            GUILayout.Space(8);

            if (_selectedStorageIndex >= 0 && _selectedStorageIndex < stored.Count)
            {
                var (unlockableId, item) = stored[_selectedStorageIndex];
                GUILayout.Label("--- Selected ---");
                GUILayout.Label($"ID: {unlockableId}");
                GUILayout.Label($"Name: {item.unlockableName ?? "Unknown"}");
                GUILayout.Label($"Type: {GetStorageTypeName(item)}");
                GUILayout.Label($"Spawn Prefab: {item.spawnPrefab}");
                GUILayout.Label($"Max Number: {item.maxNumber}");
            }
        }

        private void DrawShipObjects(StartOfRound startOfRound, List<(PlaceableShipObject placeable, UnlockableItem item)> shipObjects)
        {
            GUILayout.Label("--- In Ship ---");
            if (shipObjects.Count == 0)
            {
                GUILayout.Label("No storable ship objects found.");
                return;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Storable Objects: {shipObjects.Count}");
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Store All", GUILayout.Width(100)))
            {
                foreach (var entry in shipObjects)
                    StoreShipObject(startOfRound, entry.placeable);
            }
            GUILayout.EndHorizontal();

            _selectedShipObjectIndex = Mathf.Clamp(_selectedShipObjectIndex, 0, shipObjects.Count - 1);

            _shipObjectsScrollPosition = GUILayout.BeginScrollView(_shipObjectsScrollPosition, GUILayout.Height(180));
            for (int i = 0; i < shipObjects.Count; i++)
            {
                var (placeable, item) = shipObjects[i];
                GUILayout.BeginHorizontal();
                bool selected = i == _selectedShipObjectIndex;
                if (GUILayout.Toggle(selected, "", GUILayout.Width(20)))
                    _selectedShipObjectIndex = i;
                GUILayout.Label($"{item.unlockableName ?? $"Unlockable {placeable.unlockableID}"}{GetStorageTypeTag(item)}");
                if (GUILayout.Button("Store", GUILayout.Width(80)))
                {
                    StoreShipObject(startOfRound, placeable);
                    _selectedShipObjectIndex = Mathf.Clamp(i, 0, shipObjects.Count - 2);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();

            GUILayout.Space(8);

            if (_selectedShipObjectIndex >= 0 && _selectedShipObjectIndex < shipObjects.Count)
            {
                var (placeable, item) = shipObjects[_selectedShipObjectIndex];
                GUILayout.Label("--- Selected Ship Object ---");
                GUILayout.Label($"ID: {placeable.unlockableID}");
                GUILayout.Label($"Name: {item.unlockableName ?? "Unknown"}");
                GUILayout.Label($"Type: {GetStorageTypeName(item)}");
                GUILayout.Label($"Can Store: {item.canBeStored}");
                GUILayout.Label($"Contains Scrap: {ContainsScrap(placeable)}");
            }
        }

        private static List<(int index, UnlockableItem item)> GetStoredUnlockables(StartOfRound startOfRound)
        {
            var result = new List<(int, UnlockableItem)>();
            var unlockables = startOfRound.unlockablesList?.unlockables;
            if (unlockables == null)
                return result;

            for (int i = 0; i < unlockables.Count; i++)
            {
                var item = unlockables[i];
                if (item != null && item.inStorage)
                    result.Add((i, item));
            }

            return result;
        }

        private static List<(PlaceableShipObject placeable, UnlockableItem item)> GetStorableShipObjects(StartOfRound startOfRound)
        {
            var result = new List<(PlaceableShipObject, UnlockableItem)>();
            var unlockables = startOfRound.unlockablesList?.unlockables;
            if (unlockables == null)
                return result;

            foreach (var placeable in Object.FindObjectsOfType<PlaceableShipObject>())
            {
                if (placeable == null)
                    continue;

                if (placeable.unlockableID < 0 || placeable.unlockableID >= unlockables.Count)
                    continue;

                var item = unlockables[placeable.unlockableID];
                if (!CanShowAsPlayerOwnedShipObject(item))
                    continue;

                if (placeable.parentObject == null || placeable.parentObject.GetComponent<NetworkObject>() == null)
                    continue;

                if (ContainsScrap(placeable))
                    continue;

                result.Add((placeable, item));
            }

            return result;
        }

        private static void ReturnStoredUnlockable(StartOfRound startOfRound, int unlockableId)
        {
            if (startOfRound.shipIsLeaving || startOfRound.travellingToNewLevel || RoundManager.Instance?.dungeonIsGenerating == true)
            {
                HUDManager.Instance?.DisplayTip("Storage", "Cannot return while ship/level is busy.");
                return;
            }

            startOfRound.ReturnUnlockableFromStorageServerRpc(unlockableId);
            Loader.Log($"[Storage] Returned unlockable {unlockableId} from storage");
        }

        private static void StoreShipObject(StartOfRound startOfRound, PlaceableShipObject placeable)
        {
            if (placeable == null)
                return;

            var buildMode = ShipBuildModeManager.Instance;
            var localPlayer = GameNetworkManager.Instance?.localPlayerController;
            var networkObject = placeable.parentObject != null ? placeable.parentObject.GetComponent<NetworkObject>() : null;

            if (buildMode == null || localPlayer == null || networkObject == null)
            {
                HUDManager.Instance?.DisplayTip("Storage", "Storage system not available.");
                return;
            }

            if (startOfRound.shipIsLeaving || startOfRound.travellingToNewLevel || RoundManager.Instance?.dungeonIsGenerating == true)
            {
                HUDManager.Instance?.DisplayTip("Storage", "Cannot store while ship/level is busy.");
                return;
            }

            if (placeable.unlockableID < 0 ||
                startOfRound.unlockablesList?.unlockables == null ||
                placeable.unlockableID >= startOfRound.unlockablesList.unlockables.Count)
            {
                return;
            }

            var item = startOfRound.unlockablesList.unlockables[placeable.unlockableID];
            if (!CanShowAsPlayerOwnedShipObject(item) || ContainsScrap(placeable))
                return;

            buildMode.StoreObjectServerRpc(networkObject, (int)localPlayer.playerClientId);
            Loader.Log($"[Storage] Stored unlockable {placeable.unlockableID} ({item.unlockableName})");
        }

        private static bool ContainsScrap(PlaceableShipObject placeable)
        {
            return placeable.parentObject != null &&
                placeable.parentObject.gameObject.GetComponentInChildren<GrabbableObject>() != null;
        }

        private static bool CanShowAsPlayerOwnedShipObject(UnlockableItem? item)
        {
            return item != null &&
                !item.inStorage &&
                item.canBeStored &&
                item.hasBeenUnlockedByPlayer &&
                !item.alreadyUnlocked;
        }

        private static string GetStorageTypeTag(UnlockableItem item) => $" [{GetStorageTypeName(item)}]";

        private static string GetStorageTypeName(UnlockableItem item)
        {
            if (item.suitMaterial != null)
                return "Suit";

            return item.unlockableType switch
            {
                0 => "Decor",
                1 => "Upgrade",
                _ => $"Type {item.unlockableType}",
            };
        }
    }
}
