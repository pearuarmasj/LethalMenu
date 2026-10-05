using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace LethalMenu.Patches
{
    /// Kidnapper fox kill: BushWolfEnemy.OnCollideWithPlayer calls KillPlayer(Mauling) on the local
    /// player (while dragging, or within 16 m of the nest after being hit) behind
    /// MeetsStandardPlayerCollisionConditions. Skipping the handler for the hidden player keeps the
    /// kill independent of PlayerIsTargetable.
    [HarmonyPatch(typeof(BushWolfEnemy), nameof(BushWolfEnemy.OnCollideWithPlayer))]
    internal static class BushWolfCollisionPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(EnemyAI __instance, Collider other) =>
            !UntargetableSightPatches.IsHiddenFrom(__instance, other.gameObject.GetComponent<PlayerControllerB>());
    }

    /// Spotted meter / staring: BushWolfEnemy.Update (state 0 and 1 loops over allPlayerScripts) rates
    /// every player by PlayerControllerB.LineOfSightToPositionAngle(wolf position), where -361 means
    /// "cannot see the wolf". The hidden player's own view therefore never raises spottedMeter, never
    /// becomes staringAtPlayer (which triggers backing off, growls and SeeBushWolfServerRpc) and never
    /// counts as a watcher in state 1. The only other callers are CadaverGrowthAI's cough check
    /// (evaluated on the coughing player) and PumaAI.
    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.LineOfSightToPositionAngle))]
    internal static class BushWolfLineOfSightAnglePatch
    {
        [HarmonyPostfix]
        private static void Postfix(PlayerControllerB __instance, ref float __result)
        {
            if (UntargetableSightPatches.IsHidden(__instance))
                __result = -361f;
        }
    }

    /// Retaliation chase: BushWolfEnemy.HitEnemy / SetEnemyStunned store the attacker in lastHitByPlayer
    /// and Update state 0 then calls SetMovingTowardsTargetPlayer(lastHitByPlayer) while
    /// timeSinceTakingDamage < 2.5 s, which makes the wolf rush the hidden player. The reference is
    /// dropped before Update runs.
    [HarmonyPatch(typeof(BushWolfEnemy), nameof(BushWolfEnemy.Update))]
    internal static class BushWolfRetaliationPatch
    {
        [HarmonyPrefix]
        private static void Prefix(BushWolfEnemy __instance)
        {
            if (UntargetableSightPatches.IsHiddenFrom(__instance, __instance.lastHitByPlayer))
                __instance.lastHitByPlayer = null;
        }
    }

    /// Attack sync: the owner announces its victim with SyncTargetPlayerAndAttackServerRpc, and every
    /// non-owner client answers SyncTargetPlayerAndAttackClientRpc by setting targetPlayer to that
    /// player and entering the tongue state, where the targeted client sends HitByEnemyServerRpc
    /// (drag) from Update. A request naming the hidden player is dropped on both ends.
    [HarmonyPatch(typeof(BushWolfEnemy), nameof(BushWolfEnemy.SyncTargetPlayerAndAttackServerRpc))]
    internal static class BushWolfAttackSyncServerPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(EnemyAI __instance, int playerId) =>
            !UntargetableSightPatches.IsHiddenFrom(__instance, StartOfRound.Instance.allPlayerScripts[playerId]);
    }

    [HarmonyPatch(typeof(BushWolfEnemy), nameof(BushWolfEnemy.SyncTargetPlayerAndAttackClientRpc))]
    internal static class BushWolfAttackSyncClientPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(EnemyAI __instance, int playerId) =>
            !UntargetableSightPatches.IsHiddenFrom(__instance, StartOfRound.Instance.allPlayerScripts[playerId]);
    }
}
