using GameNetcodeStuff;
using UnityEngine;

namespace LethalMenu.Cheats.Directives.Adapters
{
    /// Giant Sapsucker. Vanilla attack = behaviour state 2, entered by StartAttackingAndSync: watchingThreat /
    /// attackingThreat = the target's IVisibleThreat, targetPlayer, Screech, SwitchToBehaviourStateOnLocalClient(2),
    /// StartAttackingThreatServerRpc. That RPC makes the server hand the bird's ownership to the victim, which
    /// the director can't allow (it re-requests ownership and the bird would flap), so Engage does the same
    /// state change through SwitchToBehaviourState (synced to every client) and replicates watchingThreat with
    /// SyncWatchingThreatServerRpc, the RPC CheckLOSForCreatures uses for that. Every client then derives the
    /// attack itself in GiantKiwiAI.Update state 2 (attacking = within 10 m of watchingThreat); the peck damage
    /// is the victim's own OnCollideWithPlayer (timeSinceHittingPlayer) + AnimationEventB -> DamagePlayer(10).
    /// Chasing is EnemyAI.Update -> NavigateTowardsTargetPlayer via targetPlayer / movingTowardsTargetPlayer
    /// (base AfterUpdate); Update sets the state 2 speed itself.
    /// AfterUpdate also keeps timeSinceSeeingThreat at 0 and watchingThreat/attackingThreat on the target
    /// (their reset and the 4 s give-up timer live in DoAIInterval, which is skipped).
    public sealed class GiantKiwiDirectiveAdapter : DirectiveAdapter<GiantKiwiAI>
    {
        private const int RoamState = 0;
        private const int AttackState = 2;

        public override void Engage(GiantKiwiAI enemy, PlayerControllerB target)
        {
            if (enemy.inSpecialAnimation || enemy.inKillAnimation) return;

            var threat = (IVisibleThreat)target;
            if (enemy.currentBehaviourStateIndex != AttackState || enemy.attackingThreat != threat)
            {
                bool enteringAttack = enemy.currentBehaviourStateIndex != AttackState;
                enemy.watchingThreat = threat;
                enemy.attackingThreat = threat;
                if (!enemy.seenThreatsHoldingEgg.Contains(target.transform))
                    enemy.seenThreatsHoldingEgg.Add(target.transform);
                enemy.timeSpentChasingThreat = 0f;
                if (enteringAttack)
                {
                    enemy.Screech(enraged: true, sync: true);
                    enemy.SwitchToBehaviourState(AttackState);
                }
                enemy.SyncWatchingThreatServerRpc(target.NetworkObject, (int)LethalMenuMod.LocalPlayer!.playerClientId);
            }

            enemy.timeSinceSeeingThreat = 0f;
            enemy.SetMovingTowardsTargetPlayer(target);
        }

        public override void AfterUpdate(GiantKiwiAI enemy, PlayerControllerB? target)
        {
            if (target == null) return;
            var threat = (IVisibleThreat)target;
            enemy.watchingThreat = threat;
            enemy.attackingThreat = threat;
            enemy.timeSinceSeeingThreat = 0f;
            base.AfterUpdate(enemy, target);
        }

        public override void Disengage(GiantKiwiAI enemy)
        {
            if (enemy.currentBehaviourStateIndex != RoamState) enemy.SwitchToBehaviourState(RoamState);
            enemy.attacking = false;
            enemy.creatureAnimator.SetBool("Attacking", false);
            enemy.movingTowardsTargetPlayer = false;
            enemy.targetPlayer = null;
            enemy.watchingThreat = null;
            enemy.attackingThreat = null;
        }

        public override void Follow(GiantKiwiAI enemy, Vector3 position)
        {
            Disengage(enemy);
            if (enemy.stunNormalizedTimer > 0f || enemy.inSpecialAnimation) return;
            enemy.agent.speed = enemy.idlePatrolSpeed;
            enemy.SetDestinationToPosition(position);
        }

        public override void Release(GiantKiwiAI enemy) => Disengage(enemy);
    }
}
