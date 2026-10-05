using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace LethalMenu.Patches
{
    /// Forest giant grab: ForestGiantAI.OnCollideWithPlayer sends BeginChasingNewPlayerServerRpc /
    /// GrabPlayerServerRpc(local id) behind MeetsStandardPlayerCollisionConditions. Skipping the
    /// handler for the hidden player keeps the grab independent of PlayerIsTargetable.
    [HarmonyPatch(typeof(ForestGiantAI), nameof(ForestGiantAI.OnCollideWithPlayer))]
    internal static class ForestGiantCollisionPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(Collider other) =>
            !UntargetableSightPatches.IsHidden(other.gameObject.GetComponent<PlayerControllerB>());
    }

    /// Eat sequence: GrabPlayerClientRpc runs BeginEatPlayer, whose coroutine ends in
    /// KillPlayer(Crushing). GrabPlayerServerRpc is RequireOwnership = false and runs on the host for
    /// any sender. Requests naming the hidden player are dropped on both ends.
    [HarmonyPatch(typeof(ForestGiantAI), nameof(ForestGiantAI.GrabPlayerServerRpc))]
    internal static class ForestGiantGrabServerPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(int playerId) =>
            !UntargetableSightPatches.IsHidden(StartOfRound.Instance.allPlayerScripts[playerId]);
    }

    [HarmonyPatch(typeof(ForestGiantAI), nameof(ForestGiantAI.GrabPlayerClientRpc))]
    internal static class ForestGiantGrabClientPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(int playerId) =>
            !UntargetableSightPatches.IsHidden(StartOfRound.Instance.allPlayerScripts[playerId]);
    }

    /// Chase target: chasingPlayer is only ever assigned by BeginChasingNewPlayerClientRpc (owner
    /// broadcast, also sent for stunnedByPlayer in LookForPlayers/Update) and
    /// FindAndTargetNewPlayerOnLocalClient (LookForPlayers, and Update for stunnedByPlayer). LookForPlayers'
    /// own candidates come from GetAllPlayersInLineOfSightNonAlloc, which already goes through
    /// PlayerIsTargetable, but stunnedByPlayer bypasses it. All three entry points drop the hidden player.
    [HarmonyPatch(typeof(ForestGiantAI), nameof(ForestGiantAI.FindAndTargetNewPlayerOnLocalClient))]
    internal static class ForestGiantFindTargetPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(PlayerControllerB newPlayer) =>
            !UntargetableSightPatches.IsHidden(newPlayer);
    }

    [HarmonyPatch(typeof(ForestGiantAI), nameof(ForestGiantAI.BeginChasingNewPlayerServerRpc))]
    internal static class ForestGiantBeginChasingServerPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(int playerId) =>
            !UntargetableSightPatches.IsHidden(StartOfRound.Instance.allPlayerScripts[playerId]);
    }

    [HarmonyPatch(typeof(ForestGiantAI), nameof(ForestGiantAI.BeginChasingNewPlayerClientRpc))]
    internal static class ForestGiantBeginChasingClientPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(int playerId) =>
            !UntargetableSightPatches.IsHidden(StartOfRound.Instance.allPlayerScripts[playerId]);
    }

    /// Stale state: a giant already chasing the player when Untargetable is enabled keeps
    /// chasingPlayer (state 1 DoAIInterval calls SetMovingTowardsTargetPlayer(chasingPlayer), Update
    /// reaches for and looks at it) and one stunned by the player keeps stunnedByPlayer. The owner
    /// drops back to roaming; stunnedByPlayer is cleared.
    [HarmonyPatch(typeof(ForestGiantAI), nameof(ForestGiantAI.Update))]
    internal static class ForestGiantStaleTargetPatch
    {
        [HarmonyPrefix]
        private static void Prefix(ForestGiantAI __instance)
        {
            if (UntargetableSightPatches.IsHidden(__instance.stunnedByPlayer))
                __instance.stunnedByPlayer = null;
            if (__instance.IsOwner && __instance.currentBehaviourStateIndex == 1 &&
                UntargetableSightPatches.IsHidden(__instance.chasingPlayer))
                __instance.SwitchToBehaviourState(0);
        }
    }

    /// Death crush: ForestGiantAI.AnimationEventA (animation event of the falling corpse) sphere-casts
    /// deathFallPosition and calls KillPlayer(Gravity) on the local player. The base method is empty,
    /// so skipping it only removes that kill.
    [HarmonyPatch(typeof(ForestGiantAI), nameof(ForestGiantAI.AnimationEventA))]
    internal static class ForestGiantDeathCrushPatch
    {
        [HarmonyPrefix]
        private static bool Prefix() =>
            !UntargetableSightPatches.IsHidden(GameNetworkManager.Instance.localPlayerController);
    }
}
