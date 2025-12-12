using UnityEngine;

namespace LethalMenu.Cheats
{
    public static partial class NetworkCheats
    {
        #region Terminal Audio

        /// Plays terminal audio for everyone (trolling).
        public static void PlayTerminalSound(int clipIndex)
        {
            var terminal = UnityEngine.Object.FindObjectOfType<Terminal>();
            if (terminal == null)
            {
                Debug.Log("[NetworkCheats] Terminal not found.");
                return;
            }

            terminal.PlayTerminalAudioServerRpc(clipIndex);
            Debug.Log($"[NetworkCheats] Playing terminal sound {clipIndex}.");
        }

        #endregion
    }
}
