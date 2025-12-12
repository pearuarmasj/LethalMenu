using UnityEngine;

namespace LethalMenu.Cheats
{
    public static partial class NetworkCheats
    {
        #region Landmines and Turrets

        /// Blow up all landmines on the map.
        public static void BlowUpAllLandmines()
        {
            var landmines = Object.FindObjectsOfType<Landmine>();
            if (landmines == null || landmines.Length == 0)
            {
                Debug.Log("[NetworkCheats] No landmines found.");
                return;
            }

            int count = 0;
            foreach (var mine in landmines)
            {
                if (mine != null && !mine.hasExploded)
                {
                    mine.ExplodeMineServerRpc();
                    count++;
                }
            }
            Debug.Log($"[NetworkCheats] Exploded {count} landmines.");
        }

        /// Toggle all landmines enabled/disabled state.
        public static void ToggleAllLandmines(bool enable)
        {
            var landmines = Object.FindObjectsOfType<Landmine>();
            if (landmines == null || landmines.Length == 0) return;

            foreach (var mine in landmines)
            {
                if (mine != null)
                {
                    mine.ToggleMine(enable);
                }
            }
            Debug.Log($"[NetworkCheats] Toggled {landmines.Length} landmines to {(enable ? "ON" : "OFF")}.");
        }

        /// Toggle all turrets enabled/disabled state.
        public static void ToggleAllTurrets(bool enable)
        {
            var turrets = Object.FindObjectsOfType<Turret>();
            if (turrets == null || turrets.Length == 0) return;

            foreach (var turret in turrets)
            {
                if (turret != null)
                {
                    turret.ToggleTurretEnabled(enable);
                }
            }
            Debug.Log($"[NetworkCheats] Toggled {turrets.Length} turrets to {(enable ? "ON" : "OFF")}.");
        }

        /// Makes all turrets go berserk mode (fires at everything).
        public static void BerserkAllTurrets()
        {
            var turrets = Object.FindObjectsOfType<Turret>();
            if (turrets == null || turrets.Length == 0) return;

            foreach (var turret in turrets)
            {
                if (turret != null)
                {
                    turret.SwitchTurretMode(3); // Berserk mode
                    turret.EnterBerserkModeServerRpc(-1);
                }
            }
            Debug.Log($"[NetworkCheats] {turrets.Length} turrets now berserk.");
        }

        #endregion

        #region Bridge and Structures

        /// Force the bridge to collapse.
        public static void ForceBridgeFall()
        {
            var bridge = Object.FindObjectOfType<BridgeTrigger>();
            if (bridge == null)
            {
                Debug.Log("[NetworkCheats] No bridge found.");
                return;
            }
            bridge.BridgeFallServerRpc();
            Debug.Log("[NetworkCheats] Bridge collapsed.");
        }

        /// Force the small bridge (type 2) to collapse.
        public static void ForceSmallBridgeFall()
        {
            var bridge = Object.FindObjectOfType<BridgeTriggerType2>();
            if (bridge == null)
            {
                Debug.Log("[NetworkCheats] No small bridge found.");
                return;
            }
            // Add instability until it falls
            for (int i = 0; i < 4; i++)
            {
                bridge.AddToBridgeInstabilityServerRpc();
            }
            Debug.Log("[NetworkCheats] Small bridge destabilized.");
        }

        #endregion
    }
}
