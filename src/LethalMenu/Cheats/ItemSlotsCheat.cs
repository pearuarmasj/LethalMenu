using GameNetcodeStuff;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LethalMenu.Cheats
{
    /// Expands player inventory from 4 slots to a configurable amount.
    /// The patches are registered in GamePatches.cs and activate based on Settings.
    /// Requires game restart to take effect when changed.
    public class ItemSlotsCheat : CheatBase
    {
        public override string Name => "Extra Item Slots";

        public override void OnUpdate()
        {
            IsEnabled = Settings.ExtraItemSlots && Settings.ItemSlotCount != 4;
        }
    }
}

namespace LethalMenu.Patches
{
    /// Patch PlayerControllerB.Awake to expand the ItemSlots array.
    [HarmonyPatch(typeof(PlayerControllerB), "Awake")]
    public static class ItemSlotsPlayerPatch
    {
        [HarmonyPostfix]
        public static void Postfix(PlayerControllerB __instance)
        {
            try
            {
                if (!Settings.ExtraItemSlots) return;
                
                int slotCount = Settings.ItemSlotCount;
                if (slotCount <= 4) return;

                // Resize the item slots array
                __instance.ItemSlots = new GrabbableObject[slotCount];
                Debug.Log($"[ItemSlots] Expanded player {__instance.playerUsername} slots to {slotCount}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ItemSlots] PlayerControllerB patch error: {ex}");
            }
        }
    }

    /// Patch HUDManager.Awake to expand the inventory UI.
    [HarmonyPatch(typeof(HUDManager), "Awake")]
    public static class ItemSlotsHUDPatch
    {
        [HarmonyPostfix]
        public static void Postfix(HUDManager __instance)
        {
            try
            {
                if (!Settings.ExtraItemSlots) return;
                
                int slotCount = Settings.ItemSlotCount;
                if (slotCount <= 4) return;

                // itemSlotIcons and itemSlotIconFrames are public
                if (__instance.itemSlotIconFrames == null || __instance.itemSlotIconFrames.Length == 0)
                {
                    Debug.LogError("[ItemSlots] No item slot frames found");
                    return;
                }

                // Get the parent container
                Transform parent = __instance.itemSlotIconFrames[0].transform.parent;
                
                // Clone the first slot as a template BEFORE destroying anything
                GameObject template = Object.Instantiate(__instance.itemSlotIconFrames[0].gameObject);
                template.SetActive(false); // Hide template

                // Collect existing children to destroy
                List<GameObject> toDestroy = new List<GameObject>();
                foreach (Transform child in parent)
                {
                    toDestroy.Add(child.gameObject);
                }

                // Destroy existing children immediately
                foreach (var obj in toDestroy)
                {
                    Object.DestroyImmediate(obj);
                }

                // Adjust the container to fit more slots
                RectTransform rect = parent.GetComponent<RectTransform>();
                if (rect != null)
                {
                    // Scale down as more slots are added
                    float scale = Mathf.Clamp(1f - (slotCount - 4) * 0.05f, 0.5f, 1f);
                    rect.localScale = new Vector3(scale, scale, 1f);
                    rect.anchorMin = new Vector2(0.35f, 0f);
                    rect.anchorMax = new Vector2(0.65f, 0.3f);
                    rect.pivot = new Vector2(0.5f, 0f);
                    rect.anchoredPosition = Vector2.zero;
                }

                // Add grid layout for proper arrangement
                GridLayoutGroup layoutGroup = parent.gameObject.GetComponent<GridLayoutGroup>();
                if (layoutGroup == null)
                {
                    layoutGroup = parent.gameObject.AddComponent<GridLayoutGroup>();
                }
                layoutGroup.spacing = new Vector2(15f, 15f);
                layoutGroup.cellSize = new Vector2(50f, 50f);
                layoutGroup.childAlignment = TextAnchor.LowerCenter;

                // Create new slots
                Image[] newIcons = new Image[slotCount];
                Image[] newFrames = new Image[slotCount];

                for (int i = 0; i < slotCount; i++)
                {
                    GameObject slot = Object.Instantiate(template, parent);
                    slot.name = $"Slot{i}";
                    slot.SetActive(true);
                    
                    newFrames[i] = slot.GetComponent<Image>();
                    if (slot.transform.childCount > 0)
                    {
                        newIcons[i] = slot.transform.GetChild(0).GetComponent<Image>();
                        // Reset the icon state
                        if (newIcons[i] != null)
                        {
                            newIcons[i].enabled = false;
                        }
                    }
                }

                // Destroy the template
                Object.DestroyImmediate(template);

                // Update the HUDManager's arrays directly (they're public)
                __instance.itemSlotIcons = newIcons;
                __instance.itemSlotIconFrames = newFrames;

                Debug.Log($"[ItemSlots] HUD expanded to {slotCount} slots");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ItemSlots] HUDManager patch error: {ex}");
            }
        }
    }
}
