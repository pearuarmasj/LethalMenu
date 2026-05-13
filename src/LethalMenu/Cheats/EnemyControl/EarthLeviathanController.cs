using System.Collections;
using UnityEngine;

namespace LethalMenu.Cheats.EnemyControl
{
    /// <summary>
    /// Controller for SandWormAI (Earth Leviathan).
    /// Primary: Emerge from ground (20s cooldown).
    /// Continuously primes the chase timer while controlled so the worm stays aggressive.
    /// </summary>
    public class EarthLeviathanController : IEnemyController<SandWormAI>
    {
        private const int CooldownSeconds = 20;
        private static bool _emergeCooldown;
        private static int _remaining;

        public void Update(SandWormAI enemy, bool isAIControlled)
        {
            if (enemy == null || isAIControlled) return;
            enemy.chaseTimer = 2f;
        }

        public void UsePrimarySkill(SandWormAI enemy)
        {
            if (IsEmerged(enemy) || _emergeCooldown) return;
            enemy.StartEmergeAnimation();
            LethalMenuMod.Instance?.StartCoroutine(EmergeCooldownRoutine());
        }

        public string? GetPrimarySkillName(SandWormAI _)
            => _emergeCooldown ? $"Emerge Cooldown ({_remaining}s)" : "Emerge";

        public string? GetSecondarySkillName(SandWormAI _) => "";

        public bool CanUseEntranceDoors(SandWormAI _) => false;
        public float InteractRange(SandWormAI _) => 0.0f;
        public bool SyncAnimationSpeedEnabled(SandWormAI _) => false;

        private static bool IsEmerged(SandWormAI enemy) => enemy.inEmergingState || enemy.emerged;

        private static IEnumerator EmergeCooldownRoutine()
        {
            _emergeCooldown = true;
            _remaining = CooldownSeconds;
            while (_remaining > 0)
            {
                yield return new WaitForSeconds(1f);
                _remaining--;
            }
            _emergeCooldown = false;
        }
    }
}
