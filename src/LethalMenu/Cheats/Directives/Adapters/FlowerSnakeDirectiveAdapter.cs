using GameNetcodeStuff;
using UnityEngine;

namespace LethalMenu.Cheats.Directives.Adapters
{
    /// Tulip Snake. Vanilla chase = behaviour state 1: FlowerSnakeEnemy.DoAIInterval walks at 6 m/s and, within
    /// 12-22 m with line of sight and a 0.25-1.1 s cooldown, leaps with StartLeapOnLocalClient(dir) +
    /// StartLeapClientRpc(dir). The attack is the victim's own OnCollideWithPlayer -> FSHitPlayerServerRpc, which makes
    /// the snake cling (clingingToPlayer, 30-60 s); the snake never deals damage itself. Engage enters state 1, sends
    /// FSHitPlayerServerRpc for the target itself once within reach (any client may), and repeats the leap check.
    /// StartLeapClientRpc only goes out from the host; a director who is not the host leaps on their own client
    /// (the flight is simulated locally on every client anyway), lands on the nav mesh through
    /// DirectiveHostLogicPatches and pushes the position every frame so the others see the flight. While leaping, falling
    /// from a leap or clinging Engage/Follow do nothing; the snake's own code returns it to state 0 when the cling
    /// ends. Update never re-targets, so AfterUpdate only keeps targetPlayer pointed at the target.
    public sealed class FlowerSnakeDirectiveAdapter : DirectiveAdapter<FlowerSnakeEnemy>
    {
        private const int RoamState = 0;
        private const int ChaseState = 1;
        private const float ChaseSpeed = 6f;
        private const float RoamSpeed = 4.5f;
        private const float ClingReach = 2.5f;

        public override void Engage(FlowerSnakeEnemy enemy, PlayerControllerB target)
        {
            if (enemy.clingingToPlayer != null || enemy.leaping || enemy.fallingFromLeap) return;
            if (enemy.snakeRoam.inProgress) enemy.StopSearch(enemy.snakeRoam);
            if (enemy.currentBehaviourStateIndex != ChaseState) enemy.SwitchToBehaviourState(ChaseState);
            enemy.choseFarawayNode = false;
            enemy.timeSinceSeeingTarget = 0f;
            enemy.SetMovingTowardsTargetPlayer(target);
            enemy.agent.speed = ChaseSpeed;

            float distance = Vector3.Distance(target.transform.position, enemy.transform.position);

            // FSHitPlayerServerRpc(playerId) has RequireOwnership = false and takes the victim's id, so the
            // cling is requested directly once in reach: works from any client, no collision on the victim needed.
            if (distance < ClingReach && Time.realtimeSinceStartup - enemy.collideWithPlayerInterval > 1f &&
                Time.realtimeSinceStartup - enemy.timeOfLastCling > 4f && !target.inAnimationWithEnemy)
            {
                enemy.collideWithPlayerInterval = Time.realtimeSinceStartup;
                enemy.FSHitPlayerServerRpc((int)target.playerClientId);
                return;
            }

            if (distance < Random.Range(12f, 22f) &&
                enemy.CheckLineOfSightForPosition(target.gameplayCamera.transform.position, 100f, 30, 5f) &&
                Time.realtimeSinceStartup - enemy.timeOfLastLeap > Random.Range(0.25f, 1.1f))
            {
                Vector3 direction = target.transform.position - enemy.transform.position;
                direction += Random.insideUnitSphere * Random.Range(0.05f, 0.15f);
                direction.y = Mathf.Clamp(direction.y, -16f, 16f);
                direction = Vector3.Normalize(direction * 1000f);
                enemy.StartLeapOnLocalClient(direction);
                enemy.StartLeapClientRpc(direction);
            }
        }

        public override void AfterUpdate(FlowerSnakeEnemy enemy, PlayerControllerB? target)
        {
            base.AfterUpdate(enemy, target);
            if (enemy.leaping && !DirectiveAuthority.IsRealServer) enemy.SyncPositionToClients();
        }

        public override void Disengage(FlowerSnakeEnemy enemy)
        {
            if (enemy.clingingToPlayer == null && enemy.currentBehaviourStateIndex != RoamState)
                enemy.SwitchToBehaviourState(RoamState);
            enemy.movingTowardsTargetPlayer = false;
            enemy.targetPlayer = null;
        }

        public override void Follow(FlowerSnakeEnemy enemy, Vector3 position)
        {
            if (enemy.clingingToPlayer != null || enemy.leaping || enemy.fallingFromLeap) return;
            if (enemy.currentBehaviourStateIndex != RoamState) enemy.SwitchToBehaviourState(RoamState);
            if (enemy.snakeRoam.inProgress) enemy.StopSearch(enemy.snakeRoam);
            enemy.agent.speed = RoamSpeed;
            enemy.SetDestinationToPosition(position);
        }

        public override void Release(FlowerSnakeEnemy enemy) => Disengage(enemy);

        public override float SightRange(FlowerSnakeEnemy enemy) => 100f;
        public override float SightAngle(FlowerSnakeEnemy enemy) => 110f;
    }
}
