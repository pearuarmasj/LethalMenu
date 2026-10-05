using HarmonyLib;
using UnityEngine;

namespace LethalMenu.Patches
{
    /// HoarderBugAI picks players only through GetAllPlayersInLineOfSight (DetectAndLookAtPlayers), which goes
    /// through PlayerIsTargetable, and hits only through OnCollideWithPlayer (MeetsStandardPlayerCollisionConditions),
    /// so the one open path is its noise reaction: DetectNoise turns the bug to the noise and sends a bug that is
    /// idle near its nest back to guard it. Noise made by the hidden player is ignored.
    [HarmonyPatch]
    internal static class HoarderBugAIUntargetablePatches
    {
        [HarmonyPatch(typeof(HoarderBugAI), nameof(HoarderBugAI.DetectNoise))]
        [HarmonyPrefix]
        private static bool DetectNoisePrefix(Vector3 noisePosition) =>
            !HiddenPlayerHelpers.IsNoiseFromHiddenPlayer(noisePosition);
    }
}
