using GameNetcodeStuff;
using UnityEngine;

namespace LethalMenu.Cheats.Directives.Adapters
{
    /// Lasso Man. Vanilla chase = behaviour state 1 entered through BeginChasingPlayerServerRpc(playerId)
    /// (RequireOwnership = false; the ClientRpc runs SwitchToBehaviourStateOnLocalClient(1) and
    /// SetMovingTowardsTargetPlayer). The attack is the victim's own OnCollideWithPlayer -> DamagePlayer(40,
    /// Strangulation) every 0.5 s, so it replicates normally. LassoManAI.Update state 1 (owner) re-targets whoever is
    /// in line of sight, sets lostPlayerInChase after 2.5 s unseen and drops to state 0 at noticePlayerTimer < -15;
    /// AfterUpdate clears both. In state 0 (escort follow) Update would start a chase of the local player after two
    /// 0.05 s sight ticks, so AfterUpdate also zeroes noticePlayerTimer in that state (stunning it with the local
    /// player still triggers vanilla's retaliation chase).
    public sealed class LassoManDirectiveAdapter : DirectiveAdapter<LassoManAI>
    {
        private const int ChaseState = 1;

        public override void Engage(LassoManAI enemy, PlayerControllerB target)
        {
            if (enemy.searchForPlayers.inProgress) enemy.StopSearch(enemy.searchForPlayers);
            if (enemy.currentBehaviourStateIndex != ChaseState || enemy.targetPlayer != target)
                enemy.BeginChasingPlayerServerRpc((int)target.playerClientId);
            enemy.lostPlayerInChase = false;
            enemy.SetMovingTowardsTargetPlayer(target);
        }

        public override void AfterUpdate(LassoManAI enemy, PlayerControllerB? target)
        {
            enemy.noticePlayerTimer = 0f;
            if (target == null) return;
            enemy.lostPlayerInChase = false;
            base.AfterUpdate(enemy, target);
        }

        public override void Disengage(LassoManAI enemy)
        {
            if (enemy.currentBehaviourStateIndex != 0) enemy.SwitchToBehaviourState(0);
            enemy.movingTowardsTargetPlayer = false;
            enemy.targetPlayer = null;
        }

        public override void Follow(LassoManAI enemy, Vector3 position)
        {
            if (enemy.currentBehaviourStateIndex != 0) enemy.SwitchToBehaviourState(0);
            if (enemy.searchForPlayers.inProgress) enemy.StopSearch(enemy.searchForPlayers);
            enemy.SetDestinationToPosition(position);
        }

        public override void Release(LassoManAI enemy) => Disengage(enemy);

        public override float SightRange(LassoManAI enemy) => 55f;
        public override float SightAngle(LassoManAI enemy) => 55f;
    }
}
