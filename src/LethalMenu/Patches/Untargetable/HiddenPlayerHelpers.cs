using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace LethalMenu.Patches
{
    /// Shared building blocks for the per-enemy Untargetable patches. Everything keys off
    /// UntargetableSightPatches.IsHidden so every patch agrees on who is hidden.
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

        public static bool LocalPlayerHidden => UntargetableSightPatches.IsHidden(LethalMenuMod.LocalPlayer);

        /// True when `playerId` indexes the hidden local player in StartOfRound.allPlayerScripts.
        public static bool IsHiddenId(int playerId)
        {
            if (!Hack.Untargetable.IsEnabled()) return false;
            var players = StartOfRound.Instance?.allPlayerScripts;
            return players != null && playerId >= 0 && playerId < players.Length && IsHidden(players[playerId]);
        }

        /// True when `collider` belongs to the hidden local player.
        public static bool IsHiddenCollider(Collider? collider)
        {
            if (collider == null || !Hack.Untargetable.IsEnabled()) return false;
            return IsHidden(collider.GetComponentInParent<PlayerControllerB>());
        }

        /// True when a noise at `noisePosition` is close enough to the hidden local player to be its own.
        public static bool IsNoiseFromHiddenPlayer(Vector3 noisePosition, float radius = OwnNoiseRadius)
        {
            var local = LethalMenuMod.LocalPlayer;
            return local != null && IsHidden(local) &&
                   Vector3.Distance(noisePosition, local.transform.position) < radius;
        }

        // ---- static helpers the transpilers call ----

        public static bool IsControlled(PlayerControllerB player) =>
            player.isPlayerControlled && !IsHidden(player);

        public static PlayerControllerB? NullIfHidden(PlayerControllerB? player) =>
            IsHidden(player) ? null : player;

        public static bool IsGrounded(CharacterController controller)
        {
            if (!controller.isGrounded) return false;
            var local = LethalMenuMod.LocalPlayer;
            return !(local != null && IsHidden(local) && controller == local.thisController);
        }

        public static bool CheckSphere(Vector3 position, float radius, int layerMask, QueryTriggerInteraction query)
        {
            if (!Hack.Untargetable.IsEnabled()) return Physics.CheckSphere(position, radius, layerMask, query);
            return Physics.OverlapSphere(position, radius, layerMask, query).Any(c => !IsHiddenCollider(c));
        }

        public static Collider[] OverlapSphere(Vector3 position, float radius, int layerMask, QueryTriggerInteraction query)
        {
            var hits = Physics.OverlapSphere(position, radius, layerMask, query);
            if (!Hack.Untargetable.IsEnabled()) return hits;
            return hits.Where(c => !IsHiddenCollider(c)).ToArray();
        }

        // ---- transpilers ----

        /// `player.isPlayerControlled` -> IsControlled(player). The hidden player stops counting as a live
        /// player in allPlayerScripts loops that only filter on isPlayerControlled.
        public static IEnumerable<CodeInstruction> HidePlayerFromLoops(IEnumerable<CodeInstruction> instructions)
        {
            var field = AccessTools.Field(typeof(PlayerControllerB), nameof(PlayerControllerB.isPlayerControlled));
            var helper = AccessTools.Method(typeof(HiddenPlayerHelpers), nameof(IsControlled));
            foreach (var instruction in instructions)
            {
                if (instruction.LoadsField(field))
                {
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = helper;
                }
                yield return instruction;
            }
        }

        /// `x.GetComponent<PlayerControllerB>()` -> NullIfHidden(...). Physics-hit and trigger handlers
        /// that resolve the player from a collider then see no player for the hidden local player.
        public static IEnumerable<CodeInstruction> HidePlayerFromComponentLookups(IEnumerable<CodeInstruction> instructions)
        {
            var helper = AccessTools.Method(typeof(HiddenPlayerHelpers), nameof(NullIfHidden));
            foreach (var instruction in instructions)
            {
                yield return instruction;
                if (instruction.operand is MethodInfo method && method.Name == nameof(Component.GetComponent) &&
                    method.IsGenericMethod && method.GetParameters().Length == 0 &&
                    method.GetGenericArguments()[0] == typeof(PlayerControllerB))
                    yield return new CodeInstruction(OpCodes.Call, helper);
            }
        }

        /// `CharacterController.isGrounded` -> IsGrounded(controller): the hidden local player is never
        /// grounded for proximity checks that read it.
        public static IEnumerable<CodeInstruction> HidePlayerGrounded(IEnumerable<CodeInstruction> instructions)
        {
            var getter = AccessTools.PropertyGetter(typeof(CharacterController), nameof(CharacterController.isGrounded));
            var helper = AccessTools.Method(typeof(HiddenPlayerHelpers), nameof(IsGrounded));
            foreach (var instruction in instructions)
            {
                if (instruction.Calls(getter))
                {
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = helper;
                }
                yield return instruction;
            }
        }

        /// `Physics.CheckSphere(pos, r, mask, query)` -> CheckSphere(...) ignoring the hidden local player.
        public static IEnumerable<CodeInstruction> HidePlayerFromCheckSphere(IEnumerable<CodeInstruction> instructions) =>
            SwapPhysicsCall(instructions, nameof(Physics.CheckSphere), nameof(CheckSphere));

        /// `Physics.OverlapSphere(pos, r, mask, query)` -> OverlapSphere(...) without the hidden local player.
        public static IEnumerable<CodeInstruction> HidePlayerFromOverlapSphere(IEnumerable<CodeInstruction> instructions) =>
            SwapPhysicsCall(instructions, nameof(Physics.OverlapSphere), nameof(OverlapSphere));

        private static IEnumerable<CodeInstruction> SwapPhysicsCall(
            IEnumerable<CodeInstruction> instructions, string physicsMethod, string helperMethod)
        {
            var args = new[] { typeof(Vector3), typeof(float), typeof(int), typeof(QueryTriggerInteraction) };
            var original = AccessTools.Method(typeof(Physics), physicsMethod, args);
            var helper = AccessTools.Method(typeof(HiddenPlayerHelpers), helperMethod, args);
            foreach (var instruction in instructions)
            {
                if (instruction.Calls(original)) instruction.operand = helper;
                yield return instruction;
            }
        }
    }
}
