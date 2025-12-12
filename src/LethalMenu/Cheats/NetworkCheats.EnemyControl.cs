using System;
using UnityEngine;

namespace LethalMenu.Cheats
{
    public static partial class NetworkCheats
    {
        #region Enemy Control

        /// Kills an enemy remotely using KillEnemyServerRpc.
        public static void KillEnemy(EnemyAI enemy)
        {
            if (enemy == null || enemy.isEnemyDead) return;

            try
            {
                enemy.KillEnemyServerRpc(true);
                Debug.Log($"[NetworkCheats] Killed enemy: {enemy.enemyType?.enemyName ?? "Unknown"}");
            }
            catch (Exception e)
            {
                Debug.Log($"[NetworkCheats] Failed to kill enemy: {e.Message}");
            }
        }

        /// Kills all enemies on the map.
        public static void KillAllEnemies()
        {
            int killed = 0;
            foreach (var enemy in LethalMenuMod.Enemies)
            {
                if (enemy != null && !enemy.isEnemyDead)
                {
                    try
                    {
                        enemy.KillEnemyServerRpc(true);
                        killed++;
                    }
                    catch { }
                }
            }
            Debug.Log($"[NetworkCheats] Killed {killed} enemies.");
        }

        /// Stun all enemies on the map.
        public static void StunAllEnemies()
        {
            var localPlayer = LethalMenuMod.LocalPlayer;
            if (localPlayer == null) return;

            int count = 0;
            foreach (var enemy in LethalMenuMod.Enemies)
            {
                if (enemy != null && !enemy.isEnemyDead)
                {
                    enemy.SetEnemyStunned(true, 5f, localPlayer);
                    count++;
                }
            }
            Debug.Log($"[NetworkCheats] Stunned {count} enemies.");
        }

        #endregion
    }
}
