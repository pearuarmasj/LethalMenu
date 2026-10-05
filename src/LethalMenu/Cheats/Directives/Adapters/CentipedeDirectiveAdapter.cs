using GameNetcodeStuff;
using UnityEngine;

namespace LethalMenu.Cheats.Directives.Adapters
{
    /// Snare Flea. Vanilla attack = ceiling ambush: state 1 (hanging, agent disabled) -> the local player under it
    /// triggers TriggerCentipedeFallServerRpc(clientId) -> SwitchToBehaviourClientRpc(2) -> fallFromCeiling, then
    /// state 2 chases on the floor and the victim's OnCollideWithPlayer -> ClingToPlayerServerRpc -> state 3
    /// (cling, damage on intervals through DamagePlayerOnIntervals, all victim-side). Engage drops a hanging
    /// flea with the same RPC (passing our own client id, so ownership stays with us) and puts a floor flea
    /// straight into state 2 chasing the target. Update state 2 sends the flea back to state 0 when chaseTimer
    /// passes 10, so AfterUpdate zeroes it; clinging (state 3) and the fall/hide animations (inSpecialAnimation)
    /// are never touched. With no target it cancels a fall the hanging flea triggered on the local player.
    public sealed class CentipedeDirectiveAdapter : DirectiveAdapter<CentipedeAI>
    {
        private const int RoamState = 0;
        private const int CeilingState = 1;
        private const int ChaseState = 2;
        private const int ClingState = 3;

        public override void Engage(CentipedeAI enemy, PlayerControllerB target)
        {
            int state = enemy.currentBehaviourStateIndex;
            if (state == CeilingState)
            {
                if (enemy.clingingToCeiling && !enemy.triggeredFall)
                {
                    enemy.triggeredFall = true;
                    enemy.TriggerCentipedeFallServerRpc(LethalMenuMod.LocalPlayer!.actualClientId);
                }
                return;
            }
            if (state == ClingState || enemy.clingingToPlayer != null || enemy.inSpecialAnimation) return;

            enemy.targetNode = null;
            enemy.SetMovingTowardsTargetPlayer(target);
            if (state != ChaseState)
                enemy.SwitchToBehaviourState(ChaseState);
        }

        public override void AfterUpdate(CentipedeAI enemy, PlayerControllerB? target)
        {
            if (target == null)
            {
                if (enemy.currentBehaviourStateIndex == ChaseState && enemy.clingingToPlayer == null)
                    Disengage(enemy);
                return;
            }
            if (enemy.currentBehaviourStateIndex != ChaseState || enemy.inSpecialAnimation) return;
            enemy.chaseTimer = 0f;
            base.AfterUpdate(enemy, target);
        }

        public override void Disengage(CentipedeAI enemy)
        {
            if (enemy.currentBehaviourStateIndex == ChaseState) enemy.SwitchToBehaviourState(RoamState);
            enemy.movingTowardsTargetPlayer = false;
            enemy.targetPlayer = null;
            enemy.targetNode = null;
        }

        public override void Follow(CentipedeAI enemy, Vector3 position)
        {
            int state = enemy.currentBehaviourStateIndex;
            if (state == CeilingState || state == ClingState || enemy.inSpecialAnimation) return;

            if (state == ChaseState) Disengage(enemy);
            enemy.targetNode = null;
            enemy.SetDestinationToPosition(position);
        }

        public override void Release(CentipedeAI enemy) => Disengage(enemy);
    }
}
