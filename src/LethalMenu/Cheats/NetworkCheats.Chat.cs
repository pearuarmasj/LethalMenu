using System.Collections;
using UnityEngine;

namespace LethalMenu.Cheats
{
    public static partial class NetworkCheats
    {
        #region Chat Exploits

        /// Sends a chat message as any player (spoofed chat).
        /// Uses the public AddTextToChatOnServer method (like lc-hax).
        /// Max 50 characters enforced by server.
        public static void SendChatMessage(string message, int playerIndex = -1)
        {
            var hud = HUDManager.Instance;
            if (hud == null)
            {
                Debug.Log("[NetworkCheats] HUD not found.");
                return;
            }

            // If no player index specified, use local player
            if (playerIndex < 0)
            {
                var local = LethalMenuMod.LocalPlayer;
                if (local != null)
                    playerIndex = (int)local.playerClientId;
            }

            // Truncate to 50 characters (server limit)
            if (message.Length > 50)
                message = message.Substring(0, 50);

            // Use PUBLIC method AddTextToChatOnServer (like lc-hax does)
            // This is the proper way to send chat, calls ServerRpc internally
            hud.AddTextToChatOnServer(message, playerIndex);
            Debug.Log($"[NetworkCheats] Sent chat as player {playerIndex}: {message}");
        }

        /// Sends a system text message (no player name attached).
        public static void SendSystemMessage(string message)
        {
            var hud = HUDManager.Instance;
            if (hud == null)
            {
                Debug.Log("[NetworkCheats] HUD not found.");
                return;
            }

            // playerId = -1 sends as system message (no player name)
            hud.AddTextToChatOnServer(message, -1);
            Debug.Log($"[NetworkCheats] Sent system message: {message}");
        }

        /// Spam system messages (uses coroutine for proper timing).
        /// Each message must be unique to avoid deduplication.
        public static void SpamSystemMessage(string message, int count = 10)
        {
            if (LethalMenuMod.Instance != null)
            {
                LethalMenuMod.Instance.StartCoroutine(SpamSystemMessageCoroutine(message, count));
            }
        }

        /// Coroutine that sends system message spam with unique suffixes.
        public static IEnumerator SpamSystemMessageCoroutine(string message, int count)
        {
            var hud = HUDManager.Instance;
            if (hud == null) yield break;

            // Calculate max base message length
            int maxSuffixLen = $" [{count}]".Length;
            int maxBaseLen = 50 - maxSuffixLen;
            string baseMsg = message.Length > maxBaseLen ? message.Substring(0, maxBaseLen) : message;

            for (int i = 0; i < count; i++)
            {
                // Each message MUST be unique or server may dedupe
                string uniqueMsg = $"{baseMsg} [{i+1}]";
                hud.AddTextToChatOnServer(uniqueMsg, -1); // -1 = system message

                // Small delay to avoid rate limiting
                yield return new WaitForSeconds(0.15f);
            }
            Debug.Log($"[NetworkCheats] Spammed system messages {count} times.");
        }

        /// Spam chat messages (uses coroutine for proper timing).
        /// Each message must be unique to avoid deduplication.
        public static void SpamChat(string message, int count = 10)
        {
            if (LethalMenuMod.Instance != null)
            {
                LethalMenuMod.Instance.StartCoroutine(SpamChatCoroutine(message, count));
            }
        }

        /// Coroutine that sends spam messages with unique suffixes.
        public static IEnumerator SpamChatCoroutine(string message, int count)
        {
            var hud = HUDManager.Instance;
            if (hud == null) yield break;

            var local = LethalMenuMod.LocalPlayer;
            int playerIndex = local != null ? (int)local.playerClientId : 0;

            // Calculate max base message length (50 - max suffix length)
            int maxSuffixLen = $" [{count}]".Length;
            int maxBaseLen = 50 - maxSuffixLen;
            string baseMsg = message.Length > maxBaseLen ? message.Substring(0, maxBaseLen) : message;

            for (int i = 0; i < count; i++)
            {
                // Each message MUST be unique or server may dedupe
                string uniqueMsg = $"{baseMsg} [{i+1}]";
                hud.AddTextToChatOnServer(uniqueMsg, playerIndex);

                // Small delay to avoid rate limiting
                yield return new WaitForSeconds(0.15f);
            }
            Debug.Log($"[NetworkCheats] Spammed chat {count} times.");
        }

        /// Max spam - sends 50 messages very fast.
        public static void SpamChatMax(string message)
        {
            SpamChat(message, 50);
        }

        #endregion
    }
}
