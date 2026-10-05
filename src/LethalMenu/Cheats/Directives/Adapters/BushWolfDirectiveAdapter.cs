using GameNetcodeStuff;
using UnityEngine;

namespace LethalMenu.Cheats.Directives.Adapters
{
    /// Kidnapper Fox. Vanilla attack: state 1 = stalking (DoAIInterval: ChooseClosestNodeToPlayer), then
    /// targetPlayer + SwitchToBehaviourStateOnLocalClient(2) + SyncTargetPlayerAndAttackServerRpc(playerId) once within
    /// attackDistance with a clear line. State 2 is run entirely by Update on the owner (tongue shot, HitByEnemyServerRpc /
    /// DodgedEnemyHitServerRpc from the victim, drag towards currentHidingSpot); the kill is the victim's own OnCollideWithPlayer
    /// (dragged and within 7 m of the nest) -> DoKillPlayerAnimationServerRpc. Engage therefore only enters state 1 / 2 and
    /// then leaves state 2 alone, except for DoAIInterval's 35 s drag timeout which is reproduced here.
    /// AfterUpdate only re-pins targetPlayer (never movingTowardsTargetPlayer, which the drag logic owns). With no target
    /// (escort) it clears the staring/spotted state that makes Update state 0 stop the fox and forces a walking speed.
    public sealed class BushWolfDirectiveAdapter : DirectiveAdapter<BushWolfEnemy>
    {
        private const int HideState = 0;
        private const int StalkState = 1;
        private const int AttackState = 2;
        private const float AttackRangeMargin = 1f;
        private const float DragTimeout = 35f;
        private const float EscortSpeed = 6f;

        public override void Engage(BushWolfEnemy enemy, PlayerControllerB target)
        {
            if (!enemy.foundSpawningPoint || enemy.inKillAnimation) return;

            if (enemy.currentBehaviourStateIndex == AttackState)
            {
                if (enemy.dragging && !enemy.startedShootingTongue && enemy.shootTongueTimer > DragTimeout)
                    enemy.SwitchToBehaviourState(StalkState);
                return;
            }

            enemy.targetPlayer = target;
            if (enemy.currentBehaviourStateIndex != StalkState)
            {
                enemy.SwitchToBehaviourState(StalkState);
                return;
            }

            if (enemy.timeSinceChangingState > 0.35f && InAttackRange(enemy, target))
            {
                enemy.SwitchToBehaviourStateOnLocalClient(AttackState);
                enemy.SyncTargetPlayerAndAttackServerRpc((int)target.playerClientId);
                return;
            }
            enemy.ChooseClosestNodeToPlayer();
        }

        public override void AfterUpdate(BushWolfEnemy enemy, PlayerControllerB? target)
        {
            if (target != null)
            {
                if (enemy.currentBehaviourStateIndex != HideState) enemy.targetPlayer = target;
                return;
            }

            if (enemy.currentBehaviourStateIndex != HideState) return;
            enemy.staringAtPlayer = null;
            enemy.spottedMeter = 0f;
            enemy.runningAwayToNest = false;
            if (enemy.timeSinceKillingPlayer >= 2f && enemy.timeSinceTakingDamage >= 0.35f && enemy.stunNormalizedTimer <= 0f)
                enemy.agent.speed = EscortSpeed * enemy.speedMultiplier;
        }

        public override void Disengage(BushWolfEnemy enemy)
        {
            if (enemy.currentBehaviourStateIndex != HideState) enemy.SwitchToBehaviourState(HideState);
            enemy.movingTowardsTargetPlayer = false;
            enemy.targetPlayer = null;
        }

        public override void Follow(BushWolfEnemy enemy, Vector3 position)
        {
            if (enemy.currentBehaviourStateIndex != HideState) enemy.SwitchToBehaviourState(HideState);
            enemy.movingTowardsTargetPlayer = false;
            enemy.moveTowardsDestination = true;
            enemy.SetDestinationToPosition(position);
        }

        public override void Release(BushWolfEnemy enemy)
        {
            if (enemy.currentBehaviourStateIndex != HideState) enemy.SwitchToBehaviourState(HideState);
            enemy.movingTowardsTargetPlayer = false;
            enemy.timeSinceAdjustingPosition = 0f;
        }

        /// The vanilla trigger (DoAIInterval case 1) with the attack distance minus a fixed margin: the tongue only connects
        /// within attackDistance, and the vanilla stand-still conditions (attackDistance - 5 / - 7) would never fire on a moving hunt target.
        private static bool InAttackRange(BushWolfEnemy enemy, PlayerControllerB target)
        {
            if (Vector3.Distance(enemy.transform.position, target.transform.position) >= enemy.attackDistance - AttackRangeMargin)
                return false;
            return !Physics.Linecast(enemy.transform.position + Vector3.up * 0.6f,
                target.gameplayCamera.transform.position - Vector3.up * 0.3f,
                StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore);
        }
    }
}
