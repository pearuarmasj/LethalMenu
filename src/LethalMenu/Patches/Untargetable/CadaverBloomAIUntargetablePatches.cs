using HarmonyLib;
using UnityEngine;

namespace LethalMenu.Patches
{
    /// CadaverBloomAI selects targets only through GetAllPlayersInLineOfSightNonAlloc (CheckForVeryClosePlayer,
    /// PlayerIsTargetable) and bites only through OnCollideWithPlayer (MeetsStandardPlayerCollisionConditions).
    /// The open path is DetectNoise: it stops the bloom and turns it towards the noise (PingAttention); noise made
    /// by the hidden player is ignored. BurstForth's infection kill belongs to CadaverGrowthAI's spore system and
    /// is not an attack the bloom directs at a player.
    [HarmonyPatch]
    internal static class CadaverBloomAIUntargetablePatches
    {
        [HarmonyPatch(typeof(CadaverBloomAI), nameof(CadaverBloomAI.DetectNoise))]
        [HarmonyPrefix]
        private static bool DetectNoisePrefix(Vector3 noisePosition) =>
            !HiddenPlayerHelpers.IsNoiseFromHiddenPlayer(noisePosition);
    }
}
