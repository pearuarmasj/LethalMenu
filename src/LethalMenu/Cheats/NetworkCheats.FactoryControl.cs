using System.Collections;
using UnityEngine;

namespace LethalMenu.Cheats
{
    public static partial class NetworkCheats
    {
        #region Factory Control

        /// Toggle factory/facility lights.
        public static void ToggleFactoryLights()
        {
            var breaker = Object.FindObjectOfType<BreakerBox>();
            if (breaker == null)
            {
                Debug.Log("[NetworkCheats] Breaker box not found.");
                return;
            }

            var roundManager = RoundManager.Instance;
            if (roundManager == null) return;

            // Toggle power
            roundManager.SwitchPower(!breaker.isPowerOn);
            Debug.Log($"[NetworkCheats] Factory lights {(breaker.isPowerOn ? "ON" : "OFF")}.");
        }

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

        /// Spam the deposit desk door open/close.
        public static void SpamDepositDeskDoor(int iterations = 20)
        {
            if (LethalMenuMod.Instance != null)
            {
                LethalMenuMod.Instance.StartCoroutine(SpamDepositDeskDoorCoroutine(iterations));
            }
        }

        private static IEnumerator SpamDepositDeskDoorCoroutine(int iterations)
        {
            var desk = Object.FindObjectOfType<DepositItemsDesk>();
            if (desk == null) yield break;

            for (int i = 0; i < iterations; i++)
            {
                desk.OpenShutDoorClientRpc();
                yield return new WaitForSeconds(0.2f);
            }
            Debug.Log($"[NetworkCheats] Spammed deposit desk door {iterations} times.");
        }

        #endregion
    }
}
