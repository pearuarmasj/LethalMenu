using GameNetcodeStuff;
using UnityEngine;

namespace LethalMenu.Cheats.Directives.Adapters
{
    /// Butler Bees. No behaviour states: DoAIInterval picks a target with TargetClosestPlayer(4 m), sets
    /// movingTowardsTargetPlayer, ramps agent.speed to 5.4 (4.25 in solo) and hands ownership to the target
    /// (ChangeOwnershipOfEnemy, suppressed while directed). The sting is ButlerBeesEnemyAI.OnCollideWithPlayer on
    /// the victim's client -> DamagePlayer(10, callRPC: true), so it replicates normally. Directed, Engage redoes
    /// the chase half of DoAIInterval (target, speed ramp, buzz pitch, chasePlayerTimer reset) and Follow the idle
    /// half (speed 3, searchForPlayers stopped because it would override the destination). There is no Update
    /// override, so AfterUpdate is the default pin.
    public sealed class ButlerBeesDirectiveAdapter : DirectiveAdapter<ButlerBeesEnemyAI>
    {
        private const float ChaseSpeedSolo = 4.25f;
        private const float ChaseSpeed = 5.4f;
        private const float IdleSpeed = 3f;

        public override void Engage(ButlerBeesEnemyAI enemy, PlayerControllerB target)
        {
            if (enemy.searchForPlayers.inProgress) enemy.StopSearch(enemy.searchForPlayers);
            enemy.SetMovingTowardsTargetPlayer(target);
            enemy.chasePlayerTimer = 0f;
            float maxSpeed = StartOfRound.Instance.connectedPlayersAmount == 0 ? ChaseSpeedSolo : ChaseSpeed;
            enemy.agent.speed = Mathf.Min(enemy.agent.speed + enemy.AIIntervalTime * 0.75f, maxSpeed);
            enemy.buzzing.pitch = Mathf.Lerp(enemy.buzzing.pitch, 1.3f, enemy.AIIntervalTime * 5f);
        }

        public override void Disengage(ButlerBeesEnemyAI enemy)
        {
            enemy.movingTowardsTargetPlayer = false;
            enemy.targetPlayer = null;
            enemy.agent.speed = IdleSpeed;
            enemy.buzzing.pitch = 1f;
        }

        public override void Follow(ButlerBeesEnemyAI enemy, Vector3 position)
        {
            if (enemy.searchForPlayers.inProgress) enemy.StopSearch(enemy.searchForPlayers);
            enemy.agent.speed = IdleSpeed;
            enemy.SetDestinationToPosition(position);
        }

        public override void Release(ButlerBeesEnemyAI enemy) => Disengage(enemy);

        /// Vanilla TargetClosestPlayer(4 m, line of sight, 180 degrees).
        public override float SightRange(ButlerBeesEnemyAI enemy) => 4f;
        public override float SightAngle(ButlerBeesEnemyAI enemy) => 180f;
    }
}
