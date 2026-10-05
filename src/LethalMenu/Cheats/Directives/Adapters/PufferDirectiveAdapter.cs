using GameNetcodeStuff;
using UnityEngine;

namespace LethalMenu.Cheats.Directives.Adapters
{
    /// Spore Lizard. Vanilla attack = behaviour state 2 (charge), entered from state 1 by PufferAI.DoAIInterval via
    /// SwitchToBehaviourState(2) once the player is close, then SetMovingTowardsTargetPlayer(closestSeenPlayer).
    /// The bite is the victim's own OnCollideWithPlayer -> DamagePlayer(20) + BitePlayerServerRpc, so it replicates
    /// normally. Engage enters state 2 directly (skipping the alert / stomp / puff display) and sets closestSeenPlayer.
    /// PufferAI.Update case 2 drains unclampedSpeed by 5/s and falls back to state 1 below -0.75; AfterUpdate tops it
    /// back up to the charge speed (9) except right after a bite, so the vanilla bite-pause-recharge rhythm stays.
    /// A stunned lizard is left alone (vanilla DoAIInterval also returns while stunned). Follow = state 0 (4 m/s).
    public sealed class PufferDirectiveAdapter : DirectiveAdapter<PufferAI>
    {
        private const int RoamState = 0;
        private const int ChargeState = 2;
        private const float ChargeSpeed = 9f;
        private const float BitePause = 0.5f;

        public override void Engage(PufferAI enemy, PlayerControllerB target)
        {
            if (enemy.stunNormalizedTimer > 0f) return;
            if (enemy.roamMap.inProgress) enemy.StopSearch(enemy.roamMap);
            if (enemy.currentBehaviourStateIndex != ChargeState) enemy.SwitchToBehaviourState(ChargeState);
            enemy.closestSeenPlayer = target;
            enemy.SetMovingTowardsTargetPlayer(target);
        }

        public override void AfterUpdate(PufferAI enemy, PlayerControllerB? target)
        {
            if (target == null) return;
            enemy.closestSeenPlayer = target;
            if (enemy.currentBehaviourStateIndex == ChargeState && enemy.timeSinceHittingPlayer > BitePause)
                enemy.unclampedSpeed = Mathf.Max(enemy.unclampedSpeed, ChargeSpeed);
            base.AfterUpdate(enemy, target);
        }

        public override void Disengage(PufferAI enemy)
        {
            if (enemy.currentBehaviourStateIndex != RoamState) enemy.SwitchToBehaviourState(RoamState);
            enemy.movingTowardsTargetPlayer = false;
            enemy.targetPlayer = null;
            enemy.closestSeenPlayer = null;
        }

        public override void Follow(PufferAI enemy, Vector3 position)
        {
            if (enemy.currentBehaviourStateIndex != RoamState) enemy.SwitchToBehaviourState(RoamState);
            if (enemy.roamMap.inProgress) enemy.StopSearch(enemy.roamMap);
            enemy.SetDestinationToPosition(position);
        }

        public override void Release(PufferAI enemy) => Disengage(enemy);

        public override bool CanUseEntrances(PufferAI enemy) => false;
    }
}
