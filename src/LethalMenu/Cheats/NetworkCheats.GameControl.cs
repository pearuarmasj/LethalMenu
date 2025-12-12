using System.Collections;
using UnityEngine;

namespace LethalMenu.Cheats
{
    public static partial class NetworkCheats
    {
        #region Game Control (RequireOwnership = false)

        /// Forces the game to start (launches ship).
        /// Uses StartOfRound.StartGameServerRpc - RequireOwnership = false.
        public static void ForceStartGame()
        {
            var startOfRound = StartOfRound.Instance;
            if (startOfRound == null)
            {
                Debug.Log("[NetworkCheats] Not in game.");
                return;
            }

            startOfRound.StartGameServerRpc();
            Debug.Log("[NetworkCheats] Force started game.");
        }

        /// Forces the game to end (returns to orbit).
        /// Uses StartOfRound.EndGameServerRpc - RequireOwnership = false.
        public static void ForceEndGame()
        {
            var startOfRound = StartOfRound.Instance;
            if (startOfRound == null)
            {
                Debug.Log("[NetworkCheats] Not in game.");
                return;
            }

            var localPlayer = LethalMenuMod.LocalPlayer;
            int playerId = localPlayer != null ? (int)localPlayer.playerClientId : 0;

            startOfRound.EndGameServerRpc(playerId);
            Debug.Log("[NetworkCheats] Force ended game.");
        }

        /// Opens or closes the ship doors.
        /// Uses StartOfRound.SetDoorsClosedServerRpc - RequireOwnership = false.
        /// Also plays the local animation via HangarShipDoor.
        public static void SetShipDoors(bool closed)
        {
            var startOfRound = StartOfRound.Instance;
            if (startOfRound == null)
            {
                Debug.Log("[NetworkCheats] Not in game.");
                return;
            }

            // Call the server RPC to sync state across network
            startOfRound.SetDoorsClosedServerRpc(closed);

            // Also play the animation locally (the RPC only sets the state, not the visual)
            var hangarDoor = Object.FindObjectOfType<HangarShipDoor>();
            if (hangarDoor != null)
            {
                // PlayDoorAnimation requires buttonsEnabled, so set animator directly
                hangarDoor.shipDoorsAnimator?.SetBool("Closed", closed);
            }

            Debug.Log($"[NetworkCheats] Ship doors {(closed ? "closed" : "opened")}.");
        }

        /// Spams ship doors open/close (annoying visual effect).
        public static void SpamShipDoors(int iterations = 10)
        {
            if (LethalMenuMod.Instance != null)
            {
                LethalMenuMod.Instance.StartCoroutine(SpamShipDoorsCoroutine(iterations));
            }
        }

        private static IEnumerator SpamShipDoorsCoroutine(int iterations)
        {
            var startOfRound = StartOfRound.Instance;
            if (startOfRound == null) yield break;

            var hangarDoor = Object.FindObjectOfType<HangarShipDoor>();

            for (int i = 0; i < iterations; i++)
            {
                bool closed = (i % 2 == 0);
                startOfRound.SetDoorsClosedServerRpc(closed);
                hangarDoor?.shipDoorsAnimator?.SetBool("Closed", closed);
                yield return new WaitForSeconds(0.5f);
            }

            Debug.Log($"[NetworkCheats] Spammed ship doors {iterations} times.");
        }

        /// Overheat ship doors - locks them via the overheat mechanism (like when Big Kiwi pries them).
        /// Uses StartOfRound.SetShipDoorsOverheatServerRpc - RequireOwnership = false.
        public static void OverheatShipDoors()
        {
            var startOfRound = StartOfRound.Instance;
            if (startOfRound == null)
            {
                Debug.Log("[NetworkCheats] OverheatShipDoors: Not in game.");
                HUDManager.Instance?.DisplayTip("Ship Doors", "Not in game.");
                return;
            }

            // This calls SetShipDoorsOverheatServerRpc which has RequireOwnership = false
            // It opens doors and sets overheated state, making them unusable
            startOfRound.SetShipDoorsOverheatServerRpc();
            Debug.Log("[NetworkCheats] Overheated ship doors.");
            HUDManager.Instance?.DisplayTip("Ship Doors", "Ship doors overheated and locked open.");
        }

        #endregion

        #region Combined Chaos (Multiple exploits at once)

        /// Triggers maximum chaos: horn spam, light flicker, door spam, signal spam.
        public static void MaxChaos()
        {
            if (LethalMenuMod.Instance != null)
            {
                LethalMenuMod.Instance.StartCoroutine(MaxChaosCoroutine());
            }
        }

        private static IEnumerator MaxChaosCoroutine()
        {
            Debug.Log("[NetworkCheats] MAXIMUM CHAOS ENGAGED!");

            // Start all chaos coroutines simultaneously
            var instance = LethalMenuMod.Instance;
            if (instance == null) yield break;
            instance.StartCoroutine(SpamShipHornCoroutine(10));
            instance.StartCoroutine(FlickerShipLightsCoroutine());
            instance.StartCoroutine(SpamShipDoorsCoroutine(8));
            instance.StartCoroutine(SpamSignalTranslatorCoroutine(15));

            yield return new WaitForSeconds(6f);
            Debug.Log("[NetworkCheats] Chaos complete.");
        }

        #endregion
    }
}
