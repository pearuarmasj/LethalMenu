using GameNetcodeStuff;
using UnityEngine;

namespace LethalMenu.Cheats.Directives.Adapters
{
    /// Barber. It only moves in the jump of a dance beat (Update: isJumping -> agent.speed = jumpSpeed for
    /// jumpTime, else 0; the beats come from the master surgeon's DoBeatOnOwnerClient / DoBeatClientRpc ->
    /// DanceBeat). The kill is the victim's own OnCollideWithPlayer -> KillPlayer(Snipping) +
    /// KillPlayerServerRpc (2 s hold-off through timeSinceSnip), so it replicates normally. Engage points the
    /// chase at the target (targetPlayer + movingTowardsTargetPlayer, EnemyAI.Update -> NavigateTowardsTargetPlayer)
    /// and keeps hasLOS false: DanceBeat's own "random point on a circle around the target" destination is only
    /// taken when hasLOS is set, and would replace the straight approach on every beat. DoAIInterval's
    /// TargetClosestPlayer / search routine is skipped; Follow and Disengage stop a running search routine.
    /// AfterUpdate is the base (targetPlayer / movingTowardsTargetPlayer).
    public sealed class ClaySurgeonDirectiveAdapter : DirectiveAdapter<ClaySurgeonAI>
    {
        public override void Engage(ClaySurgeonAI enemy, PlayerControllerB target)
        {
            if (enemy.searchRoutine.inProgress) enemy.StopSearch(enemy.searchRoutine);
            enemy.hasLOS = false;
            enemy.beatsSinceSeeingPlayer = 0;
            enemy.SetMovingTowardsTargetPlayer(target);
        }

        public override void Disengage(ClaySurgeonAI enemy)
        {
            if (enemy.searchRoutine.inProgress) enemy.StopSearch(enemy.searchRoutine);
            enemy.hasLOS = false;
            enemy.movingTowardsTargetPlayer = false;
            enemy.targetPlayer = null;
        }

        public override void Follow(ClaySurgeonAI enemy, Vector3 position)
        {
            Disengage(enemy);
            enemy.SetDestinationToPosition(position);
        }

        public override void Release(ClaySurgeonAI enemy) => Disengage(enemy);
    }
}
