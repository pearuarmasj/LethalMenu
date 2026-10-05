using System;
using System.Collections.Generic;
using System.Reflection;
using GameNetcodeStuff;
using HarmonyLib;

namespace LethalMenu.Patches
{
    /// RadMechAI: grab, torch and proximity-grab paths that never consult PlayerIsTargetable.
    /// - GrabPlayerServerRpc (RadMechAI.OnCollideWithPlayer -> server): the host refuses a grab of the hidden player.
    /// - BeginTorchPlayer (GrabPlayerClientRpc): the local client never enters the torch animation, so
    ///   TorchPlayerAnimation never runs its DamagePlayer/KillPlayer loop for the hidden player.
    /// - Update: a torch already running when Untargetable is switched on is cancelled.
    /// - AttemptGrabIfClose: the distance loop over allPlayerScripts that starts the grab stance skips the hidden player.
    [HarmonyPatch]
    internal static class RadMechAIUntargetablePatches
    {
        [HarmonyPatch(typeof(RadMechAI), nameof(RadMechAI.GrabPlayerServerRpc))]
        [HarmonyPrefix]
        private static bool GrabPlayerServerRpcPrefix(EnemyAI __instance, int playerId) => !HiddenPlayerHelpers.IsHiddenId(__instance, playerId);

        [HarmonyPatch(typeof(RadMechAI), nameof(RadMechAI.BeginTorchPlayer))]
        [HarmonyPrefix]
        private static bool BeginTorchPlayerPrefix(EnemyAI __instance, PlayerControllerB playerBeingTorched) =>
            !HiddenPlayerHelpers.IsHiddenFrom(__instance, playerBeingTorched);

        [HarmonyPatch(typeof(RadMechAI), nameof(RadMechAI.Update))]
        [HarmonyPostfix]
        private static void UpdatePostfix(RadMechAI __instance)
        {
            if (HiddenPlayerHelpers.IsHiddenFrom(__instance, __instance.inSpecialAnimationWithPlayer))
                __instance.CancelTorchPlayerAnimation();
        }

        [HarmonyPatch(typeof(RadMechAI), nameof(RadMechAI.AttemptGrabIfClose))]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> AttemptGrabIfCloseTranspiler(IEnumerable<CodeInstruction> instructions) =>
            HiddenPlayerHelpers.HidePlayerFromLoops(instructions);
    }

    /// IVisibleThreat.GetVisibility on PlayerControllerB is the single value RadMechAI.CheckSightForThreat
    /// (OverlapSphere over the player layer + TryGetComponent<IVisibleThreat>), RadMechAI.MoveTowardsThreat
    /// and RadMechAI.LookForPlayersInFlight use to decide whether a player can be a threat target. Reporting 0
    /// for the hidden local player makes every one of those checks (visibility < 0.2 / < 0.8 / > 0) fail.
    /// The other IVisibleThreat consumers (ForestGiant, BushWolf, Baboon Hawk, Giant Kiwi, Puma, Masked)
    /// read the same value, so they stop seeing the hidden player as well.
    [HarmonyPatch]
    internal static class PlayerVisibleThreatPatches
    {
        [HarmonyTargetMethod]
        private static MethodBase TargetMethod()
        {
            var map = typeof(PlayerControllerB).GetInterfaceMap(typeof(IVisibleThreat));
            var index = Array.IndexOf(map.InterfaceMethods, typeof(IVisibleThreat).GetMethod(nameof(IVisibleThreat.GetVisibility)));
            return map.TargetMethods[index];
        }

        [HarmonyPostfix]
        private static void Postfix(PlayerControllerB __instance, ref float __result)
        {
            if (HiddenPlayerHelpers.IsHidden(__instance)) __result = 0f;
        }
    }
}
