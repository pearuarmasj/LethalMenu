using System;
using System.Collections;
using System.Linq;
using GameNetcodeStuff;
using UnityEngine;

namespace LethalMenu.Cheats
{
    public static partial class NetworkCheats
    {
        #region Malicious Exploits (Use Responsibly)

        /// Lag ALL players using Bracken ownership spam.
        public static void BrackenLagAllPlayers()
        {
            var bracken = UnityEngine.Object.FindObjectOfType<FlowermanAI>();
            if (bracken == null)
            {
                Debug.Log("[NetworkCheats] No Bracken spawned.");
                return;
            }

            if (LethalMenuMod.Instance != null)
            {
                LethalMenuMod.Instance.StartCoroutine(BrackenLagAllCoroutine(bracken));
            }
        }

        private static IEnumerator BrackenLagAllCoroutine(FlowermanAI bracken)
        {
            var localPlayer = LethalMenuMod.LocalPlayer;
            if (localPlayer == null) yield break;

            var players = GetAllPlayers().Where(p => p != localPlayer).ToArray();
            if (players.Length == 0)
            {
                Debug.Log("[NetworkCheats] No other players to lag.");
                yield break;
            }

            // Cycle through all players, transferring Bracken ownership rapidly
            for (int cycle = 0; cycle < 5; cycle++)
            {
                foreach (var player in players)
                {
                    if (player == null || player.isPlayerDead) continue;

                    // Take ownership
                    bracken.ChangeEnemyOwnerServerRpc(localPlayer.actualClientId);
                    yield return null;

                    // Aggravate and teleport to player
                    bracken.SetMovingTowardsTargetPlayer(player);
                    bracken.EnterAngerModeServerRpc(float.MaxValue);
                    bracken.serverPosition = player.transform.position;
                    bracken.transform.position = player.transform.position;

                    // Transfer to target
                    bracken.ChangeEnemyOwnerServerRpc(player.actualClientId);
                    yield return new WaitForSeconds(0.1f);
                }
            }

            Debug.Log("[NetworkCheats] Bracken lag attack cycled through all players.");
        }

        /// Rapidly toggles ship lights to be annoying (uses coroutine).
        public static void FlickerShipLights()
        {
            if (LethalMenuMod.Instance != null)
            {
                LethalMenuMod.Instance.StartCoroutine(FlickerShipLightsCoroutine());
            }
        }

        /// Coroutine that flickers ship lights repeatedly.
        public static IEnumerator FlickerShipLightsCoroutine()
        {
            var shipLights = UnityEngine.Object.FindObjectOfType<ShipLights>();
            if (shipLights == null) yield break;

            // Flicker lights 30 times with visible delay
            for (int i = 0; i < 30; i++)
            {
                shipLights.SetShipLightsServerRpc(i % 2 == 0);
                yield return new WaitForSeconds(0.15f); // 150ms between toggles for visible flicker
            }

            Debug.Log("[NetworkCheats] Flickered ship lights.");
        }

        /// Kill all other players at once.
        public static void MassKillPlayers()
        {
            var players = GetAllPlayers();
            var localPlayer = LethalMenuMod.LocalPlayer;
            if (localPlayer == null) return;

            int killed = 0;
            foreach (var player in players)
            {
                if (player != null && player != localPlayer && !player.isPlayerDead)
                {
                    player.DamagePlayerFromOtherClientServerRpc(100, Vector3.up * 10f, (int)localPlayer.playerClientId);
                    killed++;
                }
            }

            Debug.Log($"[NetworkCheats] Mass killed {killed} players.");
        }

        /// Impersonate another player in chat.
        /// Sends message that appears to be from another player.
        public static void ImpersonateInChat(string message, int playerIndexToImpersonate)
        {
            var hud = HUDManager.Instance;
            if (hud == null) return;

            // Truncate to 50 characters
            if (message.Length > 50)
                message = message.Substring(0, 50);

            // Use the public method with the target player's index
            hud.AddTextToChatOnServer(message, playerIndexToImpersonate);
            Debug.Log($"[NetworkCheats] Sent message as player {playerIndexToImpersonate}.");
        }

        #endregion
    }
}
