using GameNetcodeStuff;
using UnityEngine;

namespace LethalMenu.Cheats.Directives.Adapters
{
    /// Circuit Bees. Vanilla attack = state 2 (hive stolen / angry, 10.3 m/s, zap mode 2): the swarm chases
    /// targetPlayer and the victim's own OnCollideWithPlayer does DamagePlayer(10, Electrocution) every 0.4 s and
    /// flips beesZappingMode to 3. Vanilla never kills at 10 hp or below (it skips the damage) and nothing in the game
    /// calls BeeKillPlayerServerRpc, so Engage finishes a weakened, adjacent target through that RPC (every client's
    /// copy of the victim runs KillPlayer; only the victim's owner acts). EnterAttackZapModeServerRpc is unused by
    /// vanilla too; AfterUpdate sets zap mode 3 locally while on top of the target, as OnCollideWithPlayer does.
    /// The hive is a separate scrap object and never moves; the guard / hive-defence bookkeeping in DoAIInterval is
    /// skipped, so the swarm may leave it (hunt) or trail the player (escort, state 0). AfterUpdate restores
    /// targetPlayer, which OnPlayerTeleported nulls whenever its target teleports.
    public sealed class RedLocustBeesDirectiveAdapter : DirectiveAdapter<RedLocustBees>
    {
        private const int GuardState = 0;
        private const int AttackState = 2;
        private const float ContactRange = 3f;
        private const int FinishHealth = 10;

        public override void Engage(RedLocustBees enemy, PlayerControllerB target)
        {
            if (enemy.searchForHive.inProgress) enemy.StopSearch(enemy.searchForHive);
            if (enemy.currentBehaviourStateIndex != AttackState) enemy.SwitchToBehaviourState(AttackState);
            enemy.agent.acceleration = 16f;
            enemy.lostLOSTimer = 0f;
            enemy.SetMovingTowardsTargetPlayer(target);

            if (!target.isPlayerDead && (target.health <= FinishHealth || target.criticallyInjured) &&
                Vector3.Distance(enemy.transform.position, target.transform.position) < ContactRange)
                enemy.BeeKillPlayerServerRpc((int)target.playerClientId);
        }

        public override void AfterUpdate(RedLocustBees enemy, PlayerControllerB? target)
        {
            if (target == null) return;
            enemy.lostLOSTimer = 0f;
            if (Vector3.Distance(enemy.transform.position, target.transform.position) < ContactRange)
                enemy.beesZappingMode = 3;
            base.AfterUpdate(enemy, target);
        }

        public override void Disengage(RedLocustBees enemy)
        {
            if (enemy.currentBehaviourStateIndex != GuardState) enemy.SwitchToBehaviourState(GuardState);
            enemy.movingTowardsTargetPlayer = false;
            enemy.targetPlayer = null;
        }

        public override void Follow(RedLocustBees enemy, Vector3 position)
        {
            if (enemy.currentBehaviourStateIndex != GuardState) enemy.SwitchToBehaviourState(GuardState);
            if (enemy.searchForHive.inProgress) enemy.StopSearch(enemy.searchForHive);
            enemy.SetDestinationToPosition(position);
        }

        public override void Release(RedLocustBees enemy) => Disengage(enemy);

        public override float SightRange(RedLocustBees enemy) => 16f;
        public override float SightAngle(RedLocustBees enemy) => 180f;
    }
}
