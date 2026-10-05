using System.Collections.Generic;
using HarmonyLib;

namespace LethalMenu.Patches
{
    /// DocileLocustBeesAI scatters (state 1, plus an audible noise on switching) when
    /// `Physics.CheckSphere(position, 8, 520, Collide)` finds a player collider and regroups when none is within
    /// 14 m. The hidden player's colliders no longer count, so the swarm doesn't notice it.
    [HarmonyPatch]
    internal static class DocileLocustBeesAIUntargetablePatches
    {
        [HarmonyPatch(typeof(DocileLocustBeesAI), nameof(DocileLocustBeesAI.DoAIInterval))]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> DoAIIntervalTranspiler(IEnumerable<CodeInstruction> instructions) =>
            HiddenPlayerHelpers.HidePlayerFromCheckSphere(instructions);
    }
}
