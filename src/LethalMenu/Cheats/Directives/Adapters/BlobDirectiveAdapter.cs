using GameNetcodeStuff;
using UnityEngine;

namespace LethalMenu.Cheats.Directives.Adapters
{
    /// Hygrodere. It has no chase state: vanilla DoAIInterval just does TargetClosestPlayer(4f) and sets
    /// movingTowardsTargetPlayer, and EnemyAI.Update walks it there (NavigateTowardsTargetPlayer). The attack is
    /// BlobAI.Update's slime raycasts -> RaycastCollisionWithPlayers -> OnCollideWithPlayer on the victim's own
    /// client (DamagePlayer(35), then SlimeKillPlayerEffectServerRpc if that killed), so it replicates normally.
    /// Engage = stop the search, move to the target and keep angeredTimer topped up (anger enlarges the slime
    /// range). Anger speed is 0.6 m/s, too slow to ever reach a moving player, so AfterUpdate raises the agent to
    /// 3 m/s, the speed vanilla itself lerps to while following a tamed slime. Follow uses that tamed mode
    /// (tamedTimer > 0, angeredTimer = 0): it trails at 3 m/s and OnCollideWithPlayer is disabled, so it never hurts
    /// the escorted player. AfterUpdate guards against the kill ClientRpc and stun zeroing angeredTimer.
    public sealed class BlobDirectiveAdapter : DirectiveAdapter<BlobAI>
    {
        private const float AngerTime = 18f;
        private const float TamedTime = 2f;
        private const float ChaseSpeed = 3f;

        public override void Engage(BlobAI enemy, PlayerControllerB target)
        {
            StopSearch(enemy);
            enemy.tamedTimer = 0f;
            enemy.angeredTimer = AngerTime;
            enemy.SetMovingTowardsTargetPlayer(target);
        }

        public override void AfterUpdate(BlobAI enemy, PlayerControllerB? target)
        {
            if (target == null)
            {
                if (enemy.tamedTimer > 0f) enemy.tamedTimer = TamedTime;
                return;
            }
            enemy.tamedTimer = 0f;
            enemy.angeredTimer = Mathf.Max(enemy.angeredTimer, AngerTime);
            if (enemy.stunNormalizedTimer <= 0f)
                enemy.agent.speed = ChaseSpeed;
            base.AfterUpdate(enemy, target);
        }

        public override void Disengage(BlobAI enemy)
        {
            enemy.angeredTimer = 0f;
            enemy.movingTowardsTargetPlayer = false;
            enemy.targetPlayer = null;
        }

        public override void Follow(BlobAI enemy, Vector3 position)
        {
            StopSearch(enemy);
            enemy.angeredTimer = 0f;
            enemy.tamedTimer = TamedTime;
            enemy.SetDestinationToPosition(position);
        }

        public override void Release(BlobAI enemy)
        {
            enemy.angeredTimer = 0f;
            enemy.tamedTimer = 0f;
            enemy.movingTowardsTargetPlayer = false;
            enemy.targetPlayer = null;
        }

        public override bool CanUseEntrances(BlobAI enemy) => false;

        private static void StopSearch(BlobAI enemy)
        {
            if (enemy.searchForPlayers.inProgress) enemy.StopSearch(enemy.searchForPlayers);
        }
    }
}
