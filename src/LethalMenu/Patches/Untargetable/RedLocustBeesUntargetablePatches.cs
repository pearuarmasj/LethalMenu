using System.Collections.Generic;
using HarmonyLib;

namespace LethalMenu.Patches
{
    /// RedLocustBees paths that skip PlayerIsTargetable.
    /// - DoAIInterval (hive-guard state): Physics.OverlapSphere over playersMask around the hive picks the
    ///   defence target, sets it, and moves bee ownership to it; the hidden player's colliders are dropped from
    ///   the result.
    /// - BeeKillPlayerOnLocalClient: BeeKillPlayerClientRpc kills allPlayerScripts[playerId] on every client;
    ///   the hidden player is never killed through it.
    [HarmonyPatch]
    internal static class RedLocustBeesUntargetablePatches
    {
        [HarmonyPatch(typeof(RedLocustBees), nameof(RedLocustBees.DoAIInterval))]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> DoAIIntervalTranspiler(IEnumerable<CodeInstruction> instructions) =>
            HiddenPlayerHelpers.HidePlayerFromOverlapSphere(instructions);

        [HarmonyPatch(typeof(RedLocustBees), nameof(RedLocustBees.BeeKillPlayerOnLocalClient))]
        [HarmonyPrefix]
        private static bool BeeKillPlayerOnLocalClientPrefix(int playerId) => !HiddenPlayerHelpers.IsHiddenId(playerId);
    }
}
