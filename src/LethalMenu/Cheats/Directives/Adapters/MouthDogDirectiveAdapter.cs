using GameNetcodeStuff;
using UnityEngine;

namespace LethalMenu.Cheats.Directives.Adapters
{
    /// Eyeless Dog. The dog has no target player: vanilla enrages on a noise position. Chase = behaviour state 2 entered
    /// through EnrageDogOnLocalClient(position, distance, approximatePosition false, fullyEnrage true), which also sets
    /// noisePositionGuess (the point Update measures the lunge distance against) and the destination. Update state 2 enters
    /// EnterLunge (state 3) when within 4 m of noisePositionGuess; collision in state 2 also lunges; the kill is the victim's own
    /// OnCollideWithPlayer in state 3 -> KillPlayerServerRpc. Update state 2/1 drains suspicionLevel every AITimer and drops
    /// the dog to state 1/0 and SearchForPreviouslyHeardSound rewrites noisePositionGuess, so AfterUpdate pins noisePositionGuess
    /// on the target and keeps suspicionLevel / AITimer topped up. Escort uses state 1 (Update case 1 stops roamPlanet; state 0
    /// would restart it every frame and override the destination).
    public sealed class MouthDogDirectiveAdapter : DirectiveAdapter<MouthDogAI>
    {
        private const int RoamState = 0;
        private const int WatchState = 1;
        private const int ChaseState = 2;
        private const int LungeState = 3;
        private const int ChaseSuspicion = 11;
        private const int EscortSuspicion = 6;

        public override void Engage(MouthDogAI enemy, PlayerControllerB target)
        {
            if (enemy.inKillAnimation || enemy.currentBehaviourStateIndex == LungeState) return;
            Vector3 position = target.transform.position;
            if (enemy.currentBehaviourStateIndex != ChaseState)
                enemy.EnrageDogOnLocalClient(position, Vector3.Distance(enemy.transform.position, position), approximatePosition: false, fullyEnrage: true);

            enemy.suspicionLevel = ChaseSuspicion;
            enemy.noisePositionGuess = position;
            enemy.lastHeardNoisePosition = position;
            if (!enemy.inLunge) enemy.SetDestinationToPosition(position);
        }

        /// The dog never uses targetPlayer, so the base re-target is not called.
        public override void AfterUpdate(MouthDogAI enemy, PlayerControllerB? target)
        {
            int state = enemy.currentBehaviourStateIndex;
            if (state != WatchState && state != ChaseState) return;
            if (target != null)
            {
                enemy.noisePositionGuess = target.transform.position;
                enemy.suspicionLevel = ChaseSuspicion;
            }
            else
            {
                enemy.suspicionLevel = EscortSuspicion;
            }
            enemy.AITimer = 4f;
        }

        public override void Disengage(MouthDogAI enemy)
        {
            if (enemy.currentBehaviourStateIndex == ChaseState) enemy.SwitchToBehaviourState(WatchState);
        }

        public override void Follow(MouthDogAI enemy, Vector3 position)
        {
            if (enemy.currentBehaviourStateIndex == LungeState || enemy.inKillAnimation) return;
            if (enemy.currentBehaviourStateIndex != WatchState) enemy.SwitchToBehaviourState(WatchState);
            if (enemy.roamPlanet.inProgress) enemy.StopSearch(enemy.roamPlanet);
            enemy.suspicionLevel = EscortSuspicion;
            enemy.SetDestinationToPosition(position);
        }

        public override void Release(MouthDogAI enemy)
        {
            if (enemy.currentBehaviourStateIndex != LungeState && enemy.currentBehaviourStateIndex != RoamState)
                enemy.SwitchToBehaviourState(RoamState);
            enemy.suspicionLevel = 0;
        }

        public override float SightRange(MouthDogAI enemy) => 50f;
    }
}
