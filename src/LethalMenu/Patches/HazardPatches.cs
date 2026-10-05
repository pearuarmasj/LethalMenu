using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace LethalMenu.Patches
{
    /// Turret patches - make turrets ignore the local player when Untargetable.
    /// Turret.CheckForPlayersInLineOfSight is the only player query a turret has: the host's detection and
    /// retarget loops use it, and the firing/berserk damage in Update (`CheckForPlayersInLineOfSight(3f) ==
    /// localPlayerController` -> DamagePlayer/KillPlayer) runs on the victim's own client and goes through the
    /// same method. Nulling its result for the hidden player closes detection, charging and damage on every client.
    /// A target pushed by SwitchTargetedPlayerClientRpc is dropped at the start of Update so the turret never
    /// switches to charging or aims at the hidden player.
    [HarmonyPatch]
    public static class TurretPatches
    {
        [HarmonyPatch(typeof(Turret), nameof(Turret.CheckForPlayersInLineOfSight))]
        [HarmonyPostfix]
        public static void CheckForPlayersPostfix(ref PlayerControllerB __result)
        {
            if (UntargetableSightPatches.IsHidden(__result)) __result = null!;
        }

        [HarmonyPatch(typeof(Turret), nameof(Turret.Update))]
        [HarmonyPrefix]
        public static void UpdatePrefix(Turret __instance)
        {
            if (UntargetableSightPatches.IsHidden(__instance.targetPlayerWithRotation))
                __instance.targetPlayerWithRotation = null;
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
