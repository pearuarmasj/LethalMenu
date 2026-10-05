using System;
using System.Linq;
using GameNetcodeStuff;
using Unity.Netcode;

namespace LethalMenu.Cheats
{
    public static partial class NetworkCheats
    {
        #region Utility Methods

        /// Gets all connected players.
        public static PlayerControllerB[] GetAllPlayers()
        {
            return LethalMenuMod.Players.Where(p => p != null && !p.isPlayerDead).ToArray();
        }

        /// Check if we're the host (have more permissions).
        public static bool IsHost()
        {
            return NetworkManager.Singleton?.IsHost ?? false;
        }

        #endregion
    }
}
