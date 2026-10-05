using GameNetcodeStuff;
using UnityEngine;

namespace LethalMenu.Cheats.Directives.Adapters
{
    /// Stingray. Its attack is the exit-cloak lunge + spit: state 1 (cloaked; Update's entry block sets
    /// exitCloakLunge = 11) -> SwitchToBehaviourState(0), where Update's state 0 decays exitCloakLunge into
    /// agent.speed, faces targetPlayer (owner), calls SpitAtPlayers (hasSpit) and the victim's own client
    /// detects the capsule in front of the stingray and runs SpitOnLocalPlayer + ShowSpitOnPlayerServerRpc.
    /// Both state changes go through SwitchToBehaviourState so every client runs the same sequence. Engage
    /// approaches the target in state 0, cloaks within 6 m, then (once Update ran the state 1 entry block)
    /// lunges, with a 4 s cooldown (timeSinceAttacking, Update increments it every frame).
    /// Update's state 0 entry block is `if (IsOwner && local clientId != 0) { ChangeOwnershipOfEnemy(host); break; }`;
    /// ownership changes are suppressed for directed enemies, so on a non-host director that block would repeat
    /// forever and state 0 would never run. EnterRoam performs the block's setup itself and marks
    /// previousBehaviourState, which makes Update skip it.
    /// AfterUpdate clears hidingSpot.gotHidingSpot (Update's state 0 would cloak and hide on it) and keeps the
    /// chase pointed at the target through the base.
    public sealed class StingrayDirectiveAdapter : DirectiveAdapter<StingrayAI>
    {
        private const int RoamState = 0;
        private const int CloakedState = 1;
        private const float LungeRange = 6f;
        private const float LungeCooldown = 4f;

        public override void Engage(StingrayAI enemy, PlayerControllerB target)
        {
            if (enemy.stunNormalizedTimer > 0f) return;

            enemy.SetMovingTowardsTargetPlayer(target);
            float distance = Vector3.Distance(enemy.transform.position, target.transform.position);

            if (enemy.currentBehaviourStateIndex == CloakedState)
            {
                // Wait for Update's state 1 entry block (exitCloakLunge = 11), then lunge.
                if (enemy.previousBehaviourState == CloakedState)
                {
                    enemy.SwitchToBehaviourState(RoamState);
                    EnterRoam(enemy);
                }
                return;
            }

            EnterRoam(enemy);
            enemy.SetDestinationToPosition(target.transform.position);
            enemy.movingTowardsTargetPlayer = true;
            if (enemy.exitCloakLunge <= 0f && distance < LungeRange && enemy.timeSinceAttacking > LungeCooldown)
            {
                enemy.timeSinceAttacking = 0f;
                enemy.SwitchToBehaviourState(CloakedState);
            }
        }

        public override void AfterUpdate(StingrayAI enemy, PlayerControllerB? target)
        {
            enemy.hidingSpot.gotHidingSpot = false;
            if (target == null) return;
            base.AfterUpdate(enemy, target);
        }

        public override void Disengage(StingrayAI enemy)
        {
            if (enemy.currentBehaviourStateIndex != RoamState) enemy.SwitchToBehaviourState(RoamState);
            EnterRoam(enemy);
            enemy.movingTowardsTargetPlayer = false;
            enemy.targetPlayer = null;
        }

        public override void Follow(StingrayAI enemy, Vector3 position)
        {
            Disengage(enemy);
            enemy.SetDestinationToPosition(position);
        }

        public override void Release(StingrayAI enemy) => Disengage(enemy);

        /// Update's state 0 entry block (StingrayAI.Update case 0) minus the ownership hand-off.
        private static void EnterRoam(StingrayAI enemy)
        {
            if (enemy.currentBehaviourStateIndex != RoamState || enemy.previousBehaviourState == RoamState) return;
            enemy.timeSpentInState = 0f;
            enemy.agent.speed = 0f;
            enemy.agent.acceleration = 30f;
            enemy.creatureAnimator.SetBool("cloaked", false);
            enemy.hasSpit = false;
            enemy.hidingSpot.gotHidingSpot = false;
            enemy.hidingSpot.choseTemporarySpot = false;
            enemy.hidingSpot.type = HidingSpotType.Temporary;
            enemy.watchPlayerSpitTimer = 0f;
            enemy.previousBehaviourState = RoamState;
        }
    }
}
