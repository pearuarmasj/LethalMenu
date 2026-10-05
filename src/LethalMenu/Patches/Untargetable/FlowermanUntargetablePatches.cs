using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace LethalMenu.Patches
{
    /// Bracken neck-snap: FlowermanAI.OnCollideWithPlayer sends KillPlayerAnimationServerRpc(local id)
    /// after MeetsStandardPlayerCollisionConditions. Skipping the handler for the hidden player keeps
    /// the kill independent of PlayerIsTargetable.
    [HarmonyPatch(typeof(FlowermanAI), nameof(FlowermanAI.OnCollideWithPlayer))]
    internal static class FlowermanCollisionPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(EnemyAI __instance, Collider other) =>
            !UntargetableSightPatches.IsHiddenFrom(__instance, other.gameObject.GetComponent<PlayerControllerB>());
    }

    /// Kill animation requests naming the hidden player: FlowermanAI.KillPlayerAnimationServerRpc
    /// (RequireOwnership = false, runs on the host for any sender) and KillPlayerAnimationClientRpc
    /// (puts the player in inAnimationWithEnemy and runs killAnimation(), which calls KillPlayer).
    [HarmonyPatch(typeof(FlowermanAI), nameof(FlowermanAI.KillPlayerAnimationServerRpc))]
    internal static class FlowermanKillAnimationServerPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(EnemyAI __instance, int playerObjectId) =>
            !UntargetableSightPatches.IsHiddenFrom(__instance, StartOfRound.Instance.allPlayerScripts[playerObjectId]);
    }

    [HarmonyPatch(typeof(FlowermanAI), nameof(FlowermanAI.KillPlayerAnimationClientRpc))]
    internal static class FlowermanKillAnimationClientPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(EnemyAI __instance, int playerObjectId) =>
            !UntargetableSightPatches.IsHiddenFrom(__instance, StartOfRound.Instance.allPlayerScripts[playerObjectId]);
    }

    /// Stare-down escalation: FlowermanAI.Update lets the local player's line of sight to the Bracken
    /// call LookAtFlowermanTrigger(local id) and ResetFlowermanStealthTimerServerRpc(local id); on the
    /// owner this rolls a stare-down and AddToAngerMeter, so the hidden player could provoke the
    /// Bracken. Both entry points ignore the hidden player. The Bracken's retreat when looked at is
    /// left alone: it never targets or harms the player.
    [HarmonyPatch(typeof(FlowermanAI), nameof(FlowermanAI.LookAtFlowermanTrigger))]
    internal static class FlowermanStareTriggerPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(EnemyAI __instance, int playerObj) =>
            !UntargetableSightPatches.IsHiddenFrom(__instance, StartOfRound.Instance.allPlayerScripts[playerObj]);
    }

    [HarmonyPatch(typeof(FlowermanAI), nameof(FlowermanAI.ResetFlowermanStealthTimerServerRpc))]
    internal static class FlowermanStealthResetPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(EnemyAI __instance, int playerObj) =>
            !UntargetableSightPatches.IsHiddenFrom(__instance, StartOfRound.Instance.allPlayerScripts[playerObj]);
    }
}
