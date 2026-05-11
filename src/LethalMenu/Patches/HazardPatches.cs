using GameNetcodeStuff;
using HarmonyLib;
using LethalMenu.Cheats;
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

    /// Third-person camera patches for menu/terminal/death state transitions.
    [HarmonyPatch(typeof(QuickMenuManager))]
    public static class ThirdPersonMenuPatches
    {
        /// Disable third-person when opening quick menu.
        [HarmonyPatch("OpenQuickMenu")]
        [HarmonyPrefix]
        public static void OpenQuickMenuPrefix()
        {
            ThirdPersonCheat.StoreAndDisable();
        }

        /// Restore third-person when closing quick menu.
        [HarmonyPatch("CloseQuickMenu")]
        [HarmonyPrefix]
        public static void CloseQuickMenuPrefix()
        {
            ThirdPersonCheat.RestoreState();
        }
    }

    /// Third-person patches for terminal transitions.
    [HarmonyPatch(typeof(Terminal))]
    public static class ThirdPersonTerminalPatches
    {
        /// Disable third-person when entering terminal.
        [HarmonyPatch("BeginUsingTerminal")]
        [HarmonyPrefix]
        public static void BeginUsingTerminalPrefix()
        {
            ThirdPersonCheat.StoreAndDisable();
        }

        /// Restore third-person when exiting terminal.
        [HarmonyPatch("QuitTerminal")]
        [HarmonyPrefix]
        public static void QuitTerminalPrefix()
        {
            ThirdPersonCheat.RestoreState();
        }
    }

    /// Third-person patches for player death.
    [HarmonyPatch(typeof(GameNetcodeStuff.PlayerControllerB))]
    public static class ThirdPersonDeathPatches
    {
        /// Disable third-person on death.
        [HarmonyPatch("KillPlayer")]
        [HarmonyPrefix]
        public static void KillPlayerPrefix(GameNetcodeStuff.PlayerControllerB __instance)
        {
            if (__instance == LethalMenuMod.LocalPlayer)
            {
                ThirdPersonCheat.ForceDisable();
            }
        }
    }
}
