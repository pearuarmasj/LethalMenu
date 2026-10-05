using HarmonyLib;
using UnityEngine;

namespace LethalMenu.Patches
{
    /// Untargetable and Anti-Ghost Girl both suppress a haunt aimed at the local player.
    internal static class DressGirlHaunt
    {
        public static bool Suppressed(DressGirlAI girl) =>
            girl.hauntingPlayer != null && girl.hauntingPlayer == LethalMenuMod.LocalPlayer &&
            (Hack.Untargetable.IsEnabled() || Hack.AntiGhostGirl.IsEnabled());
    }

    /// Untargetable vs Ghost Girl. DressGirlAI picks `hauntingPlayer` in ChoosePlayerToHaunt with a seeded weighted
    /// random over isPlayerControlled players (no PlayerIsTargetable), so a hidden local player can still be the
    /// haunted one, and every later step (stare placement, BeginChasing, teleports, the kill in OnCollideWithPlayer)
    /// hangs off that. hauntingPlayer cannot be swapped locally (ownership follows it and every client derives the
    /// same player from the shared seed), so the haunt is made inert instead: no stare position is found, so the
    /// ghost never appears and never starts a chase. The kill itself is closed by
    /// MeetsStandardPlayerCollisionConditions in OnCollideWithPlayer.
    [HarmonyPatch(typeof(DressGirlAI), nameof(DressGirlAI.TryFindingHauntPosition))]
    internal static class DressGirlHauntPositionPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(DressGirlAI __instance, ref Vector3 __result)
        {
            if (!DressGirlHaunt.Suppressed(__instance))
                return true;
            __instance.couldNotStareLastAttempt = true;
            __result = Vector3.zero;
            return false;
        }
    }

    /// DressGirlAI.BeginChasing switches to the chase state, may trip the breaker and sets the haunted player as
    /// the move target. Refuse it while the haunted player is the hidden local one.
    [HarmonyPatch(typeof(DressGirlAI), nameof(DressGirlAI.BeginChasing))]
    internal static class DressGirlBeginChasingPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(DressGirlAI __instance) =>
            !DressGirlHaunt.Suppressed(__instance);
    }

    /// If Untargetable is switched on while the ghost is already staring at or chasing the local player, end it at
    /// once through the AI's own exits (StopChasing / DisappearDuringHaunt).
    [HarmonyPatch(typeof(DressGirlAI), nameof(DressGirlAI.Update))]
    internal static class DressGirlActiveHauntPatch
    {
        [HarmonyPrefix]
        private static void Prefix(DressGirlAI __instance)
        {
            if (!DressGirlHaunt.Suppressed(__instance))
                return;
            if (__instance.currentBehaviourStateIndex == 1)
                __instance.StopChasing();
            else if (__instance.staringInHaunt)
                __instance.DisappearDuringHaunt();
        }
    }
}
