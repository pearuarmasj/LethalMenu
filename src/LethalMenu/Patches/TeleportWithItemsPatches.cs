using System.Collections.Generic;
using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace LethalMenu.Patches
{
    /// Teleport with items: the local player keeps their inventory when a ship teleporter moves them. Only the two
    /// teleporter call sites are redirected; every other DropAllHeldItems caller (death, disconnect, centipede,
    /// bush wolf, ...) is untouched, so other players' state never desyncs.
    ///   normal teleporter: beamUpPlayer calls DropAllHeldItemsAndSync for the local player
    ///   inverse teleporter: TeleportPlayerOutWithInverseTeleporter calls DropAllHeldItems for the teleported player
    /// beamUpPlayer's other drop (the player carrying a dead body) is a different call and is left alone.
    internal static class TeleportWithItemsPatches
    {
        private static bool KeepItems(PlayerControllerB player) =>
            Hack.TeleportWithItems.IsEnabled() && player == LethalMenuMod.LocalPlayer;

        public static void DropAllHeldItemsAndSyncUnlessKept(PlayerControllerB player, Vector3 playerPosition,
            Vector3 itemsPosition, Vector3 itemsRotation, Vector3 playerCameraPosition, Vector3 playerCameraRotation)
        {
            if (KeepItems(player)) return;
            player.DropAllHeldItemsAndSync(playerPosition, itemsPosition, itemsRotation, playerCameraPosition, playerCameraRotation);
        }

        public static void DropAllHeldItemsUnlessKept(PlayerControllerB player, bool itemsFall, bool disconnecting,
            bool setInShip, bool setInElevator, Vector3 syncedPlayerPosition, Vector3 syncedHeldObjectPosition,
            Vector3 syncedHeldObjectRotation, Vector3 syncedPlayerCamPosition, Vector3 syncedPlayerCamRotation)
        {
            if (KeepItems(player)) return;
            player.DropAllHeldItems(itemsFall, disconnecting, setInShip, setInElevator, syncedPlayerPosition,
                syncedHeldObjectPosition, syncedHeldObjectRotation, syncedPlayerCamPosition, syncedPlayerCamRotation);
        }

        /// Swaps every call to `from` for a call to the static `to` (same stack shape: instance + arguments).
        internal static IEnumerable<CodeInstruction> Redirect(IEnumerable<CodeInstruction> instructions,
            System.Reflection.MethodInfo from, System.Reflection.MethodInfo to)
        {
            foreach (var instr in instructions)
            {
                if (instr.Calls(from))
                {
                    yield return new CodeInstruction(System.Reflection.Emit.OpCodes.Call, to);
                }
                else
                {
                    yield return instr;
                }
            }
        }

        [HarmonyPatch]
        internal static class BeamUpPatch
        {
            private static System.Reflection.MethodBase TargetMethod() =>
                AccessTools.EnumeratorMoveNext(AccessTools.DeclaredMethod(typeof(ShipTeleporter), nameof(ShipTeleporter.beamUpPlayer)));

            [HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) =>
                Redirect(instructions,
                    AccessTools.Method(typeof(PlayerControllerB), nameof(PlayerControllerB.DropAllHeldItemsAndSync)),
                    AccessTools.Method(typeof(TeleportWithItemsPatches), nameof(DropAllHeldItemsAndSyncUnlessKept)));
        }

        [HarmonyPatch(typeof(ShipTeleporter), nameof(ShipTeleporter.TeleportPlayerOutWithInverseTeleporter))]
        internal static class InverseTeleportPatch
        {
            [HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) =>
                Redirect(instructions,
                    AccessTools.Method(typeof(PlayerControllerB), nameof(PlayerControllerB.DropAllHeldItems)),
                    AccessTools.Method(typeof(TeleportWithItemsPatches), nameof(DropAllHeldItemsUnlessKept)));
        }
    }
}
