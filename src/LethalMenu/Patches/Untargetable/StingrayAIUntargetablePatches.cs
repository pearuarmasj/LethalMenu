using System.Collections.Generic;
using HarmonyLib;

namespace LethalMenu.Patches
{
    /// StingrayAI paths that skip PlayerIsTargetable.
    /// - Update (cloaked state): uncloaks and lunges when the local player is within 1.5 m
    ///   (`thisController.isGrounded` + Distance + Linecast on localPlayerController) and keeps a watch timer
    ///   running from an allPlayerScripts loop that only filters on isPlayerControlled; both skip the hidden player.
    /// - DoAIInterval (cloaked state): the same isPlayerControlled loop resets timeSpentInState near any player.
    /// - SpitOnLocalPlayer / ShowSpitOnPlayerServerRpc: the lunge's OverlapCapsule check finds the local player
    ///   directly and spits on it (helmet slime, poison audio); suppressed for the hidden player.
    [HarmonyPatch]
    internal static class StingrayAIUntargetablePatches
    {
        [HarmonyPatch(typeof(StingrayAI), nameof(StingrayAI.Update))]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> UpdateTranspiler(IEnumerable<CodeInstruction> instructions) =>
            HiddenPlayerHelpers.HidePlayerGrounded(HiddenPlayerHelpers.HidePlayerFromLoops(instructions));

        [HarmonyPatch(typeof(StingrayAI), nameof(StingrayAI.DoAIInterval))]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> DoAIIntervalTranspiler(IEnumerable<CodeInstruction> instructions) =>
            HiddenPlayerHelpers.HidePlayerFromLoops(instructions);

        [HarmonyPatch(typeof(StingrayAI), nameof(StingrayAI.SpitOnLocalPlayer))]
        [HarmonyPrefix]
        private static bool SpitOnLocalPlayerPrefix() => !HiddenPlayerHelpers.LocalPlayerHidden;

        [HarmonyPatch(typeof(StingrayAI), nameof(StingrayAI.ShowSpitOnPlayerServerRpc))]
        [HarmonyPrefix]
        private static bool ShowSpitOnPlayerServerRpcPrefix(int playerId) => !HiddenPlayerHelpers.IsHiddenId(playerId);
    }
}
