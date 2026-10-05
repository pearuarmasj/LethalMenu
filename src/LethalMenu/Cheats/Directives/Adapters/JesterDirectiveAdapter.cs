using GameNetcodeStuff;
using UnityEngine;

namespace LethalMenu.Cheats.Directives.Adapters
{
    /// Jester. Vanilla attack = state 0 roam (targetPlayer set) -> Update counts beginCrankingTimer down and
    /// SwitchToBehaviourState(1) (cranking, agent stopped) -> popUpTimer reaches 0 -> SwitchToBehaviourState(2)
    /// (popped, speed ramps to 18). The kill is the victim's OnCollideWithPlayer -> KillPlayerServerRpc ->
    /// KillPlayerClientRpc, so it replicates normally. Engage sets targetPlayer and shortens beginCrankingTimer /
    /// popUpTimer so the real state sequence runs quickly. Update's popped branch returns the Jester to state 0
    /// after noPlayersToChaseTimer when nobody is inside the factory (targetingPlayer is never set), so
    /// AfterUpdate keeps that timer topped up.
    public sealed class JesterDirectiveAdapter : DirectiveAdapter<JesterAI>
    {
        private const int RoamState = 0;
        private const int CrankState = 1;
        private const int PoppedState = 2;
        private const float CrankTime = 3f;

        public override void Engage(JesterAI enemy, PlayerControllerB target)
        {
            if (enemy.roamMap.inProgress) enemy.StopSearch(enemy.roamMap);
            enemy.SetMovingTowardsTargetPlayer(target);

            switch (enemy.currentBehaviourStateIndex)
            {
                case RoamState:
                    enemy.agent.speed = enemy.stunNormalizedTimer > 0f ? 0f : 5f;
                    enemy.agent.stoppingDistance = 4f;
                    enemy.addPlayerVelocityToDestination = 0f;
                    enemy.beginCrankingTimer = 0f;
                    break;
                case CrankState:
                    enemy.popUpTimer = Mathf.Min(enemy.popUpTimer, CrankTime);
                    break;
                case PoppedState:
                    enemy.agent.stoppingDistance = 0f;
                    enemy.addPlayerVelocityToDestination = 1f;
                    break;
            }
        }

        public override void AfterUpdate(JesterAI enemy, PlayerControllerB? target)
        {
            if (target == null) return;
            enemy.noPlayersToChaseTimer = 5f;
            base.AfterUpdate(enemy, target);
        }

        public override void Disengage(JesterAI enemy)
        {
            if (enemy.currentBehaviourStateIndex != RoamState) enemy.SwitchToBehaviourState(RoamState);
            enemy.movingTowardsTargetPlayer = false;
            enemy.targetPlayer = null;
        }

        public override void Follow(JesterAI enemy, Vector3 position)
        {
            if (enemy.currentBehaviourStateIndex != RoamState) Disengage(enemy);
            if (enemy.roamMap.inProgress) enemy.StopSearch(enemy.roamMap);
            enemy.agent.speed = enemy.stunNormalizedTimer > 0f ? 0f : 5f;
            enemy.agent.stoppingDistance = 1f;
            enemy.addPlayerVelocityToDestination = 0f;
            enemy.SetDestinationToPosition(position);
        }

        public override void Release(JesterAI enemy) => Disengage(enemy);

        public override float SightRange(JesterAI enemy) => 40f;
        public override float SightAngle(JesterAI enemy) => 70f;
    }
}
