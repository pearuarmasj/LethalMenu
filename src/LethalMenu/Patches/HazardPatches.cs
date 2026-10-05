using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace LethalMenu.Patches
{
    /// Turret patches - make turrets ignore local player when Untargetable.
    [HarmonyPatch(typeof(Turret))]
    public static class TurretPatches
    {
        /// Return null if turret would target local player.
        [HarmonyPatch("CheckForPlayersInLineOfSight")]
        [HarmonyPostfix]
        public static void CheckForPlayersPostfix(ref PlayerControllerB __result)
        {
            if (!Hack.Untargetable.IsEnabled()) return;
            if (__result == LethalMenuMod.LocalPlayer)
            {
                __result = null!;
            }
        }

        /// Clear turret target if it's the local player.
        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        public static void UpdatePostfix(Turret __instance)
        {
            if (!Hack.Untargetable.IsEnabled()) return;
            if (__instance.targetPlayerWithRotation == LethalMenuMod.LocalPlayer)
            {
                __instance.targetPlayerWithRotation = null;
            }
        }
    }

    /// Anti-flash patches for HUDManager.
    [HarmonyPatch(typeof(HUDManager))]
    public static class HUDPatches
    {
        /// Disable flash filter (stun grenades).
        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        public static void UpdatePostfix(HUDManager __instance)
        {
            LethalMenuMod.HUD = __instance;

            if (Hack.AntiFlash.IsEnabled())
            {
                __instance.flashFilter = 0f;
            }
        }
    }

    /// Anti-flash patches for SoundManager (ears ringing).
    [HarmonyPatch(typeof(SoundManager))]
    public static class SoundPatches
    {
        /// Disable ears ringing from stun grenades.
        [HarmonyPatch("SetEarsRinging")]
        [HarmonyPrefix]
        public static bool SetEarsRingingPrefix()
        {
            return !Hack.AntiFlash.IsEnabled();
        }
    }

    /// Instant interact - skip hold interaction delay.
    [HarmonyPatch(typeof(HUDManager))]
    public static class InstantInteractPatches
    {
        [HarmonyPatch("HoldInteractionFill")]
        [HarmonyPrefix]
        public static bool HoldInteractionFillPrefix(ref bool __result)
        {
            if (Hack.InstantInteract.IsEnabled())
            {
                __result = true;
                return false;
            }
            return true;
        }
    }

    /// No camera shake.
    [HarmonyPatch(typeof(HUDManager), "ShakeCamera")]
    public static class NoCameraShakePatches
    {
        [HarmonyPrefix]
        public static bool Prefix()
        {
            return !Hack.NoCameraShake.IsEnabled();
        }
    }
}
