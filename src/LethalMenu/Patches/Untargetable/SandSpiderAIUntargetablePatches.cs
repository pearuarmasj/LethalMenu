using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace LethalMenu.Patches
{
    /// SandSpiderAI chase triggers that skip PlayerIsTargetable.
    /// - TriggerChaseWithPlayer: called from PlayerTripWebClientRpc (web trap), HitEnemy (spider hit by the
    ///   local player) and BreakWebServerRpc; it sets targetPlayer and switches to the chase state directly.
    /// - SandSpiderWebTrap.OnTriggerStay: walking into a web slows the local player
    ///   (isMovementHindered++), reports PlayerTripWebServerRpc and makes the spider chase; the hidden player
    ///   passes through webs.
    [HarmonyPatch]
    internal static class SandSpiderAIUntargetablePatches
    {
        [HarmonyPatch(typeof(SandSpiderAI), nameof(SandSpiderAI.TriggerChaseWithPlayer))]
        [HarmonyPrefix]
        private static bool TriggerChaseWithPlayerPrefix(EnemyAI __instance, PlayerControllerB playerScript) =>
            !HiddenPlayerHelpers.IsHiddenFrom(__instance, playerScript);

        [HarmonyPatch(typeof(SandSpiderWebTrap), nameof(SandSpiderWebTrap.OnTriggerStay))]
        [HarmonyPrefix]
        private static bool WebTrapOnTriggerStayPrefix(Collider other) => !HiddenPlayerHelpers.IsHiddenCollider(other);
    }
}
