using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using GameNetcodeStuff;
using HarmonyLib;
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

        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(EnemyAI), nameof(EnemyAI.CheckLineOfSightForPlayer));
            yield return AccessTools.Method(typeof(EnemyAI), nameof(EnemyAI.CheckLineOfSightForClosestPlayer));
            yield return AccessTools.Method(typeof(NutcrackerEnemyAI), nameof(NutcrackerEnemyAI.CheckLineOfSightForLocalPlayer));
            yield return AccessTools.Method(typeof(NutcrackerEnemyAI), nameof(NutcrackerEnemyAI.Update));
        }

        public static Vector3 SightPosition(PlayerControllerB player)
        {
            if (IsHidden(player))
                return HiddenPosition;
            return player.gameplayCamera.transform.position;
        }

        /// Replaces `ldfld gameplayCamera; callvirt get_transform; callvirt get_position` (stack: player)
        /// with `call SightPosition(player)`. Reusable by other patches:
        /// `[HarmonyTranspiler] static IEnumerable<CodeInstruction> T(IEnumerable<CodeInstruction> c) => UntargetableSightPatches.Transpiler(c);`
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var cameraField = AccessTools.Field(typeof(PlayerControllerB), nameof(PlayerControllerB.gameplayCamera));
            var getTransform = AccessTools.PropertyGetter(typeof(Component), nameof(Component.transform));
            var getPosition = AccessTools.PropertyGetter(typeof(Transform), nameof(Transform.position));
            var sightPosition = AccessTools.Method(typeof(UntargetableSightPatches), nameof(SightPosition));

            var codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                if (i + 2 < codes.Count && codes[i].LoadsField(cameraField) &&
                    codes[i + 1].Calls(getTransform) && codes[i + 2].Calls(getPosition))
                {
                    var call = new CodeInstruction(OpCodes.Call, sightPosition).MoveLabelsFrom(codes[i]);
                    call.blocks.AddRange(codes[i].blocks);
                    yield return call;
                    i += 2;
                    continue;
                }
                yield return codes[i];
            }
        }
    }

    /// Nutcracker only reports a player it sees moving; the local player never counts as moving while
    /// Untargetable.
    [HarmonyPatch(typeof(NutcrackerEnemyAI), nameof(NutcrackerEnemyAI.IsLocalPlayerMoving))]
    internal static class NutcrackerMovingPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(ref bool __result)
        {
            if (!Hack.Untargetable.IsEnabled()) return true;
            __result = false;
            return false;
        }
    }
}
