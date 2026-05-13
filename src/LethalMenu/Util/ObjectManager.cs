using System.Collections.Generic;
using GameNetcodeStuff;
using UnityEngine;

namespace LethalMenu.Util
{
    /// 
    /// Collects and caches game objects for easy access.
    /// 
    public static class ObjectManager
    {
        private static float _lastCollectTime;
        private const float CollectInterval = 2f; // Collect every 2 seconds

        /// 
        /// Force immediate collection of all objects.
        /// 
        public static void ForceCollect()
        {
            _lastCollectTime = 0f;
            CollectObjects();
        }

        /// 
        /// Collect all game objects (throttled).
        /// 
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
            CollectBreakerBoxes();
            CollectSteamValves();
            CollectBigDoors();
            CollectHangarShipDoors();
            CollectEnemyVents();
            CollectItemDropships();
            CollectVehicles();
            CollectMoldSpores();
            CollectMineshaftElevators();
            CollectSpikeRoofTraps();
        }

        private static void CollectPlayers()
        {
            LethalMenuMod.Players.Clear();

            var players = Object.FindObjectsOfType<PlayerControllerB>();
            foreach (var player in players)
            {
                if (player != null && player.isPlayerControlled)
                {
                    LethalMenuMod.Players.Add(player);
                }
            }
        }

        private static void CollectEnemies()
        {
            LethalMenuMod.Enemies.Clear();

            var enemies = Object.FindObjectsOfType<EnemyAI>();
            foreach (var enemy in enemies)
            {
                if (enemy != null && !enemy.isEnemyDead)
                {
                    LethalMenuMod.Enemies.Add(enemy);
                }
            }
        }

        private static void CollectItems()
        {
            LethalMenuMod.Items.Clear();

            var items = Object.FindObjectsOfType<GrabbableObject>();
            foreach (var item in items)
            {
                if (item != null)
                {
                    LethalMenuMod.Items.Add(item);
                }
            }
        }

        private static void CollectDoors()
        {
            LethalMenuMod.DoorLocks.Clear();

            var doors = Object.FindObjectsOfType<DoorLock>();
            foreach (var door in doors)
            {
                if (door != null)
                {
                    LethalMenuMod.DoorLocks.Add(door);
                }
            }
        }

        private static void CollectLandmines()
        {
            LethalMenuMod.Landmines.Clear();

            var mines = Object.FindObjectsOfType<Landmine>();
            foreach (var mine in mines)
            {
                if (mine != null)
                {
                    LethalMenuMod.Landmines.Add(mine);
                }
            }
        }

        private static void CollectTurrets()
        {
            LethalMenuMod.Turrets.Clear();

            var turrets = Object.FindObjectsOfType<Turret>();
            foreach (var turret in turrets)
            {
                if (turret != null)
                {
                    LethalMenuMod.Turrets.Add(turret);
                }
            }
        }

        private static void CollectEntrances()
        {
            LethalMenuMod.Entrances.Clear();

            var entrances = Object.FindObjectsOfType<EntranceTeleport>();
            foreach (var entrance in entrances)
            {
                if (entrance != null)
                {
                    LethalMenuMod.Entrances.Add(entrance);
                }
            }
        }

        private static void CollectTeleporters()
        {
            LethalMenuMod.Teleporters.Clear();

            var teleporters = Object.FindObjectsOfType<ShipTeleporter>();
            foreach (var teleporter in teleporters)
            {
                if (teleporter != null)
                {
                    LethalMenuMod.Teleporters.Add(teleporter);
                }
            }
        }

        private static void CollectBreakerBoxes()
        {
            LethalMenuMod.BreakerBoxes.Clear();

            var boxes = Object.FindObjectsOfType<BreakerBox>();
            foreach (var box in boxes)
            {
                if (box != null)
                {
                    LethalMenuMod.BreakerBoxes.Add(box);
                }
            }
        }

        private static void CollectSteamValves()
        {
            LethalMenuMod.SteamValves.Clear();
            foreach (var v in Object.FindObjectsOfType<SteamValveHazard>())
                if (v != null) LethalMenuMod.SteamValves.Add(v);
        }

        private static void CollectBigDoors()
        {
            LethalMenuMod.BigDoors.Clear();
            foreach (var d in Object.FindObjectsOfType<TerminalAccessibleObject>())
                if (d != null && d.isBigDoor) LethalMenuMod.BigDoors.Add(d);
        }

        private static void CollectHangarShipDoors()
        {
            LethalMenuMod.HangarShipDoors.Clear();
            foreach (var d in Object.FindObjectsOfType<HangarShipDoor>())
                if (d != null) LethalMenuMod.HangarShipDoors.Add(d);
        }

        private static void CollectEnemyVents()
        {
            LethalMenuMod.EnemyVents.Clear();
            foreach (var v in Object.FindObjectsOfType<EnemyVent>())
                if (v != null) LethalMenuMod.EnemyVents.Add(v);
        }

        private static void CollectItemDropships()
        {
            LethalMenuMod.ItemDropships.Clear();
            foreach (var d in Object.FindObjectsOfType<ItemDropship>())
                if (d != null) LethalMenuMod.ItemDropships.Add(d);
        }

        private static void CollectVehicles()
        {
            LethalMenuMod.Vehicles.Clear();
            foreach (var v in Object.FindObjectsOfType<VehicleController>())
                if (v != null) LethalMenuMod.Vehicles.Add(v);
        }

        private static void CollectMoldSpores()
        {
            LethalMenuMod.MoldSpores.Clear();
            foreach (var go in Object.FindObjectsOfType<GameObject>())
                if (go != null && go.name.StartsWith("MoldSpore", System.StringComparison.Ordinal))
                    LethalMenuMod.MoldSpores.Add(go);
        }

        private static void CollectMineshaftElevators()
        {
            LethalMenuMod.MineshaftElevators.Clear();
            foreach (var e in Object.FindObjectsOfType<MineshaftElevatorController>())
                if (e != null) LethalMenuMod.MineshaftElevators.Add(e);
        }

        private static void CollectSpikeRoofTraps()
        {
            LethalMenuMod.SpikeRoofTraps.Clear();
            foreach (var go in Object.FindObjectsOfType<GameObject>())
                if (go != null && go.name.StartsWith("AnimContainer", System.StringComparison.Ordinal))
                    LethalMenuMod.SpikeRoofTraps.Add(go);
        }
    }
}
