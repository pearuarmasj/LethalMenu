using System.Collections;
using UnityEngine;

namespace LethalMenu.Cheats
{
    public static partial class NetworkCheats
    {
        #region Factory Control

        /// Force the company deposit desk tentacle attack.
        public static void ForceTentacleAttack()
        {
            var desk = Object.FindObjectOfType<DepositItemsDesk>();
            if (desk == null)
            {
                Debug.Log("[NetworkCheats] Deposit desk not found.");
                return;
            }

            desk.AttackPlayersServerRpc();
            Debug.Log("[NetworkCheats] Tentacle attack triggered.");
        }

        #endregion
    }
}
