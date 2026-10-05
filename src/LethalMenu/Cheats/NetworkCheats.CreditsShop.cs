using UnityEngine;

namespace LethalMenu.Cheats
{
    public static partial class NetworkCheats
    {
        #region Credits & Shop Exploits

        /// Sets group credits to any value by calling SyncGroupCreditsServerRpc.
        public static void SetCredits(int amount)
        {
            var terminal = LethalMenuMod.GameTerminal;
            if (terminal == null)
            {
                Debug.Log("[NetworkCheats] Terminal not found.");
                return;
            }

            terminal.groupCredits = amount;
            terminal.SyncGroupCreditsServerRpc(amount, terminal.numberOfItemsInDropship);
            Debug.Log($"[NetworkCheats] Set credits to ${amount}");
        }

        #endregion
    }
}
