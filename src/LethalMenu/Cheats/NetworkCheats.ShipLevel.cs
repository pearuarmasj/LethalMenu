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

        /// Changes destination to a different moon (requires host or appropriate permissions).
        public static void ChangeLevel(int levelId)
        {
            var startOfRound = StartOfRound.Instance;
            var terminal = UnityEngine.Object.FindObjectOfType<Terminal>();
            if (startOfRound == null || terminal == null)
            {
                Debug.Log("[NetworkCheats] Not in game.");
                return;
            }

            startOfRound.ChangeLevelServerRpc(levelId, terminal.groupCredits);
            Debug.Log($"[NetworkCheats] Changing to level ID: {levelId}");
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
