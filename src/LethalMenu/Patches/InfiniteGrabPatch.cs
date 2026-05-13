using GameNetcodeStuff;
using HarmonyLib;

namespace LethalMenu.Patches
{
    /// Temporarily widens grabDistance during hover-tip evaluation so items at any range are grabbable.
    [HarmonyPatch(typeof(PlayerControllerB), "SetHoverTipAndCurrentInteractTrigger")]
    internal static class InfiniteGrabPatch
    {
        private const float HugeRange = 1000f;
        private static float _originalGrabDistance;

        [HarmonyPrefix]
        private static void Prefix(PlayerControllerB __instance)
        {
            if (!Hack.InfiniteGrab.IsEnabled()) return;
            _originalGrabDistance = __instance.grabDistance;
            __instance.grabDistance = HugeRange;
        }

        [HarmonyPostfix]
        private static void Postfix(PlayerControllerB __instance)
        {
            if (!Hack.InfiniteGrab.IsEnabled()) return;
            __instance.grabDistance = _originalGrabDistance;
        }
    }
}
