using System;
using System.Linq;
using GameNetcodeStuff;
using Unity.Netcode;

namespace LethalMenu.Cheats
{
    public static partial class NetworkCheats
    {
        #region Utility Methods

        /// Gets all available ship unlockables and their IDs.
        public static (int id, string name, bool unlocked)[] GetShipUnlockables()
        {
            var startOfRound = StartOfRound.Instance;
            if (startOfRound == null || startOfRound.unlockablesList == null)
                return Array.Empty<(int, string, bool)>();

            var unlockables = startOfRound.unlockablesList.unlockables;
            var result = new (int, string, bool)[unlockables.Count];

            for (int i = 0; i < unlockables.Count; i++)
            {
                var u = unlockables[i];
                result[i] = (i, u.unlockableName ?? $"Unlockable {i}", u.hasBeenUnlockedByPlayer);
            }

            return result;
        }

        /// Gets all available levels (moons) and their IDs.
        public static (int id, string name)[] GetAvailableLevels()
        {
            var startOfRound = StartOfRound.Instance;
            if (startOfRound == null || startOfRound.levels == null)
                return Array.Empty<(int, string)>();

            var result = new (int, string)[startOfRound.levels.Length];
            for (int i = 0; i < startOfRound.levels.Length; i++)
            {
                result[i] = (i, startOfRound.levels[i]?.PlanetName ?? $"Level {i}");
            }
            return result;
        }

        /// Gets all connected players.
        public static PlayerControllerB[] GetAllPlayers()
        {
            return LethalMenuMod.Players.Where(p => p != null && !p.isPlayerDead).ToArray();
        }

        /// Gets all alive enemies.
        public static EnemyAI[] GetAllEnemies()
        {
            return LethalMenuMod.Enemies.Where(e => e != null && !e.isEnemyDead).ToArray();
        }

        /// Check if we're the host (have more permissions).
        public static bool IsHost()
        {
            return NetworkManager.Singleton?.IsHost ?? false;
        }

        #endregion
    }
}
