using System;
using System.Collections;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace LethalMenu.Cheats
{
    public static partial class NetworkCheats
    {
        #region Enemy Targeting

        /// Target all enemies at a specific player. Ultimate troll move.
        public static void MobPlayer(PlayerControllerB targetPlayer, bool teleportEnemies = false)
        {
            if (targetPlayer == null)
            {
                HUDManager.Instance?.DisplayTip("Mob", "No target player.");
                return;
            }

            var localPlayer = LethalMenuMod.LocalPlayer;
            if (localPlayer == null) return;

            var enemies = UnityEngine.Object.FindObjectsOfType<EnemyAI>();
            if (enemies == null || enemies.Length == 0)
            {
                HUDManager.Instance?.DisplayTip("Mob", "No enemies found.");
                return;
            }

            int count = 0;
            foreach (var enemy in enemies)
            {
                if (enemy == null || enemy.isEnemyDead) continue;
                
                if (enemy is DocileLocustBeesAI || enemy is DoublewingAI || enemy is BlobAI || enemy is DressGirlAI)
                    continue;

                try
                {
                    enemy.ChangeEnemyOwnerServerRpc(localPlayer.actualClientId);
                    enemy.targetPlayer = targetPlayer;
                    enemy.movingTowardsTargetPlayer = true;
                    
                    enemy.SwitchToBehaviourState(1);
                    
                    if (teleportEnemies)
                    {
                        Vector3 offset = UnityEngine.Random.insideUnitSphere * 5f;
                        offset.y = 0;
                        Vector3 newPos = targetPlayer.transform.position + offset;
                        
                        enemy.transform.position = newPos;
                        enemy.serverPosition = newPos;
                        
                        if (enemy.agent != null && enemy.agent.isOnNavMesh)
                        {
                            enemy.agent.Warp(newPos);
                        }
                        
                        enemy.SyncPositionToClients();
                    }
                    
                    count++;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[NetworkCheats] Failed to mob enemy {enemy.enemyType?.enemyName}: {ex.Message}");
                }
            }

            Debug.Log($"[NetworkCheats] Mobbed {count} enemies on {targetPlayer.playerUsername}");
            HUDManager.Instance?.DisplayTip("Mob", $"{count} enemies targeting {targetPlayer.playerUsername}!");
        }

        /// Stun all enemies near a position.
        public static void StunEnemiesAtPosition(Vector3 position, float radius = 10f, float stunDuration = 5f)
        {
            var enemies = UnityEngine.Object.FindObjectsOfType<EnemyAI>();
            int count = 0;

            foreach (var enemy in enemies)
            {
                if (enemy == null || enemy.isEnemyDead) continue;
                
                float distance = Vector3.Distance(enemy.transform.position, position);
                if (distance <= radius)
                {
                    enemy.SetEnemyStunned(true, stunDuration);
                    count++;
                }
            }

            HUDManager.Instance?.DisplayTip("Stun", $"Stunned {count} enemies.");
        }

        /// Stun enemy/turret/landmine that the camera is looking at.
        public static void StunAtCrosshair()
        {
            var cam = Camera.main;
            if (cam == null) return;

            Ray ray = new Ray(cam.transform.position, cam.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                var enemy = hit.collider.GetComponentInParent<EnemyAI>();
                if (enemy != null)
                {
                    enemy.SetEnemyStunned(true, 5f);
                    HUDManager.Instance?.DisplayTip("Stun", $"Stunned {enemy.enemyType?.enemyName}");
                    return;
                }

                var turret = hit.collider.GetComponent<Turret>();
                if (turret != null)
                {
                    var terminalObj = turret.GetComponent<TerminalAccessibleObject>();
                    if (terminalObj != null)
                    {
                        terminalObj.CallFunctionFromTerminal();
                        HUDManager.Instance?.DisplayTip("Stun", "Disabled turret.");
                    }
                    return;
                }

                var landmine = hit.collider.GetComponent<Landmine>();
                if (landmine != null)
                {
                    var terminalObj = landmine.GetComponent<TerminalAccessibleObject>();
                    if (terminalObj != null)
                    {
                        terminalObj.CallFunctionFromTerminal();
                        HUDManager.Instance?.DisplayTip("Stun", "Disabled landmine.");
                    }
                    return;
                }
            }

            HUDManager.Instance?.DisplayTip("Stun", "Nothing to stun.");
        }

        /// Lag a player by spawning 10 Brackens and passing AI computation to them.
        public static void LagPlayer(PlayerControllerB targetPlayer)
        {
            if (targetPlayer == null)
            {
                HUDManager.Instance?.DisplayTip("Lag", "No target player.");
                return;
            }

            var localPlayer = LethalMenuMod.LocalPlayer;
            if (localPlayer == null) return;

            EnemyType? brackenType = null;
            var allEnemyTypes = Resources.FindObjectsOfTypeAll<EnemyType>();
            foreach (var type in allEnemyTypes)
            {
                if (type?.enemyName != null && 
                    (type.enemyName.Contains("Flowerman", StringComparison.OrdinalIgnoreCase) ||
                     type.enemyName.Contains("Bracken", StringComparison.OrdinalIgnoreCase)))
                {
                    brackenType = type;
                    break;
                }
            }

            if (brackenType?.enemyPrefab == null)
            {
                HUDManager.Instance?.DisplayTip("Lag", "Bracken enemy type not found.");
                return;
            }

            Vector3 basePos = targetPlayer.transform.position;
            int spawnCount = 10;
            int successCount = 0;

            for (int i = 0; i < spawnCount; i++)
            {
                Vector3 offset = new Vector3(
                    UnityEngine.Random.Range(-15f, 15f),
                    0f,
                    UnityEngine.Random.Range(-15f, 15f)
                );
                Vector3 spawnPos = basePos + offset;

                GameObject enemyObj = UnityEngine.Object.Instantiate(brackenType.enemyPrefab, spawnPos, Quaternion.identity);
                if (!enemyObj.TryGetComponent(out NetworkObject networkObject))
                {
                    UnityEngine.Object.Destroy(enemyObj);
                    continue;
                }

                if (!enemyObj.TryGetComponent(out FlowermanAI bracken))
                {
                    UnityEngine.Object.Destroy(enemyObj);
                    continue;
                }

                networkObject.Spawn(true);

                bracken.ChangeEnemyOwnerServerRpc(targetPlayer.actualClientId);
                
                bracken.targetPlayer = targetPlayer;
                bracken.movingTowardsTargetPlayer = true;
                bracken.EnterAngerModeServerRpc(float.MaxValue);

                LethalMenuMod.Instance?.StartCoroutine(MakeEnemyInvisibleDelayed(bracken));

                successCount++;
            }

            Debug.Log($"[NetworkCheats] Spawned {successCount} INVISIBLE Brackens targeting {targetPlayer.playerUsername}");
            HUDManager.Instance?.DisplayTip("Lag", $"Spawned {successCount} invisible Brackens on {targetPlayer.playerUsername}!");
        }

        private static IEnumerator MakeEnemyInvisibleDelayed(EnemyAI enemy)
        {
            yield return null;
            yield return null;
            
            if (enemy != null && !enemy.isEnemyDead)
            {
                enemy.EnableEnemyMesh(false, true);
                
                if (enemy.skinnedMeshRenderers != null)
                {
                    foreach (var renderer in enemy.skinnedMeshRenderers)
                    {
                        if (renderer != null) renderer.enabled = false;
                    }
                }
                if (enemy.meshRenderers != null)
                {
                    foreach (var renderer in enemy.meshRenderers)
                    {
                        if (renderer != null) renderer.enabled = false;
                    }
                }
            }
        }

        #endregion
    }
}
