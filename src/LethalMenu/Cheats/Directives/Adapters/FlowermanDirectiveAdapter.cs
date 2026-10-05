using GameNetcodeStuff;
using UnityEngine;

namespace LethalMenu.Cheats.Directives.Adapters
{
    /// Bracken. Vanilla attack = anger mode, behaviour state 2, entered through
    /// EnterAngerModeServerRpc(angerTime) (RequireOwnership = false). FlowermanAI.Update case 2 drains
    /// angerMeter and switches to state 1 at 0, so Engage keeps it topped up. The neck-snap is the victim's
    /// own OnCollideWithPlayer -> KillPlayerAnimationServerRpc, so it replicates normally.
    public sealed class FlowermanDirectiveAdapter : DirectiveAdapter<FlowermanAI>
    {
        private const int AngerState = 2;
        private const float AngerTime = 20f;

        public override void Engage(FlowermanAI enemy, PlayerControllerB target)
        {
            if (enemy.currentBehaviourStateIndex != AngerState)
                enemy.EnterAngerModeServerRpc(AngerTime);
            enemy.angerMeter = Mathf.Max(enemy.angerMeter, AngerTime);
            enemy.SetMovingTowardsTargetPlayer(target);
        }

        public override void Disengage(FlowermanAI enemy)
        {
            if (enemy.currentBehaviourStateIndex != 0) enemy.SwitchToBehaviourState(0);
            enemy.movingTowardsTargetPlayer = false;
            enemy.targetPlayer = null;
        }

        public override void Follow(FlowermanAI enemy, Vector3 position)
        {
            if (enemy.currentBehaviourStateIndex != 0) enemy.SwitchToBehaviourState(0);
            enemy.SetDestinationToPosition(position);
        }

        public override void Release(FlowermanAI enemy)
        {
            enemy.angerMeter = 0f;
            Disengage(enemy);
        }

        public override float SightRange(FlowermanAI enemy) => 40f;
        public override float SightAngle(FlowermanAI enemy) => 60f;
    }
}
