using System.Linq;
using GameNetcodeStuff;
using UnityEngine;

namespace LethalMenu.Cheats.EnemyControl
{
    /// <summary>
    /// Controller for StingrayAI — flying ranged spitter.
    /// Primary: Spit on nearest player.
    /// Secondary: Toggle aggressive behaviour state.
    /// </summary>
    public class StingrayController : IEnemyController<StingrayAI>
    {
        private enum State
        {
            Passive = 0,
            Aggressive = 1
        }

        public void UsePrimarySkill(StingrayAI enemy)
        {
            var target = FindNearestPlayer(enemy);
            if (target == null) return;
            enemy.ShowSpitOnPlayerServerRpc((int)target.playerClientId);
        }

        public void UseSecondarySkill(StingrayAI enemy)
        {
            bool isAggressive = enemy.IsBehaviourState(State.Aggressive);
            enemy.SetBehaviourState(isAggressive ? State.Passive : State.Aggressive);
        }

        public string? GetPrimarySkillName(StingrayAI _) => "Spit";
        public string? GetSecondarySkillName(StingrayAI enemy)
            => enemy.IsBehaviourState(State.Aggressive) ? "Calm Down" : "Enrage";

        public bool CanUseEntranceDoors(StingrayAI _) => false;
        public float InteractRange(StingrayAI _) => 8f;

        private static PlayerControllerB? FindNearestPlayer(StingrayAI enemy)
        {
            if (StartOfRound.Instance == null) return null;
            return StartOfRound.Instance.allPlayerScripts
                .Where(p => p != null && p.isPlayerControlled && !p.isPlayerDead)
                .OrderBy(p => Vector3.Distance(enemy.transform.position, p.transform.position))
                .FirstOrDefault();
        }
    }
}
