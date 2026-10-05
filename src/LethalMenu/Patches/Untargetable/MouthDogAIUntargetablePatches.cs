using GameNetcodeStuff;
using HarmonyLib;

namespace LethalMenu.Patches
{
    /// MouthDogAI paths left after EnemyPatches (DetectNoise within 8 m of the local player, EnterLunge).
    /// - HitEnemy: the owner enrages towards `playerWhoHit.transform.position` (EnrageDogOnLocalClient, i.e. a
    ///   chase to the hidden player's position); the hit still lands but the attacker is dropped.
    /// - Update: the same enrage fires towards `stunnedByPlayer` while the dog is stunned.
    /// - ChaseLocalPlayer: OnCollideWithPlayer's "bumped the local player" chase, which sets the destination to
    ///   the local player's position and takes ownership.
    /// - KillPlayerClientRpc: starts KillPlayer(allPlayerScripts[playerId]) on every client; the hidden player is
    ///   never killed through it even if another client sends its id.
    [HarmonyPatch]
    internal static class MouthDogAIUntargetablePatches
    {
        [HarmonyPatch(typeof(MouthDogAI), nameof(MouthDogAI.HitEnemy))]
        [HarmonyPrefix]
        private static void HitEnemyPrefix(ref PlayerControllerB playerWhoHit)
        {
            if (HiddenPlayerHelpers.IsHidden(playerWhoHit)) playerWhoHit = null!;
        }

        [HarmonyPatch(typeof(MouthDogAI), nameof(MouthDogAI.Update))]
        [HarmonyPrefix]
        private static void UpdatePrefix(MouthDogAI __instance)
        {
            if (HiddenPlayerHelpers.IsHidden(__instance.stunnedByPlayer)) __instance.stunnedByPlayer = null;
        }

        [HarmonyPatch(typeof(MouthDogAI), nameof(MouthDogAI.ChaseLocalPlayer))]
        [HarmonyPrefix]
        private static bool ChaseLocalPlayerPrefix() => !HiddenPlayerHelpers.LocalPlayerHidden;

        [HarmonyPatch(typeof(MouthDogAI), nameof(MouthDogAI.KillPlayerClientRpc))]
        [HarmonyPrefix]
        private static bool KillPlayerClientRpcPrefix(int playerId) => !HiddenPlayerHelpers.IsHiddenId(playerId);
    }
}
