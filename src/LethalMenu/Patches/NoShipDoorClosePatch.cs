using HarmonyLib;

namespace LethalMenu.Patches
{
    /// Blocks HangarShipDoor.SetDoorClosed so the host (and game logic) cannot close the ship door.
    [HarmonyPatch(typeof(HangarShipDoor), "SetDoorClosed")]
    internal static class NoShipDoorClosePatch
    {
        [HarmonyPrefix]
        private static bool Prefix() => !Hack.NoShipDoorClose.IsEnabled();
    }
}
