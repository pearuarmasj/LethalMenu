using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using GameNetcodeStuff;
using HarmonyLib;
using LethalMenu.Cheats.Directives;
using UnityEngine;

namespace LethalMenu.Patches
{
    /// Untargetable: hide the local player from enemy sight checks that bypass EnemyAI.PlayerIsTargetable.
    /// EnemyAI.CheckLineOfSightForPlayer / CheckLineOfSightForClosestPlayer (Crawler, Lasso Man and most
    /// others) and Nutcracker's own CheckLineOfSightForLocalPlayer/Update each read
    /// `player.gameplayCamera.transform.position` and range/linecast-test it. Every such read is rerouted
    /// through SightPosition, which reports a point far below the map for the local player, so the range
    /// and linecast checks fail and the client never reports itself to the server as seen.
    [HarmonyPatch]
    internal static class UntargetableSightPatches
    {
        /// Where the local player "is" for enemy sight checks while Untargetable.
        public static readonly Vector3 HiddenPosition = new(0f, -10000f, 0f);

        /// True when `player` is the local player and Untargetable is on. Shared by every
        /// Untargetable patch so they agree on who is hidden.
        public static bool IsHidden(PlayerControllerB? player) =>
            player != null && Hack.Untargetable.IsEnabled() && player == LethalMenuMod.LocalPlayer;

        /// IsHidden, plus: a directed enemy (escort / hunt) treats the local player exactly as hidden,
        /// regardless of the Untargetable toggle, so it never targets or harms the player directing it.
        /// Used wherever the enemy instance is available.
        public static bool IsHiddenFrom(EnemyAI? enemy, PlayerControllerB? player) =>
            IsHidden(player) ||
            (player != null && player == LethalMenuMod.LocalPlayer && enemy != null && EnemyDirector.IsDirected(enemy));

        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(EnemyAI), nameof(EnemyAI.CheckLineOfSightForPlayer));
            yield return AccessTools.Method(typeof(EnemyAI), nameof(EnemyAI.CheckLineOfSightForClosestPlayer));
            yield return AccessTools.Method(typeof(NutcrackerEnemyAI), nameof(NutcrackerEnemyAI.CheckLineOfSightForLocalPlayer));
            yield return AccessTools.Method(typeof(NutcrackerEnemyAI), nameof(NutcrackerEnemyAI.Update));
        }

        public static Vector3 SightPositionFrom(PlayerControllerB player, EnemyAI enemy)
        {
            if (IsHiddenFrom(enemy, player))
                return HiddenPosition;
            return player.gameplayCamera.transform.position;
        }

        /// Replaces `ldfld gameplayCamera; callvirt get_transform; callvirt get_position` (stack: player)
        /// with `ldarg.0; call SightPositionFrom(player, enemy)`. Enemy-aware form: every target method
        /// (EnemyAI.CheckLineOfSightForPlayer/ForClosestPlayer, NutcrackerEnemyAI.CheckLineOfSightForLocalPlayer/Update)
        /// is a plain instance method of an EnemyAI, so ldarg.0 is the enemy and directed enemies hide the local
        /// player too. Only valid for such targets.
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var cameraField = AccessTools.Field(typeof(PlayerControllerB), nameof(PlayerControllerB.gameplayCamera));
            var getTransform = AccessTools.PropertyGetter(typeof(Component), nameof(Component.transform));
            var getPosition = AccessTools.PropertyGetter(typeof(Transform), nameof(Transform.position));
            var sightPosition = AccessTools.Method(typeof(UntargetableSightPatches), nameof(SightPositionFrom));

            var codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                if (i + 2 < codes.Count && codes[i].LoadsField(cameraField) &&
                    codes[i + 1].Calls(getTransform) && codes[i + 2].Calls(getPosition))
                {
                    var enemy = new CodeInstruction(OpCodes.Ldarg_0).MoveLabelsFrom(codes[i]);
                    enemy.blocks.AddRange(codes[i].blocks);
                    yield return enemy;
                    yield return new CodeInstruction(OpCodes.Call, sightPosition);
                    i += 2;
                    continue;
                }
                yield return codes[i];
            }
        }
    }

    /// Nutcracker only reports a player it sees moving; the local player never counts as moving while
    /// Untargetable or directed.
    [HarmonyPatch(typeof(NutcrackerEnemyAI), nameof(NutcrackerEnemyAI.IsLocalPlayerMoving))]
    internal static class NutcrackerMovingPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(NutcrackerEnemyAI __instance, ref bool __result)
        {
            if (!UntargetableSightPatches.IsHiddenFrom(__instance, LethalMenuMod.LocalPlayer)) return true;
            __result = false;
            return false;
        }
    }
}
