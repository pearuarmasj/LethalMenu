using System.Collections.Generic;
using GameNetcodeStuff;
using UnityEngine;

namespace LethalMenu.Util
{
    /// <summary>
    /// Collects and caches game objects for easy access.
    /// </summary>
    public static class ObjectManager
    {
        private static float _lastCollectTime;
        private const float CollectInterval = 2f; // Collect every 2 seconds
        
        // Store FindObjectsOfType results to reduce allocation frequency
        // Note: These are updated every collection cycle (2 sec) but reusing the
        // reference helps with garbage collector pressure vs allocating in local scope
        private static PlayerControllerB[] _playerCache = System.Array.Empty<PlayerControllerB>();
        private static EnemyAI[] _enemyCache = System.Array.Empty<EnemyAI>();
        private static GrabbableObject[] _itemCache = System.Array.Empty<GrabbableObject>();
        private static DoorLock[] _doorCache = System.Array.Empty<DoorLock>();
        private static Landmine[] _mineCache = System.Array.Empty<Landmine>();
        private static Turret[] _turretCache = System.Array.Empty<Turret>();
        private static EntranceTeleport[] _entranceCache = System.Array.Empty<EntranceTeleport>();
        private static ShipTeleporter[] _teleporterCache = System.Array.Empty<ShipTeleporter>();

        /// <summary>
        /// Force immediate collection of all objects.
        /// </summary>
        public static void ForceCollect()
        {
            _lastCollectTime = 0f;
            CollectObjects();
        }

        /// <summary>
        /// Collect all game objects (throttled).
        /// </summary>
        public static void CollectObjects()
        {
            if (Time.time - _lastCollectTime < CollectInterval) return;
            _lastCollectTime = Time.time;

            CollectPlayers();
            CollectEnemies();
            CollectItems();
            CollectDoors();
            CollectLandmines();
            CollectTurrets();
            CollectEntrances();
            CollectTeleporters();
        }

        private static void CollectPlayers()
        {
            LethalMenuMod.Players.Clear();

            // Reuse cached array to reduce allocations
            _playerCache = Object.FindObjectsOfType<PlayerControllerB>();
            for (int i = 0; i < _playerCache.Length; i++)
            {
                var player = _playerCache[i];
                if (player != null && player.isPlayerControlled)
                {
                    LethalMenuMod.Players.Add(player);
                }
            }
        }

        private static void CollectEnemies()
        {
            LethalMenuMod.Enemies.Clear();

            // Reuse cached array to reduce allocations
            _enemyCache = Object.FindObjectsOfType<EnemyAI>();
            for (int i = 0; i < _enemyCache.Length; i++)
            {
                var enemy = _enemyCache[i];
                if (enemy != null && !enemy.isEnemyDead)
                {
                    LethalMenuMod.Enemies.Add(enemy);
                }
            }
        }

        private static void CollectItems()
        {
            LethalMenuMod.Items.Clear();

            // Reuse cached array to reduce allocations
            _itemCache = Object.FindObjectsOfType<GrabbableObject>();
            for (int i = 0; i < _itemCache.Length; i++)
            {
                var item = _itemCache[i];
                if (item != null)
                {
                    LethalMenuMod.Items.Add(item);
                }
            }
        }

        private static void CollectDoors()
        {
            LethalMenuMod.DoorLocks.Clear();

            // Reuse cached array to reduce allocations
            _doorCache = Object.FindObjectsOfType<DoorLock>();
            for (int i = 0; i < _doorCache.Length; i++)
            {
                var door = _doorCache[i];
                if (door != null)
                {
                    LethalMenuMod.DoorLocks.Add(door);
                }
            }
        }

        private static void CollectLandmines()
        {
            LethalMenuMod.Landmines.Clear();

            // Reuse cached array to reduce allocations
            _mineCache = Object.FindObjectsOfType<Landmine>();
            for (int i = 0; i < _mineCache.Length; i++)
            {
                var mine = _mineCache[i];
                if (mine != null)
                {
                    LethalMenuMod.Landmines.Add(mine);
                }
            }
        }

        private static void CollectTurrets()
        {
            LethalMenuMod.Turrets.Clear();

            // Reuse cached array to reduce allocations
            _turretCache = Object.FindObjectsOfType<Turret>();
            for (int i = 0; i < _turretCache.Length; i++)
            {
                var turret = _turretCache[i];
                if (turret != null)
                {
                    LethalMenuMod.Turrets.Add(turret);
                }
            }
        }

        private static void CollectEntrances()
        {
            LethalMenuMod.Entrances.Clear();

            // Reuse cached array to reduce allocations
            _entranceCache = Object.FindObjectsOfType<EntranceTeleport>();
            for (int i = 0; i < _entranceCache.Length; i++)
            {
                var entrance = _entranceCache[i];
                if (entrance != null)
                {
                    LethalMenuMod.Entrances.Add(entrance);
                }
            }
        }

        private static void CollectTeleporters()
        {
            LethalMenuMod.Teleporters.Clear();

            // Reuse cached array to reduce allocations
            _teleporterCache = Object.FindObjectsOfType<ShipTeleporter>();
            for (int i = 0; i < _teleporterCache.Length; i++)
            {
                var teleporter = _teleporterCache[i];
                if (teleporter != null)
                {
                    LethalMenuMod.Teleporters.Add(teleporter);
                }
            }
        }
    }
}
