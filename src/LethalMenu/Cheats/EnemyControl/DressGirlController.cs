using System.Linq;
using GameNetcodeStuff;
using LethalMenu.Util;
using UnityEngine;

namespace LethalMenu.Cheats.EnemyControl
{
    /// <summary>
    /// Controller for DressGirlAI (Ghost Girl).
    /// Primary: Haunt closest player → toggle chase / stop chase.
    /// On release / death: teleport off-map and hide mesh so the girl can't haunt afterward.
    /// </summary>
    public class DressGirlController : IEnemyController<DressGirlAI>
    {
        private const int ChaseStateIndex = 1;
        private static readonly Vector3 BanishPosition = new Vector3(-1000f, -1000f, -1000f);

        public void UsePrimarySkill(DressGirlAI enemy)
        {
            var player = GetClosestPlayer(enemy);
            if (player == null) return;
            enemy.hauntingPlayer = player;

            if (enemy.currentBehaviourStateIndex != ChaseStateIndex)
            {
                enemy.EnableEnemyMesh(true, true);
                enemy.SwitchToBehaviourStateOnLocalClient(0);
                enemy.Reflect().Invoke("BeginChasing");
            }
            else
            {
                enemy.Reflect().Invoke("StopChasing");
            }
        }

        public string? GetPrimarySkillName(DressGirlAI enemy)
        {
            var player = GetClosestPlayer(enemy);
            if (player == null) return "";
            return enemy.currentBehaviourStateIndex == ChaseStateIndex ? "Stop Chase" : "Begin Chase";
        }

        public string? GetSecondarySkillName(DressGirlAI _) => "";

        public void OnReleaseControl(DressGirlAI enemy) => Banish(enemy);
        public void OnDeath(DressGirlAI enemy) => Banish(enemy);

        public bool CanUseEntranceDoors(DressGirlAI _) => true;
        public float InteractRange(DressGirlAI _) => 5f;

        private static void Banish(DressGirlAI enemy)
        {
            if (enemy.currentBehaviourStateIndex == ChaseStateIndex)
            {
                enemy.Reflect().Invoke("StopChasing");
            }
            enemy.transform.position = BanishPosition;
            enemy.EnableEnemyMesh(false, true);
            enemy.hauntingPlayer = null;
        }

        private static PlayerControllerB? GetClosestPlayer(EnemyAI enemy)
        {
            if (StartOfRound.Instance == null) return null;
            return StartOfRound.Instance.allPlayerScripts
                .Where(p => p != null && p.isPlayerControlled && !p.isPlayerDead)
                .OrderBy(p => Vector3.Distance(enemy.transform.position, p.transform.position))
                .FirstOrDefault();
        }
    }
}
