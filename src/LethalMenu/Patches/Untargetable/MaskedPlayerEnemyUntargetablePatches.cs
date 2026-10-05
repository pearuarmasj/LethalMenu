using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace LethalMenu.Patches
{
    /// Masked strangle: MaskedPlayerEnemy.OnCollideWithPlayer sends KillPlayerAnimationServerRpc(local id)
    /// after MeetsStandardPlayerCollisionConditions. Skipping the handler for the hidden player keeps
    /// the kill independent of PlayerIsTargetable.
    /// Detection (CheckLineOfSightForClosestPlayer in DoAIInterval, GetClosestPlayer in the ship state)
    /// is already covered by the sight transpiler / PlayerIsTargetable.
    [HarmonyPatch(typeof(MaskedPlayerEnemy), nameof(MaskedPlayerEnemy.OnCollideWithPlayer))]
    internal static class MaskedCollisionPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(EnemyAI __instance, Collider other) =>
            !UntargetableSightPatches.IsHiddenFrom(__instance, other.gameObject.GetComponent<PlayerControllerB>());
    }

    /// Kill animation requests naming the hidden player: MaskedPlayerEnemy.KillPlayerAnimationServerRpc
    /// (RequireOwnership = false, runs on the host for any sender) and KillPlayerAnimationClientRpc
    /// (killAnimation() ends in KillPlayer and mimic creation).
    [HarmonyPatch(typeof(MaskedPlayerEnemy), nameof(MaskedPlayerEnemy.KillPlayerAnimationServerRpc))]
    internal static class MaskedKillAnimationServerPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(EnemyAI __instance, int playerObjectId) =>
            !UntargetableSightPatches.IsHiddenFrom(__instance, StartOfRound.Instance.allPlayerScripts[playerObjectId]);
    }

    [HarmonyPatch(typeof(MaskedPlayerEnemy), nameof(MaskedPlayerEnemy.KillPlayerAnimationClientRpc))]
    internal static class MaskedKillAnimationClientPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(EnemyAI __instance, int playerObjectId) =>
            !UntargetableSightPatches.IsHiddenFrom(__instance, StartOfRound.Instance.allPlayerScripts[playerObjectId]);
    }

    /// Stare: LookAtPlayerClientRpc makes every client's Masked set stareAtTransform to the named
    /// player's camera and turn to face it; the owner sends it from DoAIInterval for whoever it
    /// chose. A request naming the hidden player is dropped on both ends.
    [HarmonyPatch(typeof(MaskedPlayerEnemy), nameof(MaskedPlayerEnemy.LookAtPlayerServerRpc))]
    internal static class MaskedLookAtPlayerServerPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(EnemyAI __instance, int playerId) =>
            !UntargetableSightPatches.IsHiddenFrom(__instance, StartOfRound.Instance.allPlayerScripts[playerId]);
    }

    [HarmonyPatch(typeof(MaskedPlayerEnemy), nameof(MaskedPlayerEnemy.LookAtPlayerClientRpc))]
    internal static class MaskedLookAtPlayerClientPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(EnemyAI __instance, int playerId) =>
            !UntargetableSightPatches.IsHiddenFrom(__instance, StartOfRound.Instance.allPlayerScripts[playerId]);
    }
}
