using GameNetcodeStuff;
using UnityEngine;

namespace LethalMenu.Cheats.Directives.Adapters
{
    /// Cadaver Bloom (the walking plant that bursts out of an infected player). Vanilla chase = behaviour state 1
    /// with targetPlayer set, entered in CheckForVeryClosePlayer: targetPlayer, SetNewTargetPlayerRpc(id) (NotMe,
    /// puts the other clients into state 1 with the same target) and SwitchToBehaviourStateOnLocalClient(1).
    /// Update state 1 then does the whole chase (jitter dodging, chest-bite animation, speed, destination) but
    /// reads distanceToTarget / isBehindObstacle / pathDistance, which only DoAIInterval's state-1 block fills, so
    /// Engage recomputes them every call. The bite is OnCollideWithPlayer on the victim's client ->
    /// DamagePlayer(30, callRPC: true), which replicates normally (SyncBitePlayerRpc is audio only and has no
    /// caller in vanilla). AfterUpdate holds lostPlayerInChase/lostPlayerTimer, which Update never touches but
    /// which DoAIInterval would use to end the chase, and re-pins the target (the post-burst
    /// CheckForVeryClosePlayer(seeInstantly) can retarget once). A bloom that has not burst yet
    /// (inSpecialAnimation, agent off) is left alone.
    public sealed class CadaverBloomDirectiveAdapter : DirectiveAdapter<CadaverBloomAI>
    {
        private const int RoamState = 0;
        private const int ChaseState = 1;
        private const float PathCheckHeight = 15f;
        private const float ObstacleDetourRatio = 5f;

        public override void Engage(CadaverBloomAI enemy, PlayerControllerB target)
        {
            if (!Active(enemy)) return;

            if (enemy.currentBehaviourStateIndex != ChaseState || enemy.targetPlayer != target)
            {
                enemy.targetPlayer = target;
                enemy.SetNewTargetPlayerRpc((int)target.playerClientId);
                enemy.SwitchToBehaviourStateOnLocalClient(ChaseState);
            }
            if (enemy.searchForPlayers.inProgress) enemy.StopSearch(enemy.searchForPlayers);
            Hold(enemy, target);
            MeasureTarget(enemy, target);
        }

        public override void AfterUpdate(CadaverBloomAI enemy, PlayerControllerB? target)
        {
            if (target == null || !Active(enemy)) return;
            Hold(enemy, target);
            base.AfterUpdate(enemy, target);
        }

        public override void Disengage(CadaverBloomAI enemy)
        {
            if (enemy.currentBehaviourStateIndex != RoamState) enemy.SwitchToBehaviourState(RoamState);
            enemy.movingTowardsTargetPlayer = false;
            enemy.targetPlayer = null;
            enemy.lostPlayerInChase = true;
        }

        public override void Follow(CadaverBloomAI enemy, Vector3 position)
        {
            if (!Active(enemy)) return;
            if (enemy.currentBehaviourStateIndex != RoamState) enemy.SwitchToBehaviourState(RoamState);
            enemy.lostPlayerInChase = true;
            if (enemy.searchForPlayers.inProgress) enemy.StopSearch(enemy.searchForPlayers);
            enemy.SetDestinationToPosition(position);
        }

        public override void Release(CadaverBloomAI enemy)
        {
            Disengage(enemy);
            if (enemy.searchForPlayers.inProgress) enemy.StopSearch(enemy.searchForPlayers);
        }

        /// CheckForVeryClosePlayer: GetAllPlayersInLineOfSightNonAlloc(70, 50).
        public override float SightRange(CadaverBloomAI enemy) => 70f;
        public override float SightAngle(CadaverBloomAI enemy) => 50f;

        private static bool Active(CadaverBloomAI enemy) => enemy.hasBurst && !enemy.inSpecialAnimation;

        private static void Hold(CadaverBloomAI enemy, PlayerControllerB target)
        {
            enemy.lostPlayerInChase = false;
            enemy.lostPlayerTimer = 0f;
            enemy.lastPositionOfSeenPlayer = target.transform.position;
        }

        /// DoAIInterval state 1, targetPlayer != null block.
        private static void MeasureTarget(CadaverBloomAI enemy, PlayerControllerB target)
        {
            Vector3 targetPos = target.transform.position;
            if (Physics.Raycast(targetPos, Vector3.down, out RaycastHit hit, PathCheckHeight, enemy.agentMask, QueryTriggerInteraction.Ignore))
                targetPos = hit.point;

            if (enemy.GetPathDistance(targetPos, enemy.transform.position))
            {
                enemy.distanceToTarget = enemy.pathDistance;
                enemy.isBehindObstacle = enemy.distanceToTarget - Vector3.Distance(enemy.transform.position, target.transform.position)
                    > enemy.distanceToTarget / ObstacleDetourRatio;
            }
            else
            {
                enemy.distanceToTarget = Vector3.Distance(target.transform.position, enemy.transform.position);
                enemy.isBehindObstacle = false;
            }
        }
    }
}
