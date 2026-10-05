using System.Collections.Generic;
using HarmonyLib;

namespace LethalMenu.Patches
{
    /// SpikeRoofTrap detection and kill paths, all keyed on the local player.
    /// - Update (laser mode): the laser raycast resolves `hit.collider.GetComponent<PlayerControllerB>()` and
    ///   slams when it is the local player.
    /// - Update (interval mode, host): slams when `Physics.CheckSphere(laserEye, 14, player layer)` finds a player
    ///   (or no enemy is within 8 m); the hidden player's colliders no longer count.
    /// - OnTriggerStay: while slamming, the local player under the spikes gets KillPlayer(Crushing); the hidden
    ///   player resolves to no player there.
    [HarmonyPatch]
    internal static class SpikeRoofTrapUntargetablePatches
    {
        [HarmonyPatch(typeof(SpikeRoofTrap), nameof(SpikeRoofTrap.Update))]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> UpdateTranspiler(IEnumerable<CodeInstruction> instructions) =>
            HiddenPlayerHelpers.HidePlayerFromCheckSphere(HiddenPlayerHelpers.HidePlayerFromComponentLookups(instructions));

        [HarmonyPatch(typeof(SpikeRoofTrap), nameof(SpikeRoofTrap.OnTriggerStay))]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> OnTriggerStayTranspiler(IEnumerable<CodeInstruction> instructions) =>
            HiddenPlayerHelpers.HidePlayerFromComponentLookups(instructions);
    }
}
