using GameNetcodeStuff;
using UnityEngine;

namespace LethalMenu.Cheats.Directives.Adapters
{
    /// Bunker Spider. Vanilla attack = TriggerChaseWithPlayer: watchFromDistance = false, targetPlayer set,
    /// chaseTimer = 12.5, SwitchToBehaviourState(2) (state 2 chase, movingTowardsTargetPlayer, speed 4.4-5.04).
    /// Engage does the same minus the distance/path gate that only decides whether a spider wants to attack.
    /// The bite is the victim's OnCollideWithPlayer -> DamagePlayer(90) + HitPlayerServerRpc, so it replicates
    /// normally. The vanilla DoAIInterval also advances the visible spider mesh (CalculateSpiderPathToPosition),
    /// so Engage/Follow call it. Update state 0 attacks whoever it sees (the local player included), so escort
    /// idles in state 1 instead, where Update only keeps the wall timer (waitOnWallTimer, topped up here) and
    /// lookingForWallPosition stays false (only the skipped DoAIInterval sets it). AfterUpdate keeps chaseTimer
    /// full and the target fixed; with no target it pulls a self-started chase back to state 1.
    public sealed class SandSpiderDirectiveAdapter : DirectiveAdapter<SandSpiderAI>
    {
        private const int EscortState = 1;
        private const int ChaseState = 2;
        private const float ChaseTime = 12.5f;
        private const float WallWaitTime = 11f;

        public override void Engage(SandSpiderAI enemy, PlayerControllerB target)
        {
            if (enemy.patrolHomeBase.inProgress) enemy.StopSearch(enemy.patrolHomeBase);
            enemy.lookingForWallPosition = false;

            if (enemy.currentBehaviourStateIndex != ChaseState || enemy.watchFromDistance)
            {
                enemy.watchFromDistance = false;
                enemy.targetPlayer = target;
                enemy.chaseTimer = ChaseTime;
                enemy.SwitchToBehaviourState(ChaseState);
            }
            enemy.SetMovingTowardsTargetPlayer(target);
            UpdateMesh(enemy);
        }

        public override void AfterUpdate(SandSpiderAI enemy, PlayerControllerB? target)
        {
            if (target == null)
            {
                if (enemy.currentBehaviourStateIndex != EscortState) Disengage(enemy);
                enemy.waitOnWallTimer = WallWaitTime;
                return;
            }
            if (enemy.currentBehaviourStateIndex == ChaseState)
            {
                enemy.chaseTimer = ChaseTime;
                enemy.watchFromDistance = false;
            }
            base.AfterUpdate(enemy, target);
        }

        public override void Disengage(SandSpiderAI enemy)
        {
            enemy.overrideSpiderLookRotation = false;
            enemy.watchFromDistance = false;
            enemy.lookingForWallPosition = false;
            enemy.movingTowardsTargetPlayer = false;
            enemy.targetPlayer = null;
            if (enemy.currentBehaviourStateIndex != EscortState) enemy.SwitchToBehaviourState(EscortState);
        }

        public override void Follow(SandSpiderAI enemy, Vector3 position)
        {
            if (enemy.currentBehaviourStateIndex != EscortState) Disengage(enemy);
            if (enemy.patrolHomeBase.inProgress) enemy.StopSearch(enemy.patrolHomeBase);
            enemy.lookingForWallPosition = false;
            enemy.waitOnWallTimer = WallWaitTime;
            enemy.SetDestinationToPosition(position);
            UpdateMesh(enemy);
        }

        public override void Release(SandSpiderAI enemy)
        {
            Disengage(enemy);
            enemy.SwitchToBehaviourState(0);
        }

        private static void UpdateMesh(SandSpiderAI enemy)
        {
            if (enemy.navigateMeshTowardsPosition) enemy.CalculateSpiderPathToPosition();
        }
    }
}
