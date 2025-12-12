using System;
using GameNetcodeStuff;
using UnityEngine;

namespace LethalMenu.Cheats
{
    public static partial class NetworkCheats
    {
        #region Fake Death

        /// Makes you appear dead to other players while staying alive locally.
        /// Calls KillPlayerServerRpc without spawning body.
        /// You can still move around but others see you as dead.
        /// Note: You will actually die when the ship leaves.
        public static void FakeDeath()
        {
            var localPlayer = LethalMenuMod.LocalPlayer;
            if (localPlayer == null)
            {
                Debug.Log("[NetworkCheats] FakeDeath: Local player not found.");
                return;
            }

            if (localPlayer.isPlayerDead)
            {
                Debug.Log("[NetworkCheats] FakeDeath: Player is already dead.");
                return;
            }

            try
            {
                // Enable fake death mode in settings (used by patches to prevent actual death)
                Settings.FakeDeath = true;

                // Call the kill RPC with spawnBody = false (no ragdoll)
                // Other players will receive this and see you as dead
                localPlayer.KillPlayerServerRpc(
                    (int)localPlayer.playerClientId,
                    false, // spawnBody = false (don't spawn a ragdoll)
                    Vector3.zero,
                    (int)CauseOfDeath.Unknown,
                    0, // deathAnimation
                    Vector3.zero
                );

                Debug.Log("[NetworkCheats] FakeDeath: Death broadcasted to other players. You appear dead but can still move.");
                HUDManager.Instance?.DisplayTip("Fake Death", "Other players think you're dead!\nYou'll actually die when ship leaves.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetworkCheats] FakeDeath failed: {ex.Message}");
                Settings.FakeDeath = false;
            }
        }

        /// Cancels fake death mode. Use SelfRevive if you actually died.
        public static void CancelFakeDeath()
        {
            Settings.FakeDeath = false;
            Debug.Log("[NetworkCheats] FakeDeath cancelled.");
        }

        #endregion
    }
}
