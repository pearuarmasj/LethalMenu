using UnityEngine;

namespace LethalMenu.Cheats
{
    public static partial class NetworkCheats
    {
        #region Credits & Shop Exploits

        /// Sets group credits to any value by calling SyncGroupCreditsServerRpc.
        public static void SetCredits(int amount)
        {
            var terminal = UnityEngine.Object.FindObjectOfType<Terminal>();
            if (terminal == null)
            {
                Debug.Log("[NetworkCheats] Terminal not found.");
                return;
            }

            terminal.groupCredits = amount;
            terminal.SyncGroupCreditsServerRpc(amount, terminal.numberOfItemsInDropship);
            Debug.Log($"[NetworkCheats] Set credits to ${amount}");
        }

        /// Unlocks a ship upgrade for free by calling BuyShipUnlockableServerRpc with current credits.
        public static void UnlockShipUpgrade(int unlockableId)
        {
            var startOfRound = StartOfRound.Instance;
            var terminal = UnityEngine.Object.FindObjectOfType<Terminal>();
            if (startOfRound == null || terminal == null)
            {
                Debug.Log("[NetworkCheats] Not in game.");
                return;
            }

            // Buy the unlockable but keep current credits
            startOfRound.BuyShipUnlockableServerRpc(unlockableId, terminal.groupCredits);
            Debug.Log($"[NetworkCheats] Unlocked ship upgrade ID: {unlockableId}");
        }

        /// Buys items without spending credits.
        public static void FreeItems(int[] itemIds)
        {
            var terminal = UnityEngine.Object.FindObjectOfType<Terminal>();
            if (terminal == null)
            {
                Debug.Log("[NetworkCheats] Terminal not found.");
                return;
            }

            terminal.BuyItemsServerRpc(itemIds, terminal.groupCredits, 0);
            Debug.Log($"[NetworkCheats] Purchased {itemIds.Length} items for free.");
        }

        #endregion
    }
}
