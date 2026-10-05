using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace LethalMenu.Patches
{
    /// ButlerEnemyAI picks its murder target in CheckLOS (owner client, every Update), which loops over
    /// allPlayerScripts with its own isPlayerControlled/CheckLineOfSightForPosition test and never calls
    /// PlayerIsTargetable. Hiding the local player from that loop clears seenPlayers for it, so it can't be
    /// the watched/target player or counted in playersInVicinity.
    /// - Update prefix: when the Butler already targets the hidden player in the murder state (another client's
    ///   CheckLOS saw it and moved ownership here via SwitchOwnershipAndSetToStateServerRpc), hand it back the
    ///   way the vanilla "lost in chase" exit does (state 0, ownership to player 0) instead of freezing it in
    ///   state 2 with a null target, and drop stale references so CheckLOS doesn't re-sync a null target.
    /// - DetectNoise: noise made by the hidden player no longer turns the Butler's attention towards it.
    [HarmonyPatch]
    internal static class ButlerEnemyAIUntargetablePatches
    {
        [HarmonyPatch(typeof(ButlerEnemyAI), nameof(ButlerEnemyAI.CheckLOS))]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> CheckLOSTranspiler(IEnumerable<CodeInstruction> instructions) =>
            HiddenPlayerHelpers.HidePlayerFromLoops(instructions);

        [HarmonyPatch(typeof(ButlerEnemyAI), nameof(ButlerEnemyAI.Update))]
        [HarmonyPrefix]
        private static void UpdatePrefix(ButlerEnemyAI __instance)
        {
            if (HiddenPlayerHelpers.IsHiddenFrom(__instance, __instance.watchingPlayer)) __instance.watchingPlayer = null;
            if (HiddenPlayerHelpers.IsHiddenFrom(__instance, __instance.syncedTargetPlayer)) __instance.syncedTargetPlayer = null;
            if (!HiddenPlayerHelpers.IsHiddenFrom(__instance, __instance.targetPlayer)) return;

            if (__instance.IsOwner && __instance.currentBehaviourStateIndex == 2)
            {
                __instance.SwitchToBehaviourState(0);
                __instance.ChangeOwnershipOfEnemy(StartOfRound.Instance.allPlayerScripts[0].actualClientId);
            }
            __instance.targetPlayer = null;
        }

        [HarmonyPatch(typeof(ButlerEnemyAI), nameof(ButlerEnemyAI.DetectNoise))]
        [HarmonyPrefix]
        private static bool DetectNoisePrefix(EnemyAI __instance, Vector3 noisePosition) =>
            !HiddenPlayerHelpers.IsNoiseFromHiddenPlayer(__instance, noisePosition);
    }
}
