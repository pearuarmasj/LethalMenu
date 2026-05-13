using HarmonyLib;

namespace LethalMenu.Patches
{
    /// Blocks vehicle damage so the Cruiser ignores collisions and weapon hits.
    [HarmonyPatch(typeof(VehicleController))]
    internal static class VehicleGodModePatch
    {
        [HarmonyPatch("DealPermanentDamage")]
        [HarmonyPrefix]
        private static bool BlockDamage() => !Hack.VehicleGodMode.IsEnabled();

        [HarmonyPatch("ReactToDamage")]
        [HarmonyPrefix]
        private static bool BlockReaction() => !Hack.VehicleGodMode.IsEnabled();
    }
}
