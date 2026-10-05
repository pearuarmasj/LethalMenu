using GameNetcodeStuff;
using UnityEngine;

namespace LethalMenu.Cheats.Directives.Adapters
{
    /// Forest Giant. Vanilla chase = behaviour state 1 entered through BeginChasingNewPlayerServerRpc(playerId) after
    /// FindAndTargetNewPlayerOnLocalClient (chasingPlayer); the ClientRpc switches every client to state 1. DoAIInterval state 1
    /// (skipped) stops roamPlanet/searchForPlayers and calls SetMovingTowardsTargetPlayer(chasingPlayer), so Engage does that.
    /// The grab is the victim's own OnCollideWithPlayer -> GrabPlayerServerRpc, so it replicates normally.
    /// Update state 1 (LookForPlayers) retargets to other players / the stunner and flips lostPlayerInChase after 3 s without
    /// line of sight; AfterUpdate restores chasingPlayer and clears lostPlayerInChase / noticePlayerTimer. With no target
    /// (escort) AfterUpdate cancels Update state 0's stop-and-look and drops any chase the giant started on its own.
    public sealed class ForestGiantDirectiveAdapter : DirectiveAdapter<ForestGiantAI>
    {
        private const int RoamState = 0;
        private const int ChaseState = 1;

        public override void Engage(ForestGiantAI enemy, PlayerControllerB target)
        {
            if (enemy.inEatingPlayerAnimation) return;
            if (enemy.currentBehaviourStateIndex != ChaseState || enemy.chasingPlayer != target)
            {
                enemy.FindAndTargetNewPlayerOnLocalClient(target);
                enemy.BeginChasingNewPlayerServerRpc((int)target.playerClientId);
            }
            enemy.investigating = false;
            enemy.hasBegunInvestigating = false;
            if (enemy.roamPlanet.inProgress) enemy.StopSearch(enemy.roamPlanet, clear: false);
            if (enemy.searchForPlayers.inProgress) enemy.StopSearch(enemy.searchForPlayers);
            enemy.lostPlayerInChase = false;
            enemy.SetMovingTowardsTargetPlayer(target);
        }

        public override void AfterUpdate(ForestGiantAI enemy, PlayerControllerB? target)
        {
            if (target == null)
            {
                enemy.stopAndLookTimer = 0f;
                enemy.timeSpentStaring = 0f;
                if (enemy.currentBehaviourStateIndex == ChaseState) enemy.SwitchToBehaviourState(RoamState);
                return;
            }

            if (enemy.currentBehaviourStateIndex != ChaseState) return;
            enemy.chasingPlayer = target;
            enemy.lostPlayerInChase = false;
            enemy.noticePlayerTimer = 0f;
            enemy.chasingPlayerInLOS = true;
            base.AfterUpdate(enemy, target);
        }

        public override void Disengage(ForestGiantAI enemy)
        {
            if (enemy.currentBehaviourStateIndex != RoamState) enemy.SwitchToBehaviourState(RoamState);
            enemy.movingTowardsTargetPlayer = false;
            enemy.targetPlayer = null;
        }

        public override void Follow(ForestGiantAI enemy, Vector3 position)
        {
            if (enemy.currentBehaviourStateIndex != RoamState) enemy.SwitchToBehaviourState(RoamState);
            if (enemy.roamPlanet.inProgress) enemy.StopSearch(enemy.roamPlanet, clear: false);
            if (enemy.searchForPlayers.inProgress) enemy.StopSearch(enemy.searchForPlayers);
            enemy.investigating = false;
            enemy.hasBegunInvestigating = false;
            enemy.movingTowardsTargetPlayer = false;
            enemy.SetDestinationToPosition(position);
        }

        public override void Release(ForestGiantAI enemy) => Disengage(enemy);

        public override float SightRange(ForestGiantAI enemy) => 50f;
        public override float SightAngle(ForestGiantAI enemy) => 70f;
    }
}
