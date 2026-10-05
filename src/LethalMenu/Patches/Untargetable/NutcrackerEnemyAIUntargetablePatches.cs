using HarmonyLib;

namespace LethalMenu.Patches
{
    /// NutcrackerEnemyAI paths left after UntargetableSightPatches (CheckLineOfSightForLocalPlayer, Update sight
    /// reads) and NutcrackerMovingPatch (IsLocalPlayerMoving). The aim/fire chain only starts from the target's own
    /// client (Update state 2 -> AimGunServerRpc behind CheckLineOfSightForLocalPlayer), so it stays closed.
    /// - SeeMovingThreatServerRpc: HitEnemy reports the attacker with enterAttackFromPatrolMode, which makes every
    ///   Nutcracker switch target to the player that hit it regardless of sight; skipped for the hidden player.
    /// - LegKickPlayer: the kick is applied to allPlayerScripts[playerId] by LegKickPlayerClientRpc on every
    ///   client; the hidden player is never kicked (KillPlayer) even if another client sends its id.
    [HarmonyPatch]
    internal static class NutcrackerEnemyAIUntargetablePatches
    {
        [HarmonyPatch(typeof(NutcrackerEnemyAI), nameof(NutcrackerEnemyAI.SeeMovingThreatServerRpc))]
        [HarmonyPrefix]
        private static bool SeeMovingThreatServerRpcPrefix(EnemyAI __instance, int playerId) => !HiddenPlayerHelpers.IsHiddenId(__instance, playerId);

        [HarmonyPatch(typeof(NutcrackerEnemyAI), nameof(NutcrackerEnemyAI.LegKickPlayer))]
        [HarmonyPrefix]
        private static bool LegKickPlayerPrefix(EnemyAI __instance, int playerId) => !HiddenPlayerHelpers.IsHiddenId(__instance, playerId);
    }
}
