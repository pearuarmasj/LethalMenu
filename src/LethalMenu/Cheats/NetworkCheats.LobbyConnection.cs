using System;
using System.Text.RegularExpressions;
using UnityEngine;

namespace LethalMenu.Cheats
{
    public static partial class NetworkCheats
    {
        #region Lobby Connection

        /// Disconnect from the current lobby cleanly.
        public static void DisconnectFromLobby()
        {
            var manager = GameNetworkManager.Instance;
            if (manager == null)
            {
                HUDManager.Instance?.DisplayTip("Disconnect", "No GameNetworkManager.");
                return;
            }

            try
            {
                manager.Disconnect();
                HUDManager.Instance?.DisplayTip("Disconnect", "Disconnected from lobby.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkCheats] Disconnect failed: {e.Message}");
            }
        }

        /// Reconnect to a Steam lobby using a 17-digit Steam ID copied to the clipboard.
        public static void ReconnectFromClipboard()
        {
            string clip = GUIUtility.systemCopyBuffer;
            if (string.IsNullOrWhiteSpace(clip) || !Regex.IsMatch(clip, @"^\d{17}$"))
            {
                HUDManager.Instance?.DisplayTip("Reconnect", "Clipboard does not contain a 17-digit Steam ID.");
                return;
            }

            if (!ulong.TryParse(clip, out ulong id)) return;

            var manager = GameNetworkManager.Instance;
            if (manager == null)
            {
                HUDManager.Instance?.DisplayTip("Reconnect", "No GameNetworkManager.");
                return;
            }

            try
            {
                manager.StartClient(new Steamworks.SteamId { Value = id });
                HUDManager.Instance?.DisplayTip("Reconnect", $"Connecting to {id}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkCheats] Reconnect failed: {e.Message}");
            }
        }

        #endregion
    }
}
