using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace LethalMenu.Patches
{
    /// Spore lizard bite: PufferAI.OnCollideWithPlayer calls DamagePlayer(20) on the local player after
    /// MeetsStandardPlayerCollisionConditions. Skipping the handler for the hidden player keeps the
    /// bite independent of PlayerIsTargetable.
    [HarmonyPatch(typeof(PufferAI), nameof(PufferAI.OnCollideWithPlayer))]
    internal static class PufferCollisionPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(EnemyAI __instance, Collider other) =>
            !UntargetableSightPatches.IsHiddenFrom(__instance, other.gameObject.GetComponent<PlayerControllerB>());
    }

    /// Stale targets: PufferAI keeps closestSeenPlayer across DoAIInterval calls and, in state 2,
    /// runs SetMovingTowardsTargetPlayer(closestSeenPlayer); in state 0 a stun makes it hand
    /// ownership to stunnedByPlayer and enter state 1. Detection itself uses the patched
    /// CheckLineOfSightForPlayer / CheckLineOfSightForClosestPlayer, so only a player recorded before
    /// Untargetable was enabled (or the one who stunned it) can still be remembered; both are dropped.
    [HarmonyPatch(typeof(PufferAI), nameof(PufferAI.Update))]
    internal static class PufferTargetPatch
    {
        [HarmonyPrefix]
        private static void Prefix(PufferAI __instance)
        {
            if (UntargetableSightPatches.IsHiddenFrom(__instance, __instance.closestSeenPlayer))
                __instance.closestSeenPlayer = null;
            if (UntargetableSightPatches.IsHiddenFrom(__instance, __instance.stunnedByPlayer))
                __instance.stunnedByPlayer = null;
        }
    }
}
