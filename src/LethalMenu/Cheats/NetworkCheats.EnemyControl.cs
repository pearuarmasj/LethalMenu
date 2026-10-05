using System;
using System.Collections;
using UnityEngine;

namespace LethalMenu.Cheats
{
    public static partial class NetworkCheats
    {
        #region Enemy Control

        private static readonly Vector3 EnemyVoidPosition = new(0f, -500f, 0f);
        private const float OwnershipTimeout = 2f;

        /// Kills an enemy for everyone. KillEnemyServerRpc has RequireOwnership = false; destroy is
        /// requested only for enemies that can't die (Jester, Coil-Head, ...), which then despawn instead
        /// of leaving a corpse.
        public static void KillEnemy(EnemyAI enemy)
        {
            if (enemy == null || enemy.isEnemyDead) return;
            enemy.KillEnemyServerRpc(destroy: !enemy.enemyType.canDie);
        }

        public static void KillAllEnemies()
        {
            int killed = 0;
            foreach (var enemy in LethalMenuMod.Enemies)
            {
                if (enemy == null || enemy.isEnemyDead) continue;
                KillEnemy(enemy);
                killed++;
            }
            HUDManager.Instance?.DisplayTip("Enemies", $"Killed {killed} enemies.");
        }

        /// Stuns an enemy. Stun has no RPC — it only takes effect on the client that runs the enemy's
        /// AI — so ownership is taken first when we don't already own it.
        public static void StunEnemy(EnemyAI enemy, float duration = 5f)
        {
            WithEnemyOwnership(enemy, e => e.SetEnemyStunned(true, duration, LethalMenuMod.LocalPlayer));
        }

        public static void StunAllEnemies()
        {
            int count = 0;
            foreach (var enemy in LethalMenuMod.Enemies)
            {
                if (enemy == null || enemy.isEnemyDead) continue;
                StunEnemy(enemy);
                count++;
            }
            HUDManager.Instance?.DisplayTip("Enemies", $"Stunned {count} enemies.");
        }

        /// Moves an enemy for everyone: takes ownership, warps its agent and syncs the position.
        public static void TeleportEnemy(EnemyAI enemy, Vector3 position)
        {
            WithEnemyOwnership(enemy, e =>
            {
                e.serverPosition = position;
                if (e.agent != null && e.agent.isOnNavMesh)
                    e.agent.Warp(position);
                e.transform.position = position;
                e.SyncPositionToClients();
            });
        }

        public static void TeleportAllEnemiesAway()
        {
            foreach (var enemy in LethalMenuMod.Enemies)
            {
                if (enemy == null || enemy.isEnemyDead) continue;
                TeleportEnemy(enemy, EnemyVoidPosition);
            }
        }

        /// Sends every enemy pathing towards a position.
        public static void LureAllEnemies(Vector3 target)
        {
            foreach (var enemy in LethalMenuMod.Enemies)
            {
                if (enemy == null || enemy.isEnemyDead) continue;
                WithEnemyOwnership(enemy, e => e.SetDestinationToPosition(target));
            }
        }

        /// Runs an owner-only action on an enemy, requesting ownership first when needed and running
        /// the action once it has transferred (or giving up after OwnershipTimeout).
        private static void WithEnemyOwnership(EnemyAI enemy, Action<EnemyAI> action)
        {
            var local = LethalMenuMod.LocalPlayer;
            if (enemy == null || enemy.isEnemyDead || local == null) return;

            if (enemy.IsOwner)
            {
                action(enemy);
                return;
            }

            enemy.ChangeEnemyOwnerServerRpc(local.actualClientId);
            LethalMenuMod.Instance?.StartCoroutine(RunWhenOwned(enemy, action));
        }

        private static IEnumerator RunWhenOwned(EnemyAI enemy, Action<EnemyAI> action)
        {
            float deadline = Time.time + OwnershipTimeout;
            while (enemy != null && !enemy.IsOwner && Time.time < deadline)
                yield return null;

            if (enemy != null && enemy.IsOwner && !enemy.isEnemyDead)
                action(enemy);
        }

        #endregion
    }
}
