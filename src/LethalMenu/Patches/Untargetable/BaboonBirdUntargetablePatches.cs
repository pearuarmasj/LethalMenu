using System;
using System.Reflection;
using GameNetcodeStuff;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

namespace LethalMenu.Patches
{
    // Baboon hawk sight (DoLOSCheck skips IVisibleThreats whose GetVisibility() is 0) is closed by
    // PlayerVisibleThreatPatches in RadMechAIUntargetablePatches.cs, which zeroes the hidden player's visibility.

    /// Already-focused threat: a baboon that focused the player before Untargetable was enabled keeps
    /// focusedThreat (aggressiveMode 2 in DoAIInterval walks to the threat's position and fights) for
    /// up to 5 s after the last sighting. The owner forgets the threat and stops focusing.
    [HarmonyPatch(typeof(BaboonBirdAI), nameof(BaboonBirdAI.DoAIInterval))]
    internal static class BaboonBirdFocusPatch
    {
        [HarmonyPrefix]
        private static void Prefix(BaboonBirdAI __instance)
        {
            if (__instance.focusedThreat?.threatScript is not PlayerControllerB player || !UntargetableSightPatches.IsHiddenFrom(__instance, player))
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
        private static bool Prefix(EnemyAI __instance, NetworkObjectReference netObject) =>
            !(netObject.TryGet(out var networkObject) &&
              networkObject.TryGetComponent(out PlayerControllerB player) &&
              UntargetableSightPatches.IsHiddenFrom(__instance, player));
    }

    /// Stab: BaboonBirdAI.OnCollideWithPlayer calls DamagePlayer(20) on the local player after
    /// MeetsStandardPlayerCollisionConditions (and StabPlayerDeathAnimServerRpc if that kills).
    /// Skipping the handler for the hidden player keeps the attack independent of PlayerIsTargetable.
    [HarmonyPatch(typeof(BaboonBirdAI), nameof(BaboonBirdAI.OnCollideWithPlayer))]
    internal static class BaboonBirdCollisionPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(EnemyAI __instance, Collider other) =>
            !UntargetableSightPatches.IsHiddenFrom(__instance, other.gameObject.GetComponent<PlayerControllerB>());
    }
}
