using HarmonyLib;

namespace LethalMenu.Patches
{
    /// Skips battery decrement when InfiniteItemUsage is enabled (held items never run out).
    [HarmonyPatch(typeof(GrabbableObject), "UseItemBatteries")]
    internal static class InfiniteItemUsagePatch
    {
        [HarmonyPrefix]
        private static bool Prefix() => !Hack.InfiniteItemUsage.IsEnabled();
    }
}
