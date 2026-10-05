using GameNetcodeStuff;
using UnityEngine;

namespace LethalMenu.Cheats.Directives.Adapters
{
    /// Masked. Vanilla chase = behaviour state 1 (DoAIInterval case 0 calls LookAtPlayerServerRpc, SetMovingTowardsTargetPlayer,
    /// SwitchToBehaviourState(1)); state 1 runs/hands-out by distance through SetRunningServerRpc / SetHandsOutServerRpc.
    /// The kill is the victim's own OnCollideWithPlayer -> KillPlayerAnimationServerRpc, so it replicates normally.
    /// Update state 1 holds the enemy still for stopAndStareTimer (2-5 s, then cycles) and DoAIInterval's lost-LOS timers
    /// are skipped, so AfterUpdate keeps stopAndStareTimer negative and clears lostPlayerInChase / lostLOSTimer.
    public sealed class MaskedPlayerEnemyDirectiveAdapter : DirectiveAdapter<MaskedPlayerEnemy>
    {
        private const int RoamState = 0;
        private const int ChaseState = 1;
        private const float RunDistance = 17f;
        private const float HandsOutDistance = 6f;
        private const float WalkDistance = 12f;

        public override void Engage(MaskedPlayerEnemy enemy, PlayerControllerB target)
        {
            if (enemy.inSpecialAnimation) return;
            if (enemy.currentBehaviourStateIndex != ChaseState) enemy.SwitchToBehaviourState(ChaseState);
            if (enemy.searchForPlayers.inProgress) enemy.StopSearch(enemy.searchForPlayers);
            enemy.lostPlayerInChase = false;
            enemy.SetMovingTowardsTargetPlayer(target);

            if (enemy.stareAtTransform != target.gameplayCamera.transform)
                enemy.LookAtPlayerServerRpc((int)target.playerClientId);

            float distance = Vector3.Distance(enemy.transform.position, target.transform.position);
            if (distance > RunDistance)
            {
                SetHandsOut(enemy, false);
                SetRunning(enemy, true);
            }
            else if (distance < HandsOutDistance)
            {
                SetHandsOut(enemy, true);
            }
            else if (distance < WalkDistance)
            {
                SetHandsOut(enemy, false);
                if (!enemy.runningRandomly) SetRunning(enemy, false);
            }
        }

        public override void AfterUpdate(MaskedPlayerEnemy enemy, PlayerControllerB? target)
        {
            if (target == null) return;
            enemy.lostPlayerInChase = false;
            enemy.lostLOSTimer = 0f;
            if (enemy.currentBehaviourStateIndex == ChaseState)
                enemy.stopAndStareTimer = Mathf.Min(enemy.stopAndStareTimer, -1f);
            base.AfterUpdate(enemy, target);
        }

        public override void Disengage(MaskedPlayerEnemy enemy)
        {
            if (enemy.currentBehaviourStateIndex != RoamState) enemy.SwitchToBehaviourState(RoamState);
            if (enemy.stareAtTransform != null) enemy.StopLookingAtTransformServerRpc();
            SetHandsOut(enemy, false);
            enemy.lostPlayerInChase = false;
            enemy.movingTowardsTargetPlayer = false;
            enemy.targetPlayer = null;
        }

        public override void Follow(MaskedPlayerEnemy enemy, Vector3 position)
        {
            if (enemy.currentBehaviourStateIndex != RoamState) enemy.SwitchToBehaviourState(RoamState);
            if (enemy.searchForPlayers.inProgress) enemy.StopSearch(enemy.searchForPlayers);
            SetRunning(enemy, Vector3.Distance(enemy.transform.position, position) > WalkDistance);
            enemy.SetDestinationToPosition(position);
        }

        public override void Release(MaskedPlayerEnemy enemy)
        {
            Disengage(enemy);
            SetRunning(enemy, false);
        }

        public override float SightRange(MaskedPlayerEnemy enemy) => 70f;
        public override float SightAngle(MaskedPlayerEnemy enemy) => 50f;

        private static void SetRunning(MaskedPlayerEnemy enemy, bool running)
        {
            if (enemy.running == running) return;
            enemy.running = running;
            enemy.creatureAnimator.SetBool("Running", running);
            enemy.SetRunningServerRpc(running);
        }

        private static void SetHandsOut(MaskedPlayerEnemy enemy, bool handsOut)
        {
            if (enemy.handsOut == handsOut) return;
            enemy.handsOut = handsOut;
            enemy.SetHandsOutServerRpc(handsOut);
        }
    }
}
