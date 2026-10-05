using GameNetcodeStuff;
using UnityEngine;

namespace LethalMenu.Cheats.Directives.Adapters
{
    /// Butler. Vanilla attack = murder mode, behaviour state 2 (LookForChanceToMurder -> SwitchToBehaviourStateOnLocalClient(2)
    /// + SwitchOwnershipAndSetToStateServerRpc, which would hand the enemy to the target, so we use SwitchToBehaviourState(2)
    /// and SyncTargetServerRpc instead). State 2 movement lives in DoAIInterval (skipped): agent.speed 8, "Running" anim,
    /// 0 speed while drawing the knife (timeSinceChangingItem below 1.7). Engage reproduces that. The stab is the victim's own
    /// OnCollideWithPlayer -> DamagePlayer(10) + StabPlayerServerRpc (setBerserkMode false in state 2, so no ownership swap).
    /// Update's CheckLOS does not retarget in state 2; AfterUpdate keeps berserkModeTimer topped up and lostPlayerInChase clear.
    /// Escort uses state 0; Update case 0 stops the butler while isSweeping / timeSinceCheckingForMultiplePlayers is below 2
    /// (both normally driven by the skipped DoAIInterval), so AfterUpdate clears them.
    public sealed class ButlerDirectiveAdapter : DirectiveAdapter<ButlerEnemyAI>
    {
        private const int RoamState = 0;
        private const int MurderState = 2;
        private const float BerserkTime = 3f;
        private const float ChaseSpeed = 8f;
        private const float DrawKnifeTime = 1.7f;

        public override void Engage(ButlerEnemyAI enemy, PlayerControllerB target)
        {
            if (enemy.syncedTargetPlayer != target)
            {
                enemy.syncedTargetPlayer = target;
                enemy.SyncTargetServerRpc((int)target.playerClientId);
            }
            enemy.targetPlayer = target;
            enemy.watchingPlayer = target;
            if (enemy.currentBehaviourStateIndex != MurderState) enemy.SwitchToBehaviourState(MurderState);
            StopSearches(enemy);

            enemy.berserkModeTimer = Mathf.Max(enemy.berserkModeTimer, BerserkTime);
            enemy.lostPlayerInChase = false;
            enemy.loseInChaseTimer = 0f;
            enemy.SetMovingTowardsTargetPlayer(target);

            if (enemy.timeSinceChangingItem < DrawKnifeTime || enemy.stunNormalizedTimer > 0f)
            {
                enemy.agent.speed = 0f;
                return;
            }
            enemy.agent.speed = ChaseSpeed;
            enemy.creatureAnimator.SetBool("Running", true);
        }

        public override void AfterUpdate(ButlerEnemyAI enemy, PlayerControllerB? target)
        {
            if (target != null)
            {
                enemy.berserkModeTimer = Mathf.Max(enemy.berserkModeTimer, BerserkTime);
                enemy.lostPlayerInChase = false;
                enemy.loseInChaseTimer = 0f;
                base.AfterUpdate(enemy, target);
                return;
            }

            if (enemy.currentBehaviourStateIndex != RoamState) return;
            enemy.timeSinceCheckingForMultiplePlayers = Mathf.Max(enemy.timeSinceCheckingForMultiplePlayers, 3f);
            if (enemy.isSweeping)
            {
                enemy.isSweeping = false;
                enemy.SetSweepingAnimServerRpc(false);
            }
        }

        public override void Disengage(ButlerEnemyAI enemy)
        {
            // targetPlayer is left alone: Update's CheckLOS dereferences it when it differs from syncedTargetPlayer.
            if (enemy.currentBehaviourStateIndex != RoamState) enemy.SwitchToBehaviourState(RoamState);
            enemy.lostPlayerInChase = false;
            enemy.movingTowardsTargetPlayer = false;
        }

        public override void Follow(ButlerEnemyAI enemy, Vector3 position)
        {
            if (enemy.currentBehaviourStateIndex != RoamState) enemy.SwitchToBehaviourState(RoamState);
            StopSearches(enemy);
            enemy.movingTowardsTargetPlayer = false;
            enemy.SetDestinationToPosition(position);
        }

        public override void Release(ButlerEnemyAI enemy)
        {
            enemy.berserkModeTimer = 0f;
            Disengage(enemy);
        }

        public override float SightRange(ButlerEnemyAI enemy) => 100f;
        public override float SightAngle(ButlerEnemyAI enemy) => 50f;

        private static void StopSearches(ButlerEnemyAI enemy)
        {
            if (enemy.roamAndSweepFloor.inProgress) enemy.StopSearch(enemy.roamAndSweepFloor);
            if (enemy.hoverAroundTargetPlayer.inProgress) enemy.StopSearch(enemy.hoverAroundTargetPlayer);
        }
    }
}
