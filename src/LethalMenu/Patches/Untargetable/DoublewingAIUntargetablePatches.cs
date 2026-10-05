using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace LethalMenu.Patches
{
    /// DoublewingAI (Manticoil) reactions that skip PlayerIsTargetable.
    /// - DetectNoise: noise from the hidden player no longer alerts the bird (AlertBirdServerRpc).
    /// - TryLanding: `Physics.CheckSphere(hit.point, 16, playersMask)` keeps the bird from landing near any player;
    ///   the hidden player's colliders no longer count.
    [HarmonyPatch]
    internal static class DoublewingAIUntargetablePatches
    {
        [HarmonyPatch(typeof(DoublewingAI), nameof(DoublewingAI.DetectNoise))]
        [HarmonyPrefix]
        private static bool DetectNoisePrefix(Vector3 noisePosition) =>
            !HiddenPlayerHelpers.IsNoiseFromHiddenPlayer(noisePosition);

        [HarmonyPatch(typeof(DoublewingAI), nameof(DoublewingAI.TryLanding))]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> TryLandingTranspiler(IEnumerable<CodeInstruction> instructions) =>
            HiddenPlayerHelpers.HidePlayerFromCheckSphere(instructions);
    }
}
