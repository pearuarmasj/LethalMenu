using GameNetcodeStuff;
using UnityEngine;

namespace LethalMenu.Cheats.Directives.Adapters
{
    /// Roaming Locusts. A harmless swarm in vanilla (no collision handler, no damage): state 0 = roam, the VFX is
    /// pulled to the enemy transform (MoveToTargetForce +6); state 1 = scatter when a player is within 8 m
    /// (MoveToTargetForce -35), switched by DoAIInterval, which is skipped while directed. Directed, the swarm is
    /// held in state 0 (SwitchToBehaviourState, replicated) so it clusters instead of fleeing, and the enemy
    /// walks to the target / ring point; a target inside the swarm takes BiteDamage twice a second through
    /// DirectiveAuthority.TryStrike. bugsRoam is stopped because it would override the
    /// destination. Update only drives VFX/audio from the state; AfterUpdate is the default pin.
    public sealed class DocileLocustBeesDirectiveAdapter : DirectiveAdapter<DocileLocustBeesAI>
    {
        private const int SwarmState = 0;
        private const float SwarmReach = 3f;
        private const int BiteDamage = 3;

        public override void Engage(DocileLocustBeesAI enemy, PlayerControllerB target)
        {
            Settle(enemy);
            enemy.SetMovingTowardsTargetPlayer(target);
            if (Vector3.Distance(enemy.transform.position, target.transform.position) < SwarmReach)
                DirectiveAuthority.TryStrike(enemy, target, BiteDamage, 0.5f);
        }

        public override void Follow(DocileLocustBeesAI enemy, Vector3 position)
        {
            Settle(enemy);
            enemy.SetDestinationToPosition(position);
        }

        public override void Disengage(DocileLocustBeesAI enemy)
        {
            enemy.movingTowardsTargetPlayer = false;
            enemy.targetPlayer = null;
        }

        /// Vanilla DoAIInterval restarts the roam search and the 8 m / 14 m scatter checks.
        public override void Release(DocileLocustBeesAI enemy) => Disengage(enemy);

        public override float SightRange(DocileLocustBeesAI enemy) => 30f;
        public override float SightAngle(DocileLocustBeesAI enemy) => 180f;

        private static void Settle(DocileLocustBeesAI enemy)
        {
            if (enemy.currentBehaviourStateIndex != SwarmState)
                enemy.SwitchToBehaviourState(SwarmState);
            if (enemy.bugsRoam.inProgress) enemy.StopSearch(enemy.bugsRoam);
        }
    }
}
