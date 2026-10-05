using GameNetcodeStuff;
using HarmonyLib;

namespace LethalMenu.Patches
{
    /// Untargetable vs Tulip Snake. FlowerSnakeEnemy.SetClingToPlayer is the single place a snake attaches to a
    /// player (server side from FSHitPlayerServerRpc, clients from ClingToPlayerClientRpc, which carries an
    /// arbitrary player id). It bumps enemiesOnPerson/carryWeight and lets the snakes lift the player. Refuse it for
    /// the hidden local player. Detection is already covered: DoAIInterval picks targets through
    /// GetAllPlayersInLineOfSightNonAlloc (PlayerIsTargetable) and OnCollideWithPlayer uses
    /// MeetsStandardPlayerCollisionConditions.
    [HarmonyPatch(typeof(FlowerSnakeEnemy), nameof(FlowerSnakeEnemy.SetClingToPlayer))]
    internal static class FlowerSnakeClingPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(PlayerControllerB playerToCling) =>
            !UntargetableSightPatches.IsHidden(playerToCling);
    }

    /// FlowerSnakeEnemy.MainSnakeActAsConductor (every frame for the lead snake, clingPosition 4) applies the lift
    /// force to the clinging player. If Untargetable is switched on while snakes are already attached, release them
    /// the same way LocalPlayerDamaged does.
    [HarmonyPatch(typeof(FlowerSnakeEnemy), nameof(FlowerSnakeEnemy.MainSnakeActAsConductor))]
    internal static class FlowerSnakeConductorPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(FlowerSnakeEnemy __instance)
        {
            if (!UntargetableSightPatches.IsHidden(__instance.clingingToPlayer))
                return true;
            __instance.StopClingingOnLocalClient(isMainSnake: true);
            __instance.StopClingingServerRpc((int)LethalMenuMod.LocalPlayer!.playerClientId);
            return false;
        }
    }
}
