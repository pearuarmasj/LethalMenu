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
}
