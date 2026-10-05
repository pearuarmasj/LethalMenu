using GameNetcodeStuff;
using UnityEngine;

namespace LethalMenu.Cheats.Directives.Adapters
{
    /// Manticoil. Passive flier with no attack in vanilla (no OnCollideWithPlayer, no damage RPC). Directed, it
    /// pecks: PeckDamage through DirectiveAuthority.TryStrike once a second while within PeckReach of the target.
    /// It takes off (behaviour state 1 through SwitchToBehaviourState, which replicates)
    /// and flies at the target / ring point instead of away from players. The DoAIInterval pieces that would undo
    /// this (roamGlide search, avoid-the-player destination, landing) are skipped while directed, so Engage/Follow
    /// only redo its speed ramp (agent.speed 5..19) and stop roamGlide. Update has no re-targeting; AfterUpdate is
    /// the default pin. Stunned or leaving birds are left alone.
    public sealed class DoublewingDirectiveAdapter : DirectiveAdapter<DoublewingAI>
    {
        private const int FlightState = 1;
        private const float PeckReach = 2.5f;
        private const int PeckDamage = 5;

        public override void Engage(DoublewingAI enemy, PlayerControllerB target)
        {
            if (!TakeOff(enemy)) return;
            enemy.SetMovingTowardsTargetPlayer(target);
            if (Vector3.Distance(enemy.transform.position, target.transform.position) < PeckReach)
                DirectiveAuthority.TryStrike(enemy, target, PeckDamage, 1f);
        }

        public override void Follow(DoublewingAI enemy, Vector3 position)
        {
            if (!TakeOff(enemy)) return;
            enemy.SetDestinationToPosition(position);
        }

        public override void Disengage(DoublewingAI enemy)
        {
            enemy.movingTowardsTargetPlayer = false;
            enemy.targetPlayer = null;
        }

        /// Left airborne: vanilla DoAIInterval resumes the glide search and lands the bird on its own.
        public override void Release(DoublewingAI enemy) => Disengage(enemy);

        public override float SightRange(DoublewingAI enemy) => 80f;
        public override float SightAngle(DoublewingAI enemy) => 60f;

        private static bool TakeOff(DoublewingAI enemy)
        {
            if (enemy.daytimeEnemyLeaving || enemy.stunNormalizedTimer > 0f) return false;
            if (enemy.currentBehaviourStateIndex != FlightState)
                enemy.SwitchToBehaviourState(FlightState);
            if (enemy.roamGlide.inProgress) enemy.StopSearch(enemy.roamGlide);
            enemy.avoidingPlayer = 0f;
            enemy.flyingToOtherBirdLanding = false;
            enemy.agent.speed = Mathf.Clamp(enemy.agent.speed + enemy.AIIntervalTime * 4f, 5f, 19f);
            return true;
        }
    }
}
