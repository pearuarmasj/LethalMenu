using GameNetcodeStuff;
using UnityEngine;

namespace LethalMenu.Cheats.EnemyControl
{
    /// <summary>
    /// Controller for FlowerSnakeEnemy (Tulip Snake).
    /// Primary: Hit clung player (damage tick while latched).
    /// Secondary: Release cling and fly away.
    /// </summary>
    public class FlowerSnakeController : IEnemyController<FlowerSnakeEnemy>
    {
        public void UsePrimarySkill(FlowerSnakeEnemy enemy)
        {
            var target = FindNearestPlayer(enemy);
            if (target == null) return;
            enemy.FSHitPlayerServerRpc((int)target.playerClientId);
        }

        public void UseSecondarySkill(FlowerSnakeEnemy enemy)
        {
            var local = LethalMenuMod.LocalPlayer;
            if (local != null) enemy.StopClingingServerRpc((int)local.playerClientId);
            enemy.StartFlyingServerRpc();
        }

        public string? GetPrimarySkillName(FlowerSnakeEnemy _) => "Hit Player";
        public string? GetSecondarySkillName(FlowerSnakeEnemy _) => "Release & Fly";

        public bool CanUseEntranceDoors(FlowerSnakeEnemy _) => false;
        public float InteractRange(FlowerSnakeEnemy _) => 2.5f;

        private static PlayerControllerB? FindNearestPlayer(FlowerSnakeEnemy enemy)
        {
            PlayerControllerB? nearest = null;
            float bestDist = float.MaxValue;
            foreach (var player in StartOfRound.Instance.allPlayerScripts)
            {
                if (player == null || player.isPlayerDead || !player.isPlayerControlled) continue;
                float d = Vector3.Distance(enemy.transform.position, player.transform.position);
                if (d < bestDist) { bestDist = d; nearest = player; }
            }
            return nearest;
        }
    }
}
