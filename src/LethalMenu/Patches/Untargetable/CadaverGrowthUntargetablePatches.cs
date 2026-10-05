using HarmonyLib;

namespace LethalMenu.Patches
{
    /// Spore infection source 1: CadaverGrowthAI.InfectPlayers (every InfectIntervalTime, from Update)
    /// rolls an infection for the local player standing in a growth tile or near an infected player,
    /// then calls InfectPlayer + InfectPlayerRpc. It never touches any other player, so it is skipped
    /// while hidden (this also suppresses its "HEALTH RISK" HUD text and immunity timers).
    [HarmonyPatch(typeof(CadaverGrowthAI), nameof(CadaverGrowthAI.InfectPlayers))]
    internal static class CadaverInfectPlayersPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(EnemyAI __instance) =>
            !UntargetableSightPatches.IsHiddenFrom(__instance, GameNetworkManager.Instance.localPlayerController);
    }

    /// Spore infection source 2: CadaverGrowthAI.CoughSporesRpc (another infected player coughing in
    /// line of sight) rolls InfectPlayer(local) + InfectPlayerRpc(local) on the receiving client.
    /// InfectPlayer is the single place that flips playerInfections[id].infected, and InfectPlayerRpc
    /// is how other clients (and the host, which then spawns a standby Bloom) learn about it, so both
    /// refuse the hidden player.
    [HarmonyPatch(typeof(CadaverGrowthAI), nameof(CadaverGrowthAI.InfectPlayer))]
    internal static class CadaverInfectPlayerPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(EnemyAI __instance, GameNetcodeStuff.PlayerControllerB playerScript) =>
            !UntargetableSightPatches.IsHiddenFrom(__instance, playerScript);
    }

    [HarmonyPatch(typeof(CadaverGrowthAI), nameof(CadaverGrowthAI.InfectPlayerRpc))]
    internal static class CadaverInfectPlayerRpcPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(EnemyAI __instance, int playerId) =>
            !UntargetableSightPatches.IsHiddenFrom(__instance, StartOfRound.Instance.allPlayerScripts[playerId]);
    }

    /// Already infected: a player infected before Untargetable was enabled would still advance
    /// infectionMeter/burstMeter in ProgressPlayerInfections and finish in BurstFromPlayer, which
    /// kills the local player. The infection is cured the same way HealInfection cures it
    /// (CurePlayer + CurePlayerRpc), plus the HUD/ear-ringing state the burst phase drives.
    [HarmonyPatch(typeof(CadaverGrowthAI), nameof(CadaverGrowthAI.ProgressPlayerInfections))]
    internal static class CadaverProgressInfectionPatch
    {
        [HarmonyPrefix]
        private static void Prefix(CadaverGrowthAI __instance)
        {
            var local = GameNetworkManager.Instance.localPlayerController;
            if (!UntargetableSightPatches.IsHiddenFrom(__instance, local)) return;

            int id = (int)local.playerClientId;
            if (!__instance.playerInfections[id].infected) return;

            __instance.numberOfInfected--;
            __instance.CurePlayer(id);
            __instance.CurePlayerRpc(id);
            HUDManager.Instance.cadaverFilter = 0f;
            SoundManager.Instance.alternateEarsRinging = false;
        }
    }
}
