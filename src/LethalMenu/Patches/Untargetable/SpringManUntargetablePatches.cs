using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace LethalMenu.Patches
{
    /// Coil-head contact kill: SpringManAI.OnCollideWithPlayer calls DamagePlayer(90) on the local
    /// player after MeetsStandardPlayerCollisionConditions. Skipping the whole handler for the hidden
    /// player keeps the kill independent of PlayerIsTargetable.
    /// Detection (DoAIInterval, Update's stop-and-go check, TargetClosestPlayer) already goes through
    /// PlayerIsTargetable, so the hidden player neither draws aggro nor freezes the coil-head.
    [HarmonyPatch(typeof(SpringManAI), nameof(SpringManAI.OnCollideWithPlayer))]
    internal static class SpringManCollisionPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(Collider other) =>
            !UntargetableSightPatches.IsHidden(other.gameObject.GetComponent<PlayerControllerB>());
    }
}
