using GameNetcodeStuff;
using HarmonyLib;

namespace LethalMenu.Patches
{
    /// SandWormAI.OnCollideWithPlayer bypasses MeetsStandardPlayerCollisionConditions: while the worm is emerged,
    /// any contact with the local player calls EatPlayer, which KillPlayer()s it. Targeting itself (GetClosestPlayer,
    /// PlayerIsTargetable) is already covered; the hidden player is not eaten by an emerged worm.
    [HarmonyPatch]
    internal static class SandWormAIUntargetablePatches
    {
        [HarmonyPatch(typeof(SandWormAI), nameof(SandWormAI.EatPlayer))]
        [HarmonyPrefix]
        private static bool EatPlayerPrefix(EnemyAI __instance, PlayerControllerB playerScript) => !HiddenPlayerHelpers.IsHiddenFrom(__instance, playerScript);
    }
}
