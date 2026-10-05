using System.Collections.Generic;
using HarmonyLib;

namespace LethalMenu.Patches
{
    /// JesterAI paths that skip PlayerIsTargetable.
    /// - Update (popped-out state): `noPlayersToChaseTimer` is only decremented when no player in allPlayerScripts is
    ///   controlled and inside the factory; the hidden player no longer keeps the Jester out of its box.
    /// - KillPlayerClientRpc: starts killPlayerAnimation, which calls KillPlayer on allPlayerScripts[playerId] on
    ///   every client; the hidden player is never killed through it even if another client sends its id.
    [HarmonyPatch]
    internal static class JesterAIUntargetablePatches
    {
        [HarmonyPatch(typeof(JesterAI), nameof(JesterAI.Update))]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> UpdateTranspiler(IEnumerable<CodeInstruction> instructions) =>
            HiddenPlayerHelpers.HidePlayerFromLoops(instructions);

        [HarmonyPatch(typeof(JesterAI), nameof(JesterAI.KillPlayerClientRpc))]
        [HarmonyPrefix]
        private static bool KillPlayerClientRpcPrefix(int playerId) => !HiddenPlayerHelpers.IsHiddenId(playerId);
    }
}
