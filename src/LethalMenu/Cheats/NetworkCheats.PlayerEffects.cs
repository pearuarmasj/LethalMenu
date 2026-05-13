using System;
using GameNetcodeStuff;
using UnityEngine;

namespace LethalMenu.Cheats
{
    public static partial class NetworkCheats
    {
        #region Player Effects (Malicious)

        /// Damages another player using DamagePlayerFromOtherClientServerRpc.
        public static void DamagePlayer(PlayerControllerB target, int damage = 10)
        {
            if (target == null || target.isPlayerDead) return;

            var localPlayer = LethalMenuMod.LocalPlayer;
            if (localPlayer == null) return;

            // Call the server RPC to damage the player
            target.DamagePlayerFromOtherClientServerRpc(damage, Vector3.zero, (int)localPlayer.playerClientId);
            Debug.Log($"[NetworkCheats] Damaged {target.playerUsername} for {damage} damage.");
        }

        /// Forces a player to drop all held items using DropAllHeldItemsServerRpc.
        public static void ForceDropItems(PlayerControllerB target)
        {
            if (target == null || target.isPlayerDead) return;

            // Need to call this on the player's controller
            // Since we can't call ServerRpc directly on other players' objects,
            // we attempt via reflection or use alternative methods
            try
            {
                // Direct call attempt - may only work if we have permission
                target.DropAllHeldItemsAndSyncNonexact();
                Debug.Log($"[NetworkCheats] Forced {target.playerUsername} to drop items.");
            }
            catch (Exception e)
            {
                Debug.Log($"[NetworkCheats] Failed to force drop: {e.Message}");
            }
        }

        /// Heals a player to full health. Works on ANY player using negative damage exploit.
        /// If host: Uses DamagePlayerServerRpc to set health directly.
        /// If client: Uses DamagePlayerFromOtherClientServerRpc with -100 damage.
        public static void HealPlayer(PlayerControllerB target)
        {
            if (target == null || target.isPlayerDead)
            {
                Debug.Log("[NetworkCheats] HealPlayer: Target is null or dead.");
                return;
            }

            var localPlayer = LethalMenuMod.LocalPlayer;
            if (localPlayer == null)
            {
                Debug.Log("[NetworkCheats] HealPlayer: Local player not found.");
                return;
            }

            bool isHost = GameNetworkManager.Instance?.isHostingGame == true || localPlayer.IsServer;

            if (isHost)
            {
                // As host, use DamagePlayerServerRpc(0, 100) to set health to 100
                target.DamagePlayerServerRpc(0, 100);
                Debug.Log($"[NetworkCheats] Healed {target.playerUsername} via DamagePlayerServerRpc (host).");
            }
            else
            {
                // As client, use negative damage to heal
                // DamagePlayerFromOtherClientServerRpc with -100 damage = +100 health
                target.DamagePlayerFromOtherClientServerRpc(-100, UnityEngine.Vector3.zero, (int)localPlayer.playerClientId);
                Debug.Log($"[NetworkCheats] Healed {target.playerUsername} via negative damage exploit.");
            }
        }

        /// Heals the local player to full health.
        public static void HealSelf()
        {
            var localPlayer = LethalMenuMod.LocalPlayer;
            if (localPlayer == null || localPlayer.isPlayerDead)
            {
                Debug.Log("[NetworkCheats] HealSelf: Local player null or dead.");
                return;
            }

            HealPlayer(localPlayer);
        }

        #endregion
    }
}
