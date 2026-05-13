using HarmonyLib;

namespace LethalMenu.Patches
{
    /// Zeros currentUseCooldown each Update on the local player's held item while NoCooldown is enabled.
    [HarmonyPatch(typeof(GrabbableObject), "Update")]
    internal static class NoCooldownPatch
    {
        [HarmonyPostfix]
        private static void Postfix(GrabbableObject __instance)
        {
            if (!Hack.NoCooldown.IsEnabled()) return;
            if (__instance == null) return;
            if (__instance.playerHeldBy != LethalMenuMod.LocalPlayer) return;
            __instance.currentUseCooldown = 0f;
        }
    }
}
