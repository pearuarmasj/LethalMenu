using GameNetcodeStuff;
using HarmonyLib;

namespace LethalMenu.Patches
{
    /// Untargetable vs Crawler. CrawlerAI.Update (state 0) uses `stunnedByPlayer` as the noticed player in place of
    /// CheckLineOfSightForPlayer; when that is the local player it calls BeginChasingPlayerServerRpc and takes
    /// ownership of the enemy. Drop the stun credit while the local player is hidden. The line-of-sight branch is
    /// closed by UntargetableSightPatches, and the bite (OnCollideWithPlayer) by MeetsStandardPlayerCollisionConditions.
    [HarmonyPatch(typeof(CrawlerAI), nameof(CrawlerAI.Update))]
    internal static class CrawlerStunnedByPatch
    {
        [HarmonyPrefix]
        private static void Prefix(CrawlerAI __instance)
        {
            if (UntargetableSightPatches.IsHiddenFrom(__instance, __instance.stunnedByPlayer))
                __instance.stunnedByPlayer = null;
        }
    }

    /// CrawlerAI.CheckForVeryClosePlayer (chase state, DoAIInterval) overlap-spheres layer 8 around the Crawler and
    /// assigns `targetPlayer` to whatever player it finds without PlayerIsTargetable. Keep the previous target
    /// when that player is the hidden local one.
    [HarmonyPatch(typeof(CrawlerAI), nameof(CrawlerAI.CheckForVeryClosePlayer))]
    internal static class CrawlerVeryClosePlayerPatch
    {
        [HarmonyPrefix]
        private static void Prefix(CrawlerAI __instance, out PlayerControllerB? __state) =>
            __state = __instance.targetPlayer;

        [HarmonyPostfix]
        private static void Postfix(CrawlerAI __instance, PlayerControllerB? __state)
        {
            if (UntargetableSightPatches.IsHiddenFrom(__instance, __instance.targetPlayer))
                __instance.targetPlayer = __state;
        }
    }
}
