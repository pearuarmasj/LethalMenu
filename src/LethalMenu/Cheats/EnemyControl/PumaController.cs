using System.Linq;
using GameNetcodeStuff;
using UnityEngine;

namespace LethalMenu.Cheats.EnemyControl
{
    /// <summary>
    /// Controller for PumaAI — multi-state predator.
    /// Primary: Scratch nearest player (PumaDamagePlayerRpc).
    /// Secondary: Stalking charge burst (SyncStalkingChargeRpc).
    /// </summary>
    public class PumaController : IEnemyController<PumaAI>
    {
        private enum State
        {
            Passive = 0,
            Stalking = 1,
            Berserk = 2
        }

        public void UsePrimarySkill(PumaAI enemy)
        {
            var target = FindNearestPlayer(enemy);
            if (target == null) return;
            enemy.PumaDamagePlayerRpc((int)target.playerClientId);
        }

        public void UseSecondarySkill(PumaAI enemy)
        {
            enemy.SetBehaviourState(State.Berserk);
            enemy.SyncStalkingChargeRpc();
        }

        public string? GetPrimarySkillName(PumaAI _) => "Scratch";
        public string? GetSecondarySkillName(PumaAI _) => "Stalking Charge";

        public bool CanUseEntranceDoors(PumaAI _) => true;
        public float InteractRange(PumaAI _) => 5f;
        public float SprintMultiplier(PumaAI _) => 3.5f;

        private static PlayerControllerB? FindNearestPlayer(PumaAI enemy)
        {
            if (StartOfRound.Instance == null) return null;
            return StartOfRound.Instance.allPlayerScripts
                .Where(p => p != null && p.isPlayerControlled && !p.isPlayerDead)
                .OrderBy(p => Vector3.Distance(enemy.transform.position, p.transform.position))
                .FirstOrDefault();
        }
    }
}
