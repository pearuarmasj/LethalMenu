using GameNetcodeStuff;
using UnityEngine;

namespace LethalMenu.Cheats
{
    public static partial class NetworkCheats
    {
        #region Host-Only Network Cheats

        /// Revives all dead players. Must be host.
        public static void ReviveAllPlayers()
        {
            var startOfRound = StartOfRound.Instance;
            if (startOfRound == null)
            {
                Debug.LogWarning("[NetworkCheats] Not in game.");
                HUDManager.Instance?.DisplayTip("Revive All", "Not in game.");
                return;
            }

            if (!LethalMenuMod.LocalPlayer?.IsHost == true)
            {
                Debug.LogWarning("[NetworkCheats] Must be host to revive all players.");
                HUDManager.Instance?.DisplayTip("Revive All", "Host only.");
                return;
            }

            // Call ReviveDeadPlayers directly as host
            startOfRound.ReviveDeadPlayers();
            HUDManager.Instance?.HideHUD(false);

            // Sync to clients
            startOfRound.Debug_ReviveAllPlayersServerRpc();

            Debug.Log("[NetworkCheats] Revived all players.");
            HUDManager.Instance?.DisplayTip("Revive All", "All players revived.");
        }

        /// Unlocks all doors in the facility.
        public static void UnlockAllDoors()
        {
            var doors = Object.FindObjectsOfType<DoorLock>();
            if (doors == null || doors.Length == 0)
            {
                HUDManager.Instance?.DisplayTip("Unlock Doors", "No doors found.");
                return;
            }

            int count = 0;
            foreach (var door in doors)
            {
                if (door.isLocked)
                {
                    door.UnlockDoorSyncWithServer();
                    count++;
                }
            }

            Debug.Log($"[NetworkCheats] Unlocked {count} doors.");
            HUDManager.Instance?.DisplayTip("Unlock Doors", $"Unlocked {count} doors.");
        }

        /// Locks all doors in the facility. Host only for full effect.
        public static void LockAllDoors()
        {
            var doors = Object.FindObjectsOfType<DoorLock>();
            if (doors == null || doors.Length == 0)
            {
                HUDManager.Instance?.DisplayTip("Lock Doors", "No doors found.");
                return;
            }

            int count = 0;
            foreach (var door in doors)
            {
                if (!door.isLocked)
                {
                    door.LockDoor(9999f); // Lock for very long time
                    count++;
                }
            }

            Debug.Log($"[NetworkCheats] Locked {count} doors.");
            HUDManager.Instance?.DisplayTip("Lock Doors", $"Locked {count} doors.");
        }

        /// Spawns an enemy at a target position. Must be host.
        /// <param name="enemyName">Name of the enemy type to spawn.</param>
        /// <param name="position">World position to spawn at.</param>
        /// <param name="outsideEnemy">True for outside enemies, false for inside.</param>
        public static void SpawnEnemy(string enemyName, Vector3 position, bool outsideEnemy = false)
        {
            var roundManager = RoundManager.Instance;
            if (roundManager == null)
            {
                HUDManager.Instance?.DisplayTip("Spawn Enemy", "Not in game.");
                return;
            }

            if (!LethalMenuMod.LocalPlayer?.IsHost == true)
            {
                HUDManager.Instance?.DisplayTip("Spawn Enemy", "Host only.");
                return;
            }

            // Find the enemy type
            EnemyType? enemyType = FindEnemyType(enemyName, outsideEnemy);
            if (enemyType == null)
            {
                HUDManager.Instance?.DisplayTip("Spawn Enemy", $"Enemy '{enemyName}' not found.");
                return;
            }

            // Spawn the enemy
            roundManager.SpawnEnemyGameObject(position, 0f, -1, enemyType);
            Debug.Log($"[NetworkCheats] Spawned {enemyName} at {position}");
            HUDManager.Instance?.DisplayTip("Spawn Enemy", $"Spawned {enemyName}.");
        }

        /// Spawns an enemy at a target player's position.
        public static void SpawnEnemyAtPlayer(string enemyName, PlayerControllerB targetPlayer, bool outsideEnemy = false)
        {
            if (targetPlayer == null) return;
            SpawnEnemy(enemyName, targetPlayer.transform.position, outsideEnemy);
        }

        #endregion
    }
}
