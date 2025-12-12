using System;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LethalMenu.Cheats
{
    public static partial class NetworkCheats
    {
        #region Free Vehicles (RequireOwnership = false)

        /// Buys a vehicle for free.
        /// Uses Terminal.BuyVehicleServerRpc - RequireOwnership = false.
        public static void BuyFreeVehicle(int vehicleId)
        {
            var terminal = Object.FindObjectOfType<Terminal>();
            if (terminal == null)
            {
                Debug.Log("[NetworkCheats] Terminal not found.");
                return;
            }

            // Buy vehicle but keep current credits
            terminal.BuyVehicleServerRpc(vehicleId, terminal.groupCredits, false);
            Debug.Log($"[NetworkCheats] Bought vehicle ID {vehicleId} for free.");
        }

        /// Gets list of available vehicles.
        public static (int id, string name)[] GetAvailableVehicles()
        {
            var terminal = Object.FindObjectOfType<Terminal>();
            if (terminal == null || terminal.buyableVehicles == null)
                return Array.Empty<(int, string)>();

            var result = new (int, string)[terminal.buyableVehicles.Length];
            for (int i = 0; i < terminal.buyableVehicles.Length; i++)
            {
                var v = terminal.buyableVehicles[i];
                result[i] = (i, v?.vehicleDisplayName ?? $"Vehicle {i}");
            }
            return result;
        }

        #endregion

        #region Vehicle Control

        /// Toggle car horn on all vehicles.
        public static void ToggleCarHorns(bool on)
        {
            var vehicles = Object.FindObjectsOfType<VehicleController>();
            if (vehicles == null || vehicles.Length == 0)
            {
                Debug.Log("[NetworkCheats] No vehicles found.");
                return;
            }

            var localPlayer = LethalMenuMod.LocalPlayer;
            int playerId = localPlayer != null ? (int)localPlayer.playerClientId : -1;

            foreach (var vehicle in vehicles)
            {
                if (vehicle != null)
                {
                    vehicle.SetHonkServerRpc(on, playerId);
                }
            }
            Debug.Log($"[NetworkCheats] Vehicle horns {(on ? "ON" : "OFF")}.");
        }

        /// Spam car horns on/off rapidly.
        public static void SpamCarHorns(int iterations = 15)
        {
            if (LethalMenuMod.Instance != null)
            {
                LethalMenuMod.Instance.StartCoroutine(SpamCarHornsCoroutine(iterations));
            }
        }

        private static System.Collections.IEnumerator SpamCarHornsCoroutine(int iterations)
        {
            var vehicles = Object.FindObjectsOfType<VehicleController>();
            if (vehicles == null || vehicles.Length == 0) yield break;

            var localPlayer = LethalMenuMod.LocalPlayer;
            int playerId = localPlayer != null ? (int)localPlayer.playerClientId : -1;

            for (int i = 0; i < iterations; i++)
            {
                foreach (var vehicle in vehicles)
                {
                    if (vehicle != null)
                    {
                        vehicle.SetHonkServerRpc(i % 2 == 0, playerId);
                    }
                }
                yield return new WaitForSeconds(0.2f);
            }
            Debug.Log($"[NetworkCheats] Spammed car horns {iterations} times.");
        }

        /// Explode all vehicles (Cruisers).
        public static void ExplodeAllVehicles()
        {
            var vehicles = Object.FindObjectsOfType<VehicleController>();
            if (vehicles == null || vehicles.Length == 0)
            {
                Debug.Log("[NetworkCheats] No vehicles found.");
                return;
            }

            foreach (var vehicle in vehicles)
            {
                if (vehicle != null)
                {
                    vehicle.DestroyCarServerRpc(-1);
                }
            }
            Debug.Log($"[NetworkCheats] Exploded {vehicles.Length} vehicles.");
        }

        /// Hijack/take control of a vehicle. Uses SetPlayerInControlOfVehicleServerRpc (RequireOwnership=false).
        public static void HijackVehicle(VehicleController? vehicle = null)
        {
            var localPlayer = LethalMenuMod.LocalPlayer;
            if (localPlayer == null)
            {
                Debug.Log("[NetworkCheats] HijackVehicle: Local player is null.");
                return;
            }

            // If no specific vehicle, find the first one
            if (vehicle == null)
            {
                var vehicles = Object.FindObjectsOfType<VehicleController>();
                if (vehicles == null || vehicles.Length == 0)
                {
                    Debug.Log("[NetworkCheats] HijackVehicle: No vehicles found.");
                    HUDManager.Instance?.DisplayTip("Vehicle", "No vehicles found on map.");
                    return;
                }
                vehicle = vehicles[0];
            }

            // Call the ServerRpc to take control - this has RequireOwnership = false
            vehicle.SetPlayerInControlOfVehicleServerRpc((int)localPlayer.playerClientId);
            Debug.Log("[NetworkCheats] Hijacked vehicle.");
            HUDManager.Instance?.DisplayTip("Vehicle", "Took control of vehicle.");
        }

        /// Kick the current driver out of a vehicle.
        public static void EjectVehicleDriver(VehicleController? vehicle = null)
        {
            if (vehicle == null)
            {
                var vehicles = Object.FindObjectsOfType<VehicleController>();
                if (vehicles == null || vehicles.Length == 0)
                {
                    Debug.Log("[NetworkCheats] EjectVehicleDriver: No vehicles found.");
                    return;
                }
                vehicle = vehicles.FirstOrDefault(v => v.currentDriver != null);
            }

            if (vehicle == null || vehicle.currentDriver == null)
            {
                Debug.Log("[NetworkCheats] EjectVehicleDriver: No vehicle with driver found.");
                HUDManager.Instance?.DisplayTip("Vehicle", "No vehicle with driver found.");
                return;
            }

            int driverId = (int)vehicle.currentDriver.playerClientId;
            // Force remove player from control
            vehicle.RemovePlayerControlOfVehicleServerRpc(driverId, vehicle.transform.position, vehicle.transform.rotation, false);
            Debug.Log("[NetworkCheats] Ejected driver from vehicle.");
            HUDManager.Instance?.DisplayTip("Vehicle", "Ejected driver from vehicle.");
        }

        /// Add turbo boosts to a vehicle.
        public static void AddVehicleTurbo(int turboAmount = 5, VehicleController? vehicle = null)
        {
            var localPlayer = LethalMenuMod.LocalPlayer;
            if (localPlayer == null) return;

            if (vehicle == null)
            {
                var vehicles = Object.FindObjectsOfType<VehicleController>();
                if (vehicles == null || vehicles.Length == 0)
                {
                    Debug.Log("[NetworkCheats] AddVehicleTurbo: No vehicles found.");
                    HUDManager.Instance?.DisplayTip("Vehicle", "No vehicles found.");
                    return;
                }
                vehicle = vehicles[0];
            }

            // Call the ServerRpc to add turbo - RequireOwnership = false
            vehicle.AddTurboBoostServerRpc((int)localPlayer.playerClientId, turboAmount);
            Debug.Log($"[NetworkCheats] Added {turboAmount} turbo boosts to vehicle.");
            HUDManager.Instance?.DisplayTip("Vehicle", $"Added {turboAmount} turbo boosts.");
        }

        /// Use turbo boost on a vehicle.
        public static void UseVehicleTurbo(VehicleController? vehicle = null)
        {
            if (vehicle == null)
            {
                var vehicles = Object.FindObjectsOfType<VehicleController>();
                if (vehicles == null || vehicles.Length == 0)
                {
                    Debug.Log("[NetworkCheats] UseVehicleTurbo: No vehicles found.");
                    return;
                }
                vehicle = vehicles[0];
            }

            vehicle.UseTurboBoostServerRpc();
            Debug.Log("[NetworkCheats] Used turbo boost.");
        }

        /// Damage vehicle engine / add oil to repair.
        public static void SetVehicleEngineHealth(int hp, VehicleController? vehicle = null)
        {
            var localPlayer = LethalMenuMod.LocalPlayer;
            if (localPlayer == null) return;

            if (vehicle == null)
            {
                var vehicles = Object.FindObjectsOfType<VehicleController>();
                if (vehicles == null || vehicles.Length == 0)
                {
                    Debug.Log("[NetworkCheats] SetVehicleEngineHealth: No vehicles found.");
                    return;
                }
                vehicle = vehicles[0];
            }

            // AddEngineOilServerRpc sets the HP directly
            vehicle.AddEngineOilServerRpc((int)localPlayer.playerClientId, hp);
            Debug.Log($"[NetworkCheats] Set vehicle engine HP to {hp}.");
            HUDManager.Instance?.DisplayTip("Vehicle", $"Engine HP set to {hp}.");
        }

        /// Kill vehicle engine (set HP to 0).
        public static void KillVehicleEngine(VehicleController? vehicle = null)
        {
            SetVehicleEngineHealth(0, vehicle);
            HUDManager.Instance?.DisplayTip("Vehicle", "Engine killed.");
        }

        /// Fully repair vehicle engine.
        public static void RepairVehicleEngine(VehicleController? vehicle = null)
        {
            SetVehicleEngineHealth(100, vehicle);
            HUDManager.Instance?.DisplayTip("Vehicle", "Engine fully repaired.");
        }

        /// Shift vehicle gear remotely.
        public static void ShiftVehicleGear(int gear, VehicleController? vehicle = null)
        {
            var localPlayer = LethalMenuMod.LocalPlayer;
            if (localPlayer == null) return;

            if (vehicle == null)
            {
                var vehicles = Object.FindObjectsOfType<VehicleController>();
                if (vehicles == null || vehicles.Length == 0)
                {
                    Debug.Log("[NetworkCheats] ShiftVehicleGear: No vehicles found.");
                    return;
                }
                vehicle = vehicles[0];
            }

            // Gear: 0=Park, 1=Drive, 2=Reverse
            vehicle.ShiftToGearServerRpc(gear, (int)localPlayer.playerClientId);
            string gearName = gear switch { 0 => "Park", 1 => "Drive", 2 => "Reverse", _ => "Unknown" };
            Debug.Log($"[NetworkCheats] Shifted vehicle to gear {gear} ({gearName}).");
            HUDManager.Instance?.DisplayTip("Vehicle", $"Gear: {gearName}");
        }

        /// Set vehicle radio station.
        public static void SetVehicleRadio(int station, int quality = 100, VehicleController? vehicle = null)
        {
            if (vehicle == null)
            {
                var vehicles = Object.FindObjectsOfType<VehicleController>();
                if (vehicles == null || vehicles.Length == 0)
                {
                    Debug.Log("[NetworkCheats] SetVehicleRadio: No vehicles found.");
                    return;
                }
                vehicle = vehicles[0];
            }

            vehicle.SetRadioStationServerRpc(station, quality);
            Debug.Log($"[NetworkCheats] Set vehicle radio to station {station}.");
        }

        #endregion

        #region Jetpacks

        /// Explode all jetpacks.
        public static void ExplodeAllJetpacks()
        {
            var items = Object.FindObjectsOfType<JetpackItem>();
            if (items == null || items.Length == 0)
            {
                Debug.Log("[NetworkCheats] No jetpacks found.");
                return;
            }

            int count = 0;
            foreach (var jetpack in items)
            {
                if (jetpack != null && !jetpack.isHeld)
                {
                    jetpack.ExplodeJetpackServerRpc();
                    count++;
                }
            }
            Debug.Log($"[NetworkCheats] Exploded {count} jetpacks.");
        }

        #endregion
    }
}
