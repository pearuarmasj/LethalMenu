using System.Collections;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LethalMenu.Patches
{
    /// No depth of field.
    [HarmonyPatch(typeof(UnityEngine.Rendering.HighDefinition.DepthOfField), "IsActive")]
    public static class NoFieldOfDepthPatches
    {
        [HarmonyPrefix]
        public static bool Prefix()
        {
            return !Hack.NoDepthOfField.IsEnabled();
        }
    }

    /// Full render resolution - increases render texture to screen resolution.
    [HarmonyPatch(typeof(GameNetcodeStuff.PlayerControllerB), "Start")]
    public static class FullRenderResolutionPatch
    {
        private static RenderTexture? _originalTexture;
        private static RenderTexture? _highResTexture;
        private static bool _applied = false;
        
        [HarmonyPostfix]
        public static void Postfix(GameNetcodeStuff.PlayerControllerB __instance)
        {
            if (__instance.IsOwner)
            {
                ApplyResolution(__instance);
            }
        }
        
        public static void ApplyResolution(GameNetcodeStuff.PlayerControllerB? player)
        {
            if (player?.gameplayCamera == null) return;
            
            var camera = player.gameplayCamera;
            var currentTexture = camera.targetTexture;
            
            if (Hack.FullRenderResolution.IsEnabled())
            {
                if (_originalTexture == null && currentTexture != null)
                {
                    _originalTexture = currentTexture;
                }
                
                if (_highResTexture == null || _highResTexture.width != Screen.width || _highResTexture.height != Screen.height)
                {
                    if (_highResTexture != null)
                    {
                        _highResTexture.Release();
                        Object.Destroy(_highResTexture);
                    }
                    
                    if (_originalTexture != null)
                    {
                        _highResTexture = new RenderTexture(Screen.width, Screen.height, _originalTexture.depth, _originalTexture.format)
                        {
                            filterMode = FilterMode.Point,
                            name = "LethalMenu_HighResRT"
                        };
                        _highResTexture.Create();
                    }
                    else
                    {
                        _highResTexture = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32)
                        {
                            filterMode = FilterMode.Point,
                            name = "LethalMenu_HighResRT"
                        };
                        _highResTexture.Create();
                    }
                }
                
                if (_highResTexture != null && camera.targetTexture != _highResTexture)
                {
                    camera.targetTexture = _highResTexture;
                    _applied = true;
                    Debug.Log($"[FullRenderRes] Applied high-res texture: {Screen.width}x{Screen.height}");
                }
            }
            else if (_applied && _originalTexture != null)
            {
                camera.targetTexture = _originalTexture;
                _applied = false;
                Debug.Log("[FullRenderRes] Restored original texture");
            }
        }
        
        public static void Reset()
        {
            if (_highResTexture != null)
            {
                _highResTexture.Release();
                Object.Destroy(_highResTexture);
                _highResTexture = null;
            }
            _originalTexture = null;
            _applied = false;
        }
    }

    /// Patches for LobbySlot to mark lobbies from hosts who kicked you.
    [HarmonyPatch(typeof(LobbySlot))]
    public static class LobbySlotPatches
    {
        /// Mark lobbies from hosts who kicked you with a red highlight.
        [HarmonyPatch("Awake")]
        [HarmonyPostfix]
        public static void AwakePostfix(LobbySlot __instance)
        {
            if (!Hack.ShowKickedLobbies.IsEnabled()) return;
            
            __instance.StartCoroutine(CheckKickedLobby(__instance));
        }

        private static IEnumerator CheckKickedLobby(LobbySlot slot)
        {
            yield return new WaitForEndOfFrame();
            
            try
            {
                string lobbyName = slot.thisLobby.GetData("name");
                if (string.IsNullOrEmpty(lobbyName)) yield break;

                ulong ownerId = slot.thisLobby.Owner.Id;
                
                if (Settings.KickedHostIds.Contains(ownerId))
                {
                    ApplyKickedLobbyStyle(slot, "Host Kicked You", new Color(0.8f, 0.2f, 0.2f, 0.5f));
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[LethalMenu] Error checking kicked lobby: {ex.Message}");
            }
        }

        /// Apply visual styling to mark a lobby.
        private static void ApplyKickedLobbyStyle(LobbySlot slot, string labelText, Color bgColor)
        {
            if (slot.transform.name.Contains("Challenge")) return;

            var image = slot.GetComponent<Image>();
            if (image != null)
            {
                image.color = bgColor;
            }

            GameObject labelObj = new GameObject("KickedLabel");
            labelObj.transform.SetParent(slot.transform, false);
            
            TextMeshProUGUI label = labelObj.AddComponent<TextMeshProUGUI>();
            label.text = labelText;
            label.font = slot.playerCount.font;
            label.fontSize = 12;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(1f, 0.3f, 0.3f, 1f);
            label.enableWordWrapping = false;
            label.raycastTarget = false;

            RectTransform rect = labelObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200, 20);
            rect.anchorMin = new Vector2(0.5f, 0);
            rect.anchorMax = new Vector2(0.5f, 0);
            rect.pivot = new Vector2(0.5f, 0);
            rect.anchoredPosition = new Vector2(0, -8);
        }
    }
}
