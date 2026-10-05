using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace LethalMenu.Patches
{
    /// Untargetable vs Puma. PumaAI overrides PlayerIsTargetable without calling the base implementation, so the
    /// EnemyPatches postfix on EnemyAI.PlayerIsTargetable never runs for it. Everything that asks the Puma whether a
    /// player is a valid prey (TargetClosestPlayer in state 0, TargetClosestPlayerSkiddish in states 0/1, the
    /// state 2 re-check, RunTreeMode, and the scratch in OnCollideWithPlayer via
    /// MeetsStandardPlayerCollisionConditions) goes through this override, so forcing it false for the hidden local
    /// player closes target selection, the stalk and the attack.
    [HarmonyPatch(typeof(PumaAI), nameof(PumaAI.PlayerIsTargetable))]
    internal static class PumaPlayerIsTargetablePatch
    {
        [HarmonyPostfix]
        private static void Postfix(ref bool __result, PlayerControllerB playerScript)
        {
            if (UntargetableSightPatches.IsHidden(playerScript))
                __result = false;
        }
    }

    /// PumaAI.UpdatePlayerKnowledge (every frame) runs its own sight model over every controlled player:
    /// seenByPuma / seenByPumaThroughTrees (Puma sees the player), timeSinceSeeingPuma / seeAngle (player sees
    /// the Puma). Those feed stalk freezing, hiding, tree choice and the startled reaction without touching
    /// PlayerIsTargetable. After each update, blank the hidden local player's entry exactly as the method does
    /// for a player who is not controlled, and take it back out of playersSeeingPuma.
    [HarmonyPatch(typeof(PumaAI), nameof(PumaAI.UpdatePlayerKnowledge))]
    internal static class PumaPlayerKnowledgePatch
    {
        [HarmonyPostfix]
        private static void Postfix(PumaAI __instance)
        {
            foreach (var knowledge in __instance.playerKnowledge)
            {
                if (!UntargetableSightPatches.IsHidden(knowledge.playerScript))
                    continue;

                if (knowledge.playerScript.isPlayerControlled && !knowledge.playerScript.isInsideFactory &&
                    knowledge.timeSinceSeeingPuma < 1f)
                    __instance.playersSeeingPuma = Mathf.Max(0, __instance.playersSeeingPuma - 1);

                knowledge.seenByPuma = false;
                knowledge.seenByPumaThroughTrees = false;
                knowledge.timeSinceSeeingPuma = 1000f;
                knowledge.timeSincePumaSeeing = 1000f;
                knowledge.seeAngle = -361f;
            }
        }
    }

    /// PumaAI.IsPlayerThreatening (called from UpdatePlayerKnowledge) decides whether a player the Puma sees is
    /// armed or close enough to startle it into pinging attention (PingPumaAttentionRpc) and running. The hidden
    /// local player is never a threat to it.
    [HarmonyPatch(typeof(PumaAI), nameof(PumaAI.IsPlayerThreatening))]
    internal static class PumaThreateningPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(PlayerControllerB playerScript, ref bool __result)
        {
            if (!UntargetableSightPatches.IsHidden(playerScript))
                return true;
            __result = false;
            return false;
        }
    }

    /// PumaAI.DetectNoise is an override that runs its own hearing logic (PingAttention, startled turn) after the
    /// base call, so the EnemyPatches prefix on EnemyAI.DetectNoise does not stop it. Same 5 m rule.
    [HarmonyPatch(typeof(PumaAI), nameof(PumaAI.DetectNoise))]
    internal static class PumaDetectNoisePatch
    {
        private const float IgnoreRadius = 5f;

        [HarmonyPrefix]
        private static bool Prefix(Vector3 noisePosition) =>
            !UntargetableSightPatches.IsHidden(LethalMenuMod.LocalPlayer) ||
            Vector3.Distance(noisePosition, LethalMenuMod.LocalPlayer!.transform.position) >= IgnoreRadius;
    }
}
