using System;
using System.Reflection;
using GameNetcodeStuff;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

namespace LethalMenu.Patches
{
    /// Baboon hawk sight: BaboonBirdAI.DoLOSCheck overlap-spheres visibleThreatsMask, takes every
    /// IVisibleThreat (PlayerControllerB implements it explicitly) and skips a threat whose
    /// GetVisibility() is 0. The hidden player reports 0, so it never enters `threats`, never
    /// raises fearLevel and is never passed to ReactToThreat / focused.
    [HarmonyPatch]
    internal static class BaboonBirdVisibilityPatch
    {
        private static MethodBase TargetMethod()
        {
            var map = typeof(PlayerControllerB).GetInterfaceMap(typeof(IVisibleThreat));
            int index = Array.IndexOf(map.InterfaceMethods, typeof(IVisibleThreat).GetMethod(nameof(IVisibleThreat.GetVisibility)));
            return map.TargetMethods[index];
        }

        [HarmonyPostfix]
        private static void Postfix(PlayerControllerB __instance, ref float __result)
        {
            if (UntargetableSightPatches.IsHidden(__instance))
                __result = 0f;
        }
    }

    /// Already-focused threat: a baboon that focused the player before Untargetable was enabled keeps
    /// focusedThreat (aggressiveMode 2 in DoAIInterval walks to the threat's position and fights) for
    /// up to 5 s after the last sighting. The owner forgets the threat and stops focusing.
    [HarmonyPatch(typeof(BaboonBirdAI), nameof(BaboonBirdAI.DoAIInterval))]
    internal static class BaboonBirdFocusPatch
    {
        [HarmonyPrefix]
        private static void Prefix(BaboonBirdAI __instance)
        {
            if (__instance.focusedThreat?.threatScript is not PlayerControllerB player || !UntargetableSightPatches.IsHidden(player))
                return;

            __instance.threats.Remove(player.transform);
            __instance.StopFocusingThreat();
            __instance.focusedThreat = null;
            __instance.focusingOnThreat = false;
            __instance.focusedThreatIsInView = false;
            __instance.focusedThreatTransform = null;
        }
    }

    /// Focus sync: the owner announces its focused threat with StartFocusOnThreatServerRpc and every
    /// non-owner client answers StartFocusOnThreatClientRpc by entering the threat state and staring
    /// at that threat's look transform. A request naming the hidden player is dropped on this client.
    [HarmonyPatch(typeof(BaboonBirdAI), nameof(BaboonBirdAI.StartFocusOnThreatClientRpc))]
    internal static class BaboonBirdFocusSyncPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(NetworkObjectReference netObject) =>
            !(netObject.TryGet(out var networkObject) &&
              networkObject.TryGetComponent(out PlayerControllerB player) &&
              UntargetableSightPatches.IsHidden(player));
    }

    /// Stab: BaboonBirdAI.OnCollideWithPlayer calls DamagePlayer(20) on the local player after
    /// MeetsStandardPlayerCollisionConditions (and StabPlayerDeathAnimServerRpc if that kills).
    /// Skipping the handler for the hidden player keeps the attack independent of PlayerIsTargetable.
    [HarmonyPatch(typeof(BaboonBirdAI), nameof(BaboonBirdAI.OnCollideWithPlayer))]
    internal static class BaboonBirdCollisionPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(Collider other) =>
            !UntargetableSightPatches.IsHidden(other.gameObject.GetComponent<PlayerControllerB>());
    }
}
