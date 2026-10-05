using GameNetcodeStuff;
using UnityEngine;

namespace LethalMenu.Cheats.Directives.Adapters
{
    /// Thumper. Vanilla chase = behaviour state 1 entered through BeginChasingPlayerServerRpc(playerId)
    /// (RequireOwnership = false; the ClientRpc screeches, SwitchToBehaviourStateOnLocalClient(1),
    /// SetMovingTowardsTargetPlayer). CrawlerAI.Update state 1 then accelerates and re-targets whoever is in
    /// CheckLineOfSightForPlayer, so AfterUpdate puts our target back. The bite is the victim's own
    /// OnCollideWithPlayer -> HitPlayerServerRpc, so it replicates normally.
    public sealed class CrawlerDirectiveAdapter : DirectiveAdapter<CrawlerAI>
    {
        private const int ChaseState = 1;

        public override void Engage(CrawlerAI enemy, PlayerControllerB target)
        {
            if (enemy.currentBehaviourStateIndex != ChaseState || enemy.targetPlayer != target)
                enemy.BeginChasingPlayerServerRpc((int)target.playerClientId);
            enemy.lostPlayerInChase = false;
            enemy.SetMovingTowardsTargetPlayer(target);
        }

        public override void AfterUpdate(CrawlerAI enemy, PlayerControllerB? target)
        {
            if (target == null) return;
            enemy.lostPlayerInChase = false;
            enemy.noticePlayerTimer = 0f;
            base.AfterUpdate(enemy, target);
        }

        public override void Disengage(CrawlerAI enemy)
        {
            if (enemy.currentBehaviourStateIndex != 0) enemy.SwitchToBehaviourState(0);
            enemy.movingTowardsTargetPlayer = false;
            enemy.targetPlayer = null;
        }

        public override void Follow(CrawlerAI enemy, Vector3 position)
        {
            if (enemy.currentBehaviourStateIndex != 0) enemy.SwitchToBehaviourState(0);
            if (enemy.searchForPlayers.inProgress) enemy.StopSearch(enemy.searchForPlayers);
            enemy.SetDestinationToPosition(position);
        }

        public override void Release(CrawlerAI enemy) => Disengage(enemy);

        public override float SightRange(CrawlerAI enemy) => 60f;
        public override float SightAngle(CrawlerAI enemy) => 55f;
    }
}
