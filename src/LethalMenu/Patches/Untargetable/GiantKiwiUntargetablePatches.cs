using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace LethalMenu.Patches
{
    /// Untargetable vs Giant Kiwi. The Kiwi does not use PlayerIsTargetable or the EnemyAI sight helpers: it
    /// overlap-spheres every collider on the visible-threat mask and keeps whatever IVisibleThreat it finds
    /// (a PlayerControllerB is one) in CheckLOSForCreatures, which AttackIfThreatened, PreoccupiedWithDefensePriorities
    /// and the chase state all use to pick `watchingThreat`, send SyncWatchingThreatServerRpc /
    /// AddToThreatsHoldingEggListServerRpc and start StartAttackingAndSync. The single
    /// `Component.TryGetComponent&lt;IVisibleThreat&gt;` that reads each collider is routed through TryGetThreat, which
    /// reports "no threat" for the hidden local player. GiantKiwiAI.CheckLOSForCreatures is a plain instance method, so the
    /// transpiler is enemy-aware: it pushes `ldarg.0` after the call's operands and TryGetThreat takes the enemy last.
    [HarmonyPatch(typeof(GiantKiwiAI), nameof(GiantKiwiAI.CheckLOSForCreatures))]
    internal static class GiantKiwiThreatScanPatch
    {
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var replacement = AccessTools.Method(typeof(GiantKiwiThreatScanPatch), nameof(TryGetThreat));
            var replaced = 0;
            foreach (var instruction in instructions)
            {
                if ((instruction.opcode == OpCodes.Call || instruction.opcode == OpCodes.Callvirt) &&
                    instruction.operand is MethodInfo method &&
                    method.Name == nameof(Component.TryGetComponent) &&
                    method.IsGenericMethod &&
                    typeof(Component).IsAssignableFrom(method.DeclaringType) &&
                    method.GetGenericArguments()[0] == typeof(IVisibleThreat))
                {
                    var enemy = new CodeInstruction(OpCodes.Ldarg_0).MoveLabelsFrom(instruction);
                    enemy.blocks.AddRange(instruction.blocks);
                    instruction.blocks.Clear();
                    yield return enemy;
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = replacement;
                    replaced++;
                }
                yield return instruction;
            }
            if (replaced == 0)
                throw new InvalidOperationException("GiantKiwiAI.CheckLOSForCreatures: TryGetComponent<IVisibleThreat> not found");
        }

        private static bool TryGetThreat(Component source, out IVisibleThreat threat, EnemyAI enemy)
        {
            if (source.TryGetComponent(out threat) && !UntargetableSightPatches.IsHiddenFrom(enemy, threat as PlayerControllerB))
                return true;
            threat = null!;
            return false;
        }
    }

    /// Other ways the Kiwi acquires the local player without a sight check: ReactToThreatAttack (called from
    /// DoAIInterval with `stunnedByPlayer` and from HitEnemy with `playerWhoHit`) immediately sets
    /// watchingThreat/attackingThreat and runs StartAttackingAndSync; DoAIInterval state 2 re-targets
    /// `lastPlayerWhoAttacked`; SyncWatchingThreatClientRpc / StartAttackingClientRpc write the threat the owner
    /// chose into watchingThreat/attackingThreat. Refuse the react, and each frame drop any hidden-player reference
    /// before the AI interval runs. In the attack state attackingThreat is dereferenced unconditionally
    /// (`attackingThreat.IsThreatDead()`), so a Kiwi that was chasing the hidden player is sent back to state 1,
    /// which then hands ownership back to the host the way losing a threat normally does.
    [HarmonyPatch(typeof(GiantKiwiAI), nameof(GiantKiwiAI.ReactToThreatAttack))]
    internal static class GiantKiwiReactPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(EnemyAI __instance, IVisibleThreat threat) =>
            !UntargetableSightPatches.IsHiddenFrom(__instance, threat as PlayerControllerB);
    }

    [HarmonyPatch(typeof(GiantKiwiAI), nameof(GiantKiwiAI.Update))]
    internal static class GiantKiwiDropThreatPatch
    {
        [HarmonyPrefix]
        private static void Prefix(GiantKiwiAI __instance)
        {
            if (!UntargetableSightPatches.IsHiddenFrom(__instance, LethalMenuMod.LocalPlayer))
                return;

            if (UntargetableSightPatches.IsHiddenFrom(__instance, __instance.stunnedByPlayer))
                __instance.stunnedByPlayer = null;
            if (UntargetableSightPatches.IsHiddenFrom(__instance, __instance.lastPlayerWhoAttacked))
                __instance.lastPlayerWhoAttacked = null;
            if (UntargetableSightPatches.IsHiddenFrom(__instance, __instance.watchingThreat as PlayerControllerB))
                __instance.watchingThreat = null;
            if (UntargetableSightPatches.IsHiddenFrom(__instance, __instance.attackingThreat as PlayerControllerB))
            {
                __instance.attackingThreat = null;
                if (__instance.currentBehaviourStateIndex == 2)
                    __instance.SwitchToBehaviourStateOnLocalClient(1);
            }
        }
    }

    /// GiantKiwiAI.AnimationEventB deals the peck damage to the local player whenever `timeSinceHittingPlayer` is
    /// under 0.1 s, which OnCollideWithPlayer sets after MeetsStandardPlayerCollisionConditions. Close the window
    /// that is left open if Untargetable is switched on right after such a collision.
    [HarmonyPatch(typeof(GiantKiwiAI), nameof(GiantKiwiAI.AnimationEventB))]
    internal static class GiantKiwiPeckDamagePatch
    {
        [HarmonyPrefix]
        private static void Prefix(GiantKiwiAI __instance)
        {
            if (UntargetableSightPatches.IsHiddenFrom(__instance, LethalMenuMod.LocalPlayer) && __instance.timeSinceHittingPlayer < 0.1f)
                __instance.timeSinceHittingPlayer = 0.1f;
        }
    }

    /// GiantKiwiAI.DetectNoise is an override with its own hearing logic (PingAttention, wakes the Kiwi from idle
    /// sleep), so the EnemyPatches prefix on EnemyAI.DetectNoise does not stop it. Same 5 m rule.
    [HarmonyPatch(typeof(GiantKiwiAI), nameof(GiantKiwiAI.DetectNoise))]
    internal static class GiantKiwiDetectNoisePatch
    {
        private const float IgnoreRadius = 5f;

        [HarmonyPrefix]
        private static bool Prefix(EnemyAI __instance, Vector3 noisePosition) =>
            !UntargetableSightPatches.IsHiddenFrom(__instance, LethalMenuMod.LocalPlayer) ||
            Vector3.Distance(noisePosition, LethalMenuMod.LocalPlayer!.transform.position) >= IgnoreRadius;
    }
}
