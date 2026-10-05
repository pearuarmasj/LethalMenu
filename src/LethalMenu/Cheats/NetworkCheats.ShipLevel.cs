using UnityEngine;

namespace LethalMenu.Cheats
{
    public static partial class NetworkCheats
    {
        #region Ship & Level Control

        /// Forces ship to leave early using SetShipLeaveEarlyServerRpc.
        public static void ForceShipLeave()
        {
            var timeOfDay = TimeOfDay.Instance;
            if (timeOfDay == null)
            {
                Debug.Log("[NetworkCheats] TimeOfDay not found.");
                return;
            }

            timeOfDay.SetShipLeaveEarlyServerRpc();
            Debug.Log("[NetworkCheats] Forced ship to leave early.");
        }

        /// Toggles ship lights on/off for everyone.
        public static void ToggleShipLights(bool on)
        {
            var shipLights = UnityEngine.Object.FindObjectOfType<ShipLights>();
            if (shipLights == null)
            {
                Debug.Log("[NetworkCheats] ShipLights not found.");
                return;
            }

            shipLights.SetShipLightsServerRpc(on);
            Debug.Log($"[NetworkCheats] Ship lights set to {(on ? "ON" : "OFF")}.");
        }

        /// Forces all players to eject/leave (usually used when in orbit).
        public static void EjectAllPlayers()
        {
            var startOfRound = StartOfRound.Instance;
            if (startOfRound == null)
            {
                Debug.Log("[NetworkCheats] Not in game.");
                return;
            }

            startOfRound.ManuallyEjectPlayersServerRpc();
            Debug.Log("[NetworkCheats] Ejecting all players.");
        }

        /// Toggles the ship's magnet on/off.
        public static void ToggleMagnet(bool on)
        {
            var startOfRound = StartOfRound.Instance;
            if (startOfRound == null)
            {
                Debug.Log("[NetworkCheats] Not in game.");
                return;
            }

            startOfRound.SetMagnetOnServerRpc(on);
            Debug.Log($"[NetworkCheats] Magnet set to {(on ? "ON" : "OFF")}.");
        }

        #endregion
    }
}
