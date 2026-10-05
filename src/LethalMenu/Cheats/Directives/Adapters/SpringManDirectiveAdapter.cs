using GameNetcodeStuff;
using UnityEngine;

namespace LethalMenu.Cheats.Directives.Adapters
{
    /// Coil-head. Vanilla attack = chase state 1 (SwitchToBehaviourState(1) with targetPlayer set). Update owns
    /// the game rule: the owner computes stoppingMovement from every player looking at it and syncs it through
    /// SetAnimationStop/GoServerRpc, so the coil-head only moves unobserved. Engage never overrides that. The
    /// hit is the victim's OnCollideWithPlayer -> DamagePlayer(90), so it replicates normally. The move/cooldown
    /// cycle (timeSpentMoving -> SetCoilheadOnCooldownClientRpc, server only) still runs; because the vanilla
    /// state-0 DoAIInterval that drains onCooldownPhase is skipped, Engage/Follow drain it and end the cooldown
    /// through SetCoilheadOnCooldownServerRpc(false). AfterUpdate only restores targetPlayer and never forces
    /// movingTowardsTargetPlayer while the coil-head is frozen.
    public sealed class SpringManDirectiveAdapter : DirectiveAdapter<SpringManAI>
    {
        private const int IdleState = 0;
        private const int ChaseState = 1;

        public override void Engage(SpringManAI enemy, PlayerControllerB target)
        {
            if (HoldDuringCooldown(enemy)) return;

            if (enemy.searchForPlayers.inProgress) enemy.StopSearch(enemy.searchForPlayers);
            enemy.targetPlayer = target;
            if (enemy.currentBehaviourStateIndex != ChaseState)
                enemy.SwitchToBehaviourState(ChaseState);
            if (!enemy.stoppingMovement)
                enemy.SetMovingTowardsTargetPlayer(target);
        }

        public override void AfterUpdate(SpringManAI enemy, PlayerControllerB? target)
        {
            if (target == null) return;
            enemy.targetPlayer = target;
        }

        public override void Disengage(SpringManAI enemy)
        {
            if (enemy.currentBehaviourStateIndex != IdleState) enemy.SwitchToBehaviourState(IdleState);
            enemy.movingTowardsTargetPlayer = false;
            enemy.targetPlayer = null;
        }

        public override void Follow(SpringManAI enemy, Vector3 position)
        {
            if (HoldDuringCooldown(enemy)) return;

            if (enemy.currentBehaviourStateIndex != IdleState) Disengage(enemy);
            if (enemy.searchForPlayers.inProgress) enemy.StopSearch(enemy.searchForPlayers);
            enemy.movingTowardsTargetPlayer = false;
            enemy.agent.speed = 6f;
            enemy.SetDestinationToPosition(position);
        }

        public override void Release(SpringManAI enemy) => Disengage(enemy);

        /// Vanilla state-0 cooldown handling: stand still, count onCooldownPhase down, then clear it for everyone.
        private static bool HoldDuringCooldown(SpringManAI enemy)
        {
            if (!enemy.setOnCooldown && enemy.onCooldownPhase <= 0f) return false;

            enemy.agent.speed = 0f;
            enemy.SetDestinationToPosition(enemy.transform.position);
            enemy.onCooldownPhase -= enemy.AIIntervalTime;
            if (enemy.onCooldownPhase <= 0f && enemy.setOnCooldown)
                enemy.SetCoilheadOnCooldownServerRpc(false);
            return true;
        }
    }
}
