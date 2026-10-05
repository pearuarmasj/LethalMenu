using HarmonyLib;
using UnityEngine;

namespace LethalMenu.Patches
{
    /// LassoManAI (Crawler-type chaser) paths that skip PlayerIsTargetable.
    /// - OnCollideWithPlayer: never calls MeetsStandardPlayerCollisionConditions; any contact with the local
    ///   player calls DamagePlayer(40, Strangulation).
    /// - BeginChasingPlayerServerRpc: Update state 0 reports the local player as noticed when it is the stunner
    ///   (`stunnedByPlayer` bypasses CheckLineOfSightForPlayer); the report is dropped for the hidden player.
    [HarmonyPatch]
    internal static class LassoManAIUntargetablePatches
    {
        [HarmonyPatch(typeof(LassoManAI), nameof(LassoManAI.OnCollideWithPlayer))]
        [HarmonyPrefix]
        private static bool OnCollideWithPlayerPrefix(Collider other) => !HiddenPlayerHelpers.IsHiddenCollider(other);

        [HarmonyPatch(typeof(LassoManAI), nameof(LassoManAI.BeginChasingPlayerServerRpc))]
        [HarmonyPrefix]
        private static bool BeginChasingPlayerServerRpcPrefix(int playerObjectId) =>
            !HiddenPlayerHelpers.IsHiddenId(playerObjectId);
    }
}
