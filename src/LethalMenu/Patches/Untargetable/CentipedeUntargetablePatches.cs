using GameNetcodeStuff;
using HarmonyLib;
using Unity.Netcode;

namespace LethalMenu.Patches
{
    /// Untargetable vs Snare Flea. CentipedeAI.Update (state 1, hiding on the ceiling) spherecasts straight down and,
    /// when the local player is underneath, calls TriggerCentipedeFallServerRpc(localClientId), which hands the
    /// enemy to this client and drops it (state 2). That check never goes through PlayerIsTargetable. Swallow the RPC
    /// for the hidden local player and re-arm `triggeredFall` so the check keeps running if Untargetable is toggled off.
    [HarmonyPatch(typeof(CentipedeAI), nameof(CentipedeAI.TriggerCentipedeFallServerRpc))]
    internal static class CentipedeCeilingTriggerPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(CentipedeAI __instance, ulong clientId)
        {
            if (!UntargetableSightPatches.IsHiddenFrom(__instance, LethalMenuMod.LocalPlayer) ||
                clientId != NetworkManager.Singleton.LocalClientId)
                return true;
            __instance.triggeredFall = false;
            return false;
        }
    }

    /// CentipedeAI.ClingToPlayer (reached from ClingToPlayerClientRpc, which carries an arbitrary player id)
    /// attaches the Snare Flea to a player, strips their held items and enters state 3. Refuse it for the hidden
    /// local player.
    [HarmonyPatch(typeof(CentipedeAI), nameof(CentipedeAI.ClingToPlayer))]
    internal static class CentipedeClingPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(EnemyAI __instance, PlayerControllerB playerScript) =>
            !UntargetableSightPatches.IsHiddenFrom(__instance, playerScript);
    }

    /// CentipedeAI.DamagePlayerOnIntervals (state 3, only while clinging to the local client) deals 10 suffocation
    /// damage every 2 s. If Untargetable is switched on mid-cling, shake the Snare Flea off through the normal
    /// StopClingingServerRpc path instead of damaging.
    [HarmonyPatch(typeof(CentipedeAI), nameof(CentipedeAI.DamagePlayerOnIntervals))]
    internal static class CentipedeClingDamagePatch
    {
        [HarmonyPrefix]
        private static bool Prefix(CentipedeAI __instance)
        {
            if (!UntargetableSightPatches.IsHiddenFrom(__instance, __instance.clingingToPlayer))
                return true;
            if (!__instance.inDroppingOffPlayerAnim)
            {
                __instance.inDroppingOffPlayerAnim = true;
                __instance.StopClingingServerRpc(playerDead: false);
            }
            return false;
        }
    }
}
