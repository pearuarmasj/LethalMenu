using HarmonyLib;

namespace LethalMenu.Patches
{
    /// Keeps the ship door open on this client while NoShipDoorClose is enabled. The close path is
    /// button -> SetDoorsClosedServerRpc -> SetDoorsClosedClientRpc (everyone) -> StartOfRound.SetShipDoorsClosed,
    /// with the animator driven by PlayDoorAnimation. Closing is refused at each of those local entry points;
    /// a client cannot stop other clients from closing their own copy of the door.
    internal static class NoShipDoorClosePatch
    {
        /// Local state: hangarDoorsClosed stays false even when another player's close RPC arrives.
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SetShipDoorsClosed))]
        internal static class StateGate
        {
            [HarmonyPrefix]
            private static bool Prefix(bool closed) => !(closed && Hack.NoShipDoorClose.IsEnabled());
        }

        /// Tells the server/other clients nothing when we try to close the door ourselves.
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SetDoorsClosedServerRpc))]
        internal static class RpcGate
        {
            [HarmonyPrefix]
            private static bool Prefix(bool closed) => !(closed && Hack.NoShipDoorClose.IsEnabled());
        }

        /// Keeps the door animation open.
        [HarmonyPatch(typeof(HangarShipDoor), nameof(HangarShipDoor.PlayDoorAnimation))]
        internal static class AnimationGate
        {
            [HarmonyPrefix]
            private static bool Prefix(bool closed) => !(closed && Hack.NoShipDoorClose.IsEnabled());
        }

        /// The close button handler.
        [HarmonyPatch(typeof(HangarShipDoor), nameof(HangarShipDoor.SetDoorClosed))]
        internal static class ButtonGate
        {
            [HarmonyPrefix]
            private static bool Prefix() => !Hack.NoShipDoorClose.IsEnabled();
        }
    }
}
