using System;
using System.Linq;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace LethalMenu.Cheats
{
    public static partial class NetworkCheats
    {
        #region Enemy Spawning

        /// Gets all available enemy types for the current level.
        public static string[] GetAvailableEnemyNames()
        {
            var roundManager = RoundManager.Instance;
            if (roundManager?.currentLevel == null) return Array.Empty<string>();

            var enemies = new System.Collections.Generic.List<string>();

            if (roundManager.currentLevel.Enemies != null)
            {
                foreach (var spawnableEnemy in roundManager.currentLevel.Enemies)
                {
                    if (spawnableEnemy?.enemyType != null)
                        enemies.Add(spawnableEnemy.enemyType.enemyName);
                }
            }

            if (roundManager.currentLevel.OutsideEnemies != null)
            {
                foreach (var spawnableEnemy in roundManager.currentLevel.OutsideEnemies)
                {
                    if (spawnableEnemy?.enemyType != null && !enemies.Contains(spawnableEnemy.enemyType.enemyName))
                        enemies.Add("[O] " + spawnableEnemy.enemyType.enemyName);
                }
            }

            return enemies.ToArray();
        }

        /// Finds an EnemyType by name from the current level.
        private static EnemyType? FindEnemyType(string enemyName, bool outsideEnemy)
        {
            var roundManager = RoundManager.Instance;
            if (roundManager?.currentLevel == null) return null;

            string searchName = enemyName.StartsWith("[O] ") ? enemyName.Substring(4) : enemyName;

            if (!outsideEnemy && roundManager.currentLevel.Enemies != null)
            {
                foreach (var spawnableEnemy in roundManager.currentLevel.Enemies)
                {
                    if (spawnableEnemy?.enemyType?.enemyName?.Equals(searchName, StringComparison.OrdinalIgnoreCase) == true)
                        return spawnableEnemy.enemyType;
                }
            }

            if (roundManager.currentLevel.OutsideEnemies != null)
            {
                foreach (var spawnableEnemy in roundManager.currentLevel.OutsideEnemies)
                {
                    if (spawnableEnemy?.enemyType?.enemyName?.Equals(searchName, StringComparison.OrdinalIgnoreCase) == true)
                        return spawnableEnemy.enemyType;
                }
            }

            var allEnemyTypes = Resources.FindObjectsOfTypeAll<EnemyType>();
            foreach (var type in allEnemyTypes)
            {
                if (type?.enemyName?.Equals(searchName, StringComparison.OrdinalIgnoreCase) == true)
                    return type;
            }

            return null;
        }

        /// Spawns a mimic (masked enemy) that looks like a specific player. Must be host.
        public static void SpawnMimic(PlayerControllerB targetPlayer)
        {
            if (targetPlayer == null)
            {
                HUDManager.Instance?.DisplayTip("Spawn Mimic", "No player selected.");
                return;
            }

            var roundManager = RoundManager.Instance;
            if (roundManager == null)
            {
                HUDManager.Instance?.DisplayTip("Spawn Mimic", "Not in game.");
                return;
            }

            if (!LethalMenuMod.LocalPlayer?.IsHost == true)
            {
                HUDManager.Instance?.DisplayTip("Spawn Mimic", "Host only.");
                return;
            }

            EnemyType? maskedType = FindEnemyType("Masked", false);
            if (maskedType == null)
            {
                var allTypes = Resources.FindObjectsOfTypeAll<EnemyType>();
                maskedType = allTypes.FirstOrDefault(t => t.enemyName.Contains("Masked"));
            }

            if (maskedType == null)
            {
                HUDManager.Instance?.DisplayTip("Spawn Mimic", "Masked enemy type not found.");
                return;
            }

            Vector3 spawnPos = targetPlayer.transform.position;
            float yRot = targetPlayer.transform.eulerAngles.y;

            var netObjRef = roundManager.SpawnEnemyGameObject(spawnPos, yRot, -1, maskedType);
            
            NetworkObject? networkObject;
            if (netObjRef.TryGet(out networkObject))
            {
                var mimic = networkObject.GetComponent<MaskedPlayerEnemy>();
                if (mimic != null)
                {
                    mimic.SetSuit(targetPlayer.currentSuitID);
                    mimic.mimickingPlayer = targetPlayer;
                    mimic.SetEnemyOutside(!targetPlayer.isInsideFactory);
                    Debug.Log($"[NetworkCheats] Spawned mimic of {targetPlayer.playerUsername}");
                    HUDManager.Instance?.DisplayTip("Spawn Mimic", $"Spawned mimic of {targetPlayer.playerUsername}.");
                }
            }
        }

        #endregion
    }
}
