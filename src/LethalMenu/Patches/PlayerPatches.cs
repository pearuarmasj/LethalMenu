using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace LethalMenu.Patches
{
    /// Patches for PlayerControllerB to implement various cheats.
    [HarmonyPatch(typeof(PlayerControllerB))]
    public static class PlayerPatches
    {
        /// God mode - prevent damage.
        [HarmonyPatch("DamagePlayer")]
        [HarmonyPrefix]
        public static bool DamagePlayerPrefix(PlayerControllerB __instance)
        {
            if (Hack.GodMode.IsEnabled() && __instance == LethalMenuMod.LocalPlayer)
            {
                return false;
            }
            return true;
        }

        /// God mode - prevent kill.
        [HarmonyPatch("KillPlayer")]
        [HarmonyPrefix]
        public static bool KillPlayerPrefix(PlayerControllerB __instance)
        {
            if (Hack.GodMode.IsEnabled() && __instance == LethalMenuMod.LocalPlayer)
            {
                return false;
            }
            return true;
        }

        /// No fall damage.
        [HarmonyPatch("PlayerHitGroundEffects")]
        [HarmonyPrefix]
        public static bool PlayerHitGroundPrefix(PlayerControllerB __instance)
        {
            if (Hack.NoFallDamage.IsEnabled() && __instance == LethalMenuMod.LocalPlayer)
            {
                __instance.fallValue = 0f;
                __instance.fallValueUncapped = 0f;
            }
            return true;
        }

        /// Player update - various runtime cheats.
        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        public static void UpdatePostfix(PlayerControllerB __instance)
        {
            if (__instance != LethalMenuMod.LocalPlayer) return;

            if (Hack.InfiniteStamina.IsEnabled())
            {
                __instance.sprintMeter = 1f;
            }

            if (Hack.SuperSpeed.IsEnabled())
            {
                __instance.movementSpeed = 10f;
            }

            if (Hack.FastClimb.IsEnabled())
            {
                __instance.climbSpeed = 8f;
            }

            if (Hack.UnlimitedOxygen.IsEnabled())
            {
                __instance.drunkness = 0f;
                __instance.drunknessInertia = 0f;
            }

            if (Hack.Reach.IsEnabled())
            {
                __instance.grabDistance = 30f;
            }
        }

        /// One-handed items - forces twoHanded to false.
        [HarmonyPatch("LateUpdate")]
        [HarmonyPostfix]
        public static void LateUpdatePostfix(PlayerControllerB __instance)
        {
            if (__instance != LethalMenuMod.LocalPlayer) return;

            if (Hack.OneHanded.IsEnabled())
            {
                __instance.twoHanded = false;
            }
        }

        /// TauntSlide - allow emoting while moving.
        [HarmonyPatch("CheckConditionsForEmote")]
        [HarmonyPostfix]
        public static void CheckEmotePostfix(ref bool __result, PlayerControllerB __instance)
        {
            if (Hack.TauntSlide.IsEnabled() && __instance == LethalMenuMod.LocalPlayer)
            {
                if (__instance.isPlayerControlled && !__instance.isPlayerDead && !__instance.inSpecialInteractAnimation)
                {
                    __result = true;
                }
            }
        }

        /// GrabInLobby - allow grabbing items before game starts.
        [HarmonyPatch("GrabObjectServerRpc")]
        [HarmonyPrefix]
        public static void GrabObjectPrefix(PlayerControllerB __instance)
        {
            if (Hack.GrabInLobby.IsEnabled() && __instance == LethalMenuMod.LocalPlayer)
            {
                if (StartOfRound.Instance != null)
                {
                    StartOfRound.Instance.localPlayerUsingController = false;
                }
            }
        }

        /// Unlimited jump - allows jumping in air.
        [HarmonyPatch("Jump_performed")]
        [HarmonyPrefix]
        public static bool JumpPrefix(PlayerControllerB __instance)
        {
            if (!Hack.UnlimitedJump.IsEnabled()) return true;
            if (__instance != LethalMenuMod.LocalPlayer) return true;
            if (!__instance.isPlayerControlled) return false;
            if (__instance.inSpecialInteractAnimation) return false;
            if (__instance.isTypingChat) return false;
            if (__instance.quickMenuManager?.isMenuOpen == true) return false;

            __instance.sprintMeter = Mathf.Clamp(__instance.sprintMeter - 0.08f, 0f, 1f);
            
            if (__instance.movementAudio != null && StartOfRound.Instance?.playerJumpSFX != null)
            {
                __instance.movementAudio.PlayOneShot(StartOfRound.Instance.playerJumpSFX);
            }
            
            __instance.playerSlidingTimer = 0f;
            __instance.isJumping = true;

            if (__instance.jumpCoroutine != null)
            {
                __instance.StopCoroutine(__instance.jumpCoroutine);
            }

            __instance.jumpCoroutine = __instance.StartCoroutine(__instance.PlayerJump());

            return false;
        }
    }

    /// No Quicksand patch - prevents sinking/slowing.
    [HarmonyPatch(typeof(PlayerControllerB))]
    public static class NoQuicksandPatch
    {
        [HarmonyPatch("CheckConditionsForSinkingInQuicksand")]
        [HarmonyPostfix]
        public static void Postfix(ref bool __result, PlayerControllerB __instance)
        {
            if (Hack.NoQuicksand.IsEnabled() && __instance == LethalMenuMod.LocalPlayer)
            {
                __result = false;
            }
        }
    }

    /// Super jump force.
    [HarmonyPatch(typeof(PlayerControllerB), "PlayerJump")]
    public static class SuperJumpPatches
    {
        [HarmonyPostfix]
        public static void Postfix(PlayerControllerB __instance)
        {
            if (LethalMenuMod.LocalPlayer == null || __instance != LethalMenuMod.LocalPlayer) return;
            __instance.jumpForce = Hack.SuperJump.IsEnabled() ? Settings.SuperJumpForce : 13f;
        }
    }

    /// Strong hands - two-handed items can be held one-handed.
    [HarmonyPatch]
    public static class StrongHandsPatches
    {
        [HarmonyPatch(typeof(PlayerControllerB), "LateUpdate")]
        [HarmonyPostfix]
        public static void Postfix(PlayerControllerB __instance)
        {
            if (LethalMenuMod.LocalPlayer == null || __instance.playerClientId != LethalMenuMod.LocalPlayer.playerClientId) return;
            
            var heldObject = __instance.currentlyHeldObjectServer;
            if (Hack.StrongHands.IsEnabled() && heldObject != null)
            {
                __instance.twoHanded = false;
            }
        }
    }

    /// Teleport with items - don't drop items when teleporting.
    [HarmonyPatch(typeof(PlayerControllerB), "DropAllHeldItems")]
    public static class TeleportWithItemsPatches
    {
        [HarmonyPrefix]
        public static bool Prefix()
        {
            return !Hack.TeleportWithItems.IsEnabled();
        }
    }

    /// Through walls patches.
    [HarmonyPatch]
    public static class ThroughWallsPatches
    {
        [HarmonyPatch(typeof(PlayerControllerB), "LateUpdate")]
        [HarmonyPostfix]
        public static void ThroughWallsPostfix(PlayerControllerB __instance)
        {
            if (__instance != LethalMenuMod.LocalPlayer) return;

            if (Hack.LootThroughWalls.IsEnabled() || Hack.InteractThroughWalls.IsEnabled())
            {
                __instance.grabDistance = 10000f;
                int mask = 0;
                if (Hack.LootThroughWalls.IsEnabled())
                    mask = LayerMask.GetMask("Props");
                if (Hack.InteractThroughWalls.IsEnabled())
                    mask = LayerMask.GetMask("InteractableObject");
                if (Hack.LootThroughWalls.IsEnabled() && Hack.InteractThroughWalls.IsEnabled())
                    mask = LayerMask.GetMask("Props", "InteractableObject");

                var field = __instance.GetType().GetField("interactableObjectsMask",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                field?.SetValue(__instance, mask);
            }
            else if (!Hack.Reach.IsEnabled())
            {
                var field = __instance.GetType().GetField("interactableObjectsMask",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                field?.SetValue(__instance, 832);
                __instance.grabDistance = 5f;
            }
        }
    }
}
