using HarmonyLib;
using UnityEngine;

namespace LethalMenu.Patches
{
    /// Untargetable vs Cave Dweller. Target selection (TargetClosestPlayer in DoAIIntervalChaseLogic, the server
    /// loop in Update, the baby's GetAllPlayersInLineOfSightNonAlloc) all go through PlayerIsTargetable and the
    /// leap kill starts in OnCollideWithPlayer behind MeetsStandardPlayerCollisionConditions, so those are covered.
    /// What is left is the kill animation RPC pair, whose player id is whatever the sender wrote.
    /// When this client is the host, KillPlayerAnimationServerRpc executes requests from other clients; refuse one
    /// that names the hidden local player so the server never enters inKillAnimation for it.
    [HarmonyPatch(typeof(CaveDwellerAI), nameof(CaveDwellerAI.KillPlayerAnimationServerRpc))]
    internal static class CaveDwellerKillRequestPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(int playerObjectId) =>
            !UntargetableSightPatches.IsHidden(StartOfRound.Instance.allPlayerScripts[playerObjectId]);
    }

    /// CaveDwellerAI.KillPlayerAnimationClientRpc calls KillPlayer(Mauling) on the local player whenever the id in
    /// the message is theirs. If a request was already in flight when Untargetable was switched on (or the host
    /// is another client), refuse it on this client; the other clients' copies of the animation are unaffected.
    /// `startingKillAnimationLocalClient`, set by OnCollideWithPlayer before the request, is cleared so the next
    /// collision is not blocked.
    [HarmonyPatch(typeof(CaveDwellerAI), nameof(CaveDwellerAI.KillPlayerAnimationClientRpc))]
    internal static class CaveDwellerKillExecutePatch
    {
        [HarmonyPrefix]
        private static bool Prefix(CaveDwellerAI __instance, int playerObjectId)
        {
            if (!UntargetableSightPatches.IsHidden(StartOfRound.Instance.allPlayerScripts[playerObjectId]))
                return true;
            __instance.startingKillAnimationLocalClient = false;
            return false;
        }
    }

    /// CaveDwellerAI.DetectNoise is an override with its own hearing logic (PingAttention, startles the baby into
    /// crying), so the EnemyPatches prefix on EnemyAI.DetectNoise does not stop it. Same 5 m rule.
    [HarmonyPatch(typeof(CaveDwellerAI), nameof(CaveDwellerAI.DetectNoise))]
    internal static class CaveDwellerDetectNoisePatch
    {
        private const float IgnoreRadius = 5f;

        [HarmonyPrefix]
        private static bool Prefix(Vector3 noisePosition) =>
            !UntargetableSightPatches.IsHidden(LethalMenuMod.LocalPlayer) ||
            Vector3.Distance(noisePosition, LethalMenuMod.LocalPlayer!.transform.position) >= IgnoreRadius;
    }
}
