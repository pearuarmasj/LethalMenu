using GameNetcodeStuff;
using UnityEngine;

namespace LethalMenu.Cheats.Directives.Adapters
{
    /// Cadaver Growth. A stationary plant colony: Update pins destination to its own position, there is no chase
    /// state and no collision handler. Its only attack is infection. Vanilla InfectPlayers (Update, every
    /// InfectIntervalTime) rolls an infection for the LOCAL player standing among plants and then runs
    /// InfectPlayer(local, severe, spores) + InfectPlayerRpc(id, severe, spores) (NotMe, RequireOwnership false);
    /// the victim's own client progresses the fever and bursts (BurstFromPlayer kills through the Bloom), so the
    /// RPC carries it. Engage does the same for the target, without the dice, once the target is within
    /// InfectionRange of a living plant (non-eradicated tile, plantsInTile > 0) and not already infected. Follow,
    /// Disengage and Release do nothing (it cannot move) and CanUseEntrances is false. Update has no targeting, so
    /// AfterUpdate is a no-op. The colony's own growth is DoAIInterval's state 0, skipped while directed, so Grow
    /// repeats it every interval (GrowInTiles replicates through its own RPCs).
    public sealed class CadaverGrowthDirectiveAdapter : DirectiveAdapter<CadaverGrowthAI>
    {
        private const float InfectionRange = 8f;
        private const bool Severe = true;
        private const bool EmitSpores = true;

        public override void Engage(CadaverGrowthAI enemy, PlayerControllerB target)
        {
            Grow(enemy);
            int id = (int)target.playerClientId;
            if (!target.isPlayerControlled || target.isPlayerDead || !target.isInsideFactory) return;
            if (enemy.playerInfections[id].infected) return;
            if (!NearPlant(enemy, target.transform.position)) return;

            enemy.InfectPlayer(target, Severe, EmitSpores);
            enemy.InfectPlayerRpc(id, Severe, EmitSpores);
        }

        public override void AfterUpdate(CadaverGrowthAI enemy, PlayerControllerB? target) { }

        public override void Disengage(CadaverGrowthAI enemy) { }
        public override void Follow(CadaverGrowthAI enemy, Vector3 position) => Grow(enemy);
        public override void Release(CadaverGrowthAI enemy) { }

        public override float SightRange(CadaverGrowthAI enemy) => 15f;
        public override float SightAngle(CadaverGrowthAI enemy) => 180f;
        public override bool CanUseEntrances(CadaverGrowthAI enemy) => false;

        /// CadaverGrowthAI.DoAIInterval case 0.
        private static void Grow(CadaverGrowthAI enemy)
        {
            if (enemy.currentBehaviourStateIndex != 0) return;
            float step = enemy.AIIntervalTime;
            float timeOfDay = TimeOfDay.Instance.normalizedTimeOfDay;

            if (enemy.inGrowthBurst)
            {
                if (enemy.growthBurstTimer > 4f) enemy.inGrowthBurst = false;
                else enemy.growthBurstTimer += step;
            }
            else if (enemy.growthBurstTimer > 25f)
            {
                enemy.growthBurstTimer = 0f;
                enemy.inGrowthBurst = true;
            }
            else
            {
                enemy.growthBurstTimer += timeOfDay < 0.2f ? step * 0.66f : step;
            }

            float interval = enemy.inGrowthBurst
                ? 0.15f
                : Mathf.Clamp(enemy.growthIntervalCurveOverDay.Evaluate(timeOfDay) * enemy.GrowthInterval, 0.25f, 12f);
            if (enemy.growingInTiles || enemy.spreadInterval > interval)
            {
                enemy.growingInTiles = true;
                enemy.spreadInterval = 0f;
                enemy.GrowInTiles();
            }
            else
            {
                enemy.spreadInterval += step;
            }
        }

        private static bool NearPlant(CadaverGrowthAI enemy, Vector3 position)
        {
            float rangeSqr = InfectionRange * InfectionRange;
            foreach (var tile in enemy.GrowthTiles)
            {
                if (tile.eradicated || tile.plantsInTile <= 0) continue;
                foreach (var plant in tile.plantPositions)
                    if ((plant - position).sqrMagnitude < rangeSqr) return true;
            }
            return false;
        }
    }
}
