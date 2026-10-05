using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using GameNetcodeStuff;
using HarmonyLib;
using LethalMenu.Cheats.Directives;
using UnityEngine;

namespace LethalMenu.Patches
{
    /// Shared building blocks for the per-enemy Untargetable patches. Everything keys off
    /// UntargetableSightPatches.IsHidden / IsHiddenFrom so every patch agrees on who is hidden.
    /// Helpers called from an enemy instance context take the enemy (`...From`) so directed enemies hide the
    /// local player too; the enemy-less forms remain for non-enemy targets (SpikeRoofTrap, SandSpiderWebTrap,
    /// PlayerControllerB).
    ///
    /// The transpilers rewrite player-detection reads inside enemy methods that loop over
    /// StartOfRound.allPlayerScripts or run physics sphere queries without going through
    /// EnemyAI.PlayerIsTargetable. Each rewrite swaps one instruction for a static helper with the same
    /// stack shape, so labels and exception blocks stay attached.
    internal static class HiddenPlayerHelpers
    {
        /// Noise radius around the local player inside which a noise counts as made by the local player.
        public const float OwnNoiseRadius = 5f;

        public static bool IsHidden(PlayerControllerB? player) => UntargetableSightPatches.IsHidden(player);

        public static bool IsHiddenFrom(EnemyAI? enemy, PlayerControllerB? player) =>
            UntargetableSightPatches.IsHiddenFrom(enemy, player);

        public static bool LocalPlayerHiddenFrom(EnemyAI enemy) =>
            UntargetableSightPatches.IsHiddenFrom(enemy, LethalMenuMod.LocalPlayer);

        /// True when `playerId` indexes the player hidden from `enemy` in StartOfRound.allPlayerScripts.
        public static bool IsHiddenId(EnemyAI enemy, int playerId)
        {
            var players = StartOfRound.Instance?.allPlayerScripts;
            return players != null && playerId >= 0 && playerId < players.Length && IsHiddenFrom(enemy, players[playerId]);
        }

        /// True when `collider` belongs to the player hidden from `enemy`.
        public static bool IsHiddenCollider(EnemyAI enemy, Collider? collider) =>
            collider != null && IsHiddenFrom(enemy, collider.GetComponentInParent<PlayerControllerB>());

        /// True when `collider` belongs to the hidden local player (non-enemy callers).
        public static bool IsHiddenCollider(Collider? collider) =>
            collider != null && Hack.Untargetable.IsEnabled() && IsHidden(collider.GetComponentInParent<PlayerControllerB>());

        /// True when a noise at `noisePosition` is close enough to the local player hidden from `enemy` to be its own.
        public static bool IsNoiseFromHiddenPlayer(EnemyAI enemy, Vector3 noisePosition, float radius = OwnNoiseRadius)
        {
            var local = LethalMenuMod.LocalPlayer;
            return local != null && IsHiddenFrom(enemy, local) &&
                   Vector3.Distance(noisePosition, local.transform.position) < radius;
        }

        // ---- static helpers the transpilers call ----
        // Enemy-aware forms take the enemy last (the transpiler pushes `ldarg.0` after the original operands).

        public static bool IsControlledFrom(PlayerControllerB player, EnemyAI enemy) =>
            player.isPlayerControlled && !IsHiddenFrom(enemy, player);

        public static PlayerControllerB? NullIfHidden(PlayerControllerB? player) =>
            IsHidden(player) ? null : player;

        public static PlayerControllerB? NullIfHiddenFrom(PlayerControllerB? player, EnemyAI enemy) =>
            IsHiddenFrom(enemy, player) ? null : player;

        public static bool IsGroundedFrom(CharacterController controller, EnemyAI enemy)
        {
            if (!controller.isGrounded) return false;
            var local = LethalMenuMod.LocalPlayer;
            return !(local != null && IsHiddenFrom(enemy, local) && controller == local.thisController);
        }

        public static bool CheckSphere(Vector3 position, float radius, int layerMask, QueryTriggerInteraction query)
        {
            if (!Hack.Untargetable.IsEnabled()) return Physics.CheckSphere(position, radius, layerMask, query);
            return Physics.OverlapSphere(position, radius, layerMask, query).Any(c => !IsHiddenCollider(c));
        }

        public static bool CheckSphereFrom(Vector3 position, float radius, int layerMask, QueryTriggerInteraction query, EnemyAI enemy)
        {
            if (!LocalPlayerHiddenFrom(enemy)) return Physics.CheckSphere(position, radius, layerMask, query);
            return Physics.OverlapSphere(position, radius, layerMask, query).Any(c => !IsHiddenCollider(enemy, c));
        }

        public static Collider[] OverlapSphereFrom(Vector3 position, float radius, int layerMask, QueryTriggerInteraction query, EnemyAI enemy)
        {
            var hits = Physics.OverlapSphere(position, radius, layerMask, query);
            if (!LocalPlayerHiddenFrom(enemy)) return hits;
            return hits.Where(c => !IsHiddenCollider(enemy, c)).ToArray();
        }

        // ---- transpilers ----
        // Enemy-aware transpilers (HidePlayerFromLoops, HidePlayerGrounded, HidePlayerFromOverlapSphere) are only
        // valid on plain instance methods declared on an EnemyAI-derived type (not a coroutine MoveNext, static, or
        // non-enemy type): `ldarg.0` is then the enemy. HidePlayerFromComponentLookups / HidePlayerFromCheckSphere
        // take `forEnemy`; false keeps the enemy-less helper for non-enemy targets (SpikeRoofTrap).

        /// Pushes `ldarg.0` in front of `instruction`, taking over its labels and exception blocks so jumps and
        /// try/catch boundaries still land on the start of the rewritten sequence.
        private static CodeInstruction PushEnemyBefore(CodeInstruction instruction)
        {
            var push = new CodeInstruction(OpCodes.Ldarg_0).MoveLabelsFrom(instruction);
            push.blocks.AddRange(instruction.blocks);
            instruction.blocks.Clear();
            return push;
        }

        /// Enemy-aware. `player.isPlayerControlled` -> `ldarg.0; IsControlledFrom(player, enemy)`. The hidden player
        /// stops counting as a live player in allPlayerScripts loops that only filter on isPlayerControlled.
        public static IEnumerable<CodeInstruction> HidePlayerFromLoops(IEnumerable<CodeInstruction> instructions)
        {
            var field = AccessTools.Field(typeof(PlayerControllerB), nameof(PlayerControllerB.isPlayerControlled));
            var helper = AccessTools.Method(typeof(HiddenPlayerHelpers), nameof(IsControlledFrom));
            foreach (var instruction in instructions)
            {
                if (instruction.LoadsField(field))
                {
                    yield return PushEnemyBefore(instruction);
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = helper;
                }
                yield return instruction;
            }
        }

        /// `x.GetComponent<PlayerControllerB>()` -> NullIfHidden(...) (`forEnemy` false) or
        /// `ldarg.0; NullIfHiddenFrom(..., enemy)` (`forEnemy` true). Physics-hit and trigger handlers
        /// that resolve the player from a collider then see no player for the hidden local player.
        public static IEnumerable<CodeInstruction> HidePlayerFromComponentLookups(IEnumerable<CodeInstruction> instructions, bool forEnemy)
        {
            var helper = AccessTools.Method(typeof(HiddenPlayerHelpers), forEnemy ? nameof(NullIfHiddenFrom) : nameof(NullIfHidden));
            foreach (var instruction in instructions)
            {
                yield return instruction;
                if (instruction.operand is MethodInfo method && method.Name == nameof(Component.GetComponent) &&
                    method.IsGenericMethod && method.GetParameters().Length == 0 &&
                    method.GetGenericArguments()[0] == typeof(PlayerControllerB))
                {
                    if (forEnemy) yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, helper);
                }
            }
        }

        /// Enemy-aware. `CharacterController.isGrounded` -> `ldarg.0; IsGroundedFrom(controller, enemy)`: the hidden
        /// local player is never grounded for proximity checks that read it.
        public static IEnumerable<CodeInstruction> HidePlayerGrounded(IEnumerable<CodeInstruction> instructions)
        {
            var getter = AccessTools.PropertyGetter(typeof(CharacterController), nameof(CharacterController.isGrounded));
            var helper = AccessTools.Method(typeof(HiddenPlayerHelpers), nameof(IsGroundedFrom));
            foreach (var instruction in instructions)
            {
                if (instruction.Calls(getter))
                {
                    yield return PushEnemyBefore(instruction);
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = helper;
                }
                yield return instruction;
            }
        }

        /// `Physics.CheckSphere(pos, r, mask, query)` -> CheckSphere(...) (`forEnemy` false) or
        /// `CheckSphereFrom(..., ldarg.0)` (`forEnemy` true) ignoring the hidden local player.
        public static IEnumerable<CodeInstruction> HidePlayerFromCheckSphere(IEnumerable<CodeInstruction> instructions, bool forEnemy) =>
            SwapPhysicsCall(instructions, nameof(Physics.CheckSphere), forEnemy ? nameof(CheckSphereFrom) : nameof(CheckSphere), forEnemy);

        /// Enemy-aware. `Physics.OverlapSphere(pos, r, mask, query)` -> `OverlapSphereFrom(..., ldarg.0)` without the
        /// hidden local player.
        public static IEnumerable<CodeInstruction> HidePlayerFromOverlapSphere(IEnumerable<CodeInstruction> instructions) =>
            SwapPhysicsCall(instructions, nameof(Physics.OverlapSphere), nameof(OverlapSphereFrom), true);

        private static IEnumerable<CodeInstruction> SwapPhysicsCall(
            IEnumerable<CodeInstruction> instructions, string physicsMethod, string helperMethod, bool forEnemy)
        {
            var args = new[] { typeof(Vector3), typeof(float), typeof(int), typeof(QueryTriggerInteraction) };
            var original = AccessTools.Method(typeof(Physics), physicsMethod, args);
            var helperArgs = forEnemy ? args.Append(typeof(EnemyAI)).ToArray() : args;
            var helper = AccessTools.Method(typeof(HiddenPlayerHelpers), helperMethod, helperArgs);
            foreach (var instruction in instructions)
            {
                if (instruction.Calls(original))
                {
                    if (forEnemy) yield return PushEnemyBefore(instruction);
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = helper;
                }
                yield return instruction;
            }
        }
    }
}
