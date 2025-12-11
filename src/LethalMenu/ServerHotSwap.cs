using System;
using UnityEngine;

namespace LethalMenu
{
    /// Hot-swap server connection - disconnect and reconnect without going to main menu.
    /// CURRENTLY DISABLED - half-works but boots to main menu instead of new lobby.
    public static class ServerHotSwap
    {
        // Stub properties
        public static ulong PendingLobbyId => 0;
        public static ulong PendingHostId => 0;
        public static bool IsHotSwapping => false;
        public static string Status => "Disabled";

        /// Stub - feature disabled.
        public static void HotSwapTo(ulong lobbyId, ulong hostId)
        {
            HUDManager.Instance?.DisplayTip("Hot Swap", "Feature currently disabled.");
            Debug.Log("[HotSwap] Feature is disabled - needs more work");
        }

        public static void Cancel() { }
    }
}
